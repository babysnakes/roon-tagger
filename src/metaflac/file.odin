package metaflac

import "core:bufio"
import "core:io"
import "core:os"
import "core:strings"

Flac_Metadata :: struct {
	path:   string,
	length: u32,
	blocks: [dynamic]Block,
}

Metaflac_Error :: union {
	os.Error,
	io.Error,
	Flac_Error,
	Block_Error,
}

Flac_Error :: enum {
	Not_Flac,
	Stream_Info_Error,
}

load_metadata_from_file :: proc(path: string) -> (result: Flac_Metadata, err: Metaflac_Error) {
	result.path = path

	file := os.open(path, os.O_RDONLY) or_return
	defer os.close(file)
	reader: bufio.Reader
	bufio.reader_init(&reader, os.to_stream(file))
	defer bufio.reader_destroy(&reader)
	stream := bufio.reader_to_stream(&reader)

	load_metadata_from_reader(stream, &result) or_return

	return result, nil
}

load_metadata_from_reader :: proc(stream: io.Reader, meta: ^Flac_Metadata) -> Metaflac_Error {
	read_ident(stream) or_return

	for {
		hdr: [1]u8
		io.read_full(stream, hdr[:]) or_return
		is_last := (hdr[0] & 0x80) != 0 // first bit is 0 means more blocks follow
		block_type := hdr[0] & 0x7F
		len_buf: [3]u8
		io.read_full(stream, len_buf[:]) or_return
		data_len := read_3bytes_as_u32be(len_buf)
		data := make([]u8, data_len) // ownership of data is passed to `parse_block`
		io.read_full(stream, data[:]) or_return

		block := parse_block(block_type, data) or_return
		append(&meta.blocks, block)
		meta.length += (data_len + 4)

		if is_last do break
	}

	return validate_metadata(meta)
}

save_metadata :: proc(meta: ^Flac_Metadata) -> Metaflac_Error {
	blocks, length := write_blocks_data(meta)
	defer delete(blocks)
	sufficient_padding := length + 4 <= meta.length // 4 is the minimal padding header size

	if sufficient_padding {
		new_padding_size := meta.length - length - 4
		write_padding(new_padding_size, &blocks)

		file := os.open(meta.path, os.O_RDWR) or_return
		defer os.close(file)
		stream := os.to_stream(file)

		read_ident(stream) or_return
		written := io.write(stream, blocks[:]) or_return
		ensure(written == len(blocks), "BUG: mismatch number of bytes written to Flac file")
	} else {
		tmp_path := strings.concatenate({meta.path, ".tmp"})
		defer delete(tmp_path)
		cur_flac := os.open(meta.path, os.O_RDONLY) or_return
		cur_stream := os.to_stream(cur_flac)
		new_flac := os.create(tmp_path) or_return
		new_stream := os.to_stream(new_flac)

		// place the defers in a scope that will close the files before
		// the rename.
		{
			defer os.close(cur_flac)
			defer os.close(new_flac)

			write_padding(1024, &blocks) // give it a default of 1024 bytes
			io.write_string(new_stream, "fLaC") or_return
			io.write(new_stream, blocks[:])
			skip_metadata(cur_stream) or_return

			buf: [64 * 1024]u8 // larger buffer than the default in io.copy
			_, c_err := io.copy_buffer(new_stream, cur_stream, buf[:])
			if c_err != nil {
				defer os.remove(tmp_path)
				return c_err
			}
		}

		// TODO We must better handle the error here, the user must be notified about the
		os.rename(tmp_path, meta.path) or_return
		return nil
	}

	return nil
}

// Skips all the headers and metadata in the supplied flac file as stream.
// Positions the stream at the start of the audio data.
skip_metadata :: proc(stream: io.Reader) -> Metaflac_Error {
	read_ident(stream) or_return

	for {
		header := io.read_byte(stream) or_return
		is_last := (header & 0x80) != 0
		length: [3]u8
		io.read_full(stream, length[:]) or_return
		skip_length := read_3bytes_as_u32be(length)
		io.seek(stream, i64(skip_length), .Current) or_return

		if is_last do break
	}

	return nil
}

validate_metadata :: proc(meta: ^Flac_Metadata) -> Metaflac_Error {
	if len(meta.blocks) < 1 do return .Stream_Info_Error
	_, type_ok := meta.blocks[0].(Stream_Info_Block)
	if !type_ok do return .Stream_Info_Error

	return nil
}
// Release memory allocated by metadata
release_metadata :: proc(meta: ^Flac_Metadata) {
	for b in meta.blocks {
		release_block(b)
	}
	defer delete(meta.blocks)
}

// Reads the stream header to identify whether it's a valid flac file. Returns false if it's not a Flac file.
@(private)
read_ident :: proc(rd: io.Reader) -> Metaflac_Error {
	buf: [4]u8
	io.read_full(rd, buf[:]) or_return

	// TODO: optionally skip ID3 tab header - this is not in the RFC, but some libraries support it.

	if (string(buf[:]) != "fLaC") {
		return .Not_Flac
	}

	return nil
}

// parse 3 bytes as u32 BigEndian
read_3bytes_as_u32be :: proc(bytes: [3]u8) -> u32 {
	return u32(bytes[0]) << 16 | u32(bytes[1]) << 8 | u32(bytes[2])
}

// write u32 to 3 bytes using big endian format (in reality the u32 should be
// smaller than 24bits)
write_u32_be_3bytes :: proc(value: u32) -> [3]u8 {
	return {u8(value >> 16), u8(value >> 8), u8(value)}
}
