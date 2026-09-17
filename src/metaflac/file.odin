package metaflac

import "core:bufio"
import "core:io"
import "core:os"

Flac_Metadata :: struct {
	path:   string,
	length: u32,
	blocks: [dynamic]Block,
}

Metaflac_Error :: union {
	os.Error,
	io.Error,
	Not_Flac,
	Block_Error,
}

Not_Flac :: struct {}

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

	// TODO: Validate blocks (e.g., StreamInfo is the first block...)

	return nil
}

// Release memory allocated by metadata
release_metadata :: proc(meta: ^Flac_Metadata) {
	for &b in meta.blocks {
		release_block(&b)
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
		return Not_Flac{}
	}

	return nil
}

// parse 3 bytes as u32 BigEndian
read_3bytes_as_u32be :: proc(bytes: [3]u8) -> u32 {
	return u32(bytes[0]) << 16 | u32(bytes[1]) << 8 | u32(bytes[2])
}
