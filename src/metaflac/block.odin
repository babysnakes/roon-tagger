package metaflac

import "core:encoding/endian"
import "core:fmt"
import "core:strings"

Block :: union #no_nil {
	Stream_Info_Block,
	Padding_Block,
	Application_Block,
	Seek_Table_Block,
	Vorbis_Comment_Block,
	Cuesheet_Block,
	Picture_Block,
	Unknown_Block,
}

// Our handling of StreamInfo is only for extracting some file info. When
// writing back we use the original data!
Stream_Info_Block :: struct {
	min_block_size:  u16,
	max_block_size:  u16,
	min_frame_size:  u32,
	max_frame_size:  u32,
	sample_rate:     u32,
	num_channels:    u8,
	bits_per_sample: u8,
	data:            []u8,
}

Padding_Block :: struct {
	size: int,
}

Application_Block :: struct {
	data: []u8,
}

Seek_Table_Block :: struct {
	data: []u8,
}

Vorbis_Comment_Block :: struct {
	vendor_string: string,
	comments:      map[string][dynamic]string,
}

Cuesheet_Block :: struct {
	data: []u8,
}

Picture_Block :: struct {
	data: []u8,
}

Unknown_Block :: struct {
	kind: u8,
	data: []u8,
}

Block_Error :: enum {
	None,
	Stream_Info_Parse_Error,
	Vorbis_Comment_Parse_Error,
}

parse_block :: proc(block_type: u8, data: []u8) -> (Block, Block_Error) {
	switch block_type {
	case 0:
		return parse_stream_info(data)
	case 1:
		return parse_padding(data)
	case 2:
		return parse_application(data)
	case 3:
		return parse_seek_table(data)
	case 4:
		return parse_vorbis_comment(data)
	case 5:
		return parse_cuesheet(data)
	case 6:
		return parse_picture(data)
	case:
		return parse_unknown(block_type, data)
	}
}

// Get pointer to the stream info block of the metadata
// Note: Assumes the metadata validity is verified - the first metadata block *must* be a stream info.
stream_info :: proc(meta: ^Flac_Metadata) -> ^Stream_Info_Block {
	return &meta.blocks[0].(Stream_Info_Block)
}

vorbis_comment :: proc(meta: ^Flac_Metadata) -> ^Vorbis_Comment_Block {
	for &b in meta.blocks {
		if vc, ok := &b.(Vorbis_Comment_Block); ok do return vc
	}
	// we do not append as it may already have block with `is_last` indication
	inject_at(&meta.blocks, 1, Vorbis_Comment_Block{})
	return &meta.blocks[0].(Vorbis_Comment_Block)
}

print_block :: proc(block: Block) {
	switch b in block {
	case Stream_Info_Block:
		fmt.println("  Stream_Info_Block:")
		fmt.printfln("    min block size:          %v", b.min_block_size)
		fmt.printfln("    max block size:          %v", b.max_block_size)
		fmt.printfln("    min frame size:          %v", b.min_frame_size)
		fmt.printfln("    max frame size:          %v", b.max_frame_size)
		fmt.printfln("    sample rate:             %v", b.sample_rate)
		fmt.printfln("    number of channels:      %v", b.num_channels)
		fmt.printfln("    bits per sample:         %v", b.bits_per_sample)
		fmt.printfln("    data (length: %d)", len(b.data))
	case Padding_Block:
		fmt.println("  Padding_Block:")
		fmt.printfln("    size: %d", b.size)
	case Application_Block:
		fmt.println("  Application_Block:")
		fmt.printfln("    data (length: %d)", len(b.data))
	case Seek_Table_Block:
		fmt.println("  Seek_Table_Block:")
		fmt.printfln("    data (length: %d)", len(b.data))
	case Vorbis_Comment_Block:
		fmt.println("  Vorbis_Comment_Block:")
		fmt.printfln("    vendor string: %s\n", b.vendor_string)
		for k, vs in b.comments {
			fmt.printfln("    * %s:", k)

			for v in vs {
				fmt.printfln("      - %s", v)
			}
		}
	case Cuesheet_Block:
		fmt.println("  Cuesheet_Block:")
		fmt.printfln("    data (length: %d)", len(b.data))
	case Picture_Block:
		fmt.println("  Picture_Block:")
		fmt.printfln("    data (length: %d)", len(b.data))
	case Unknown_Block:
		fmt.println("  Unknown_Block:")
		fmt.printfln("    kind: %v", b.kind)
		fmt.printfln("    data (length: %d)", len(b.data))
	}
}

// Converts the provided metadata blocks to bytes. Removes padding!
write_blocks_data :: proc(meta: ^Flac_Metadata) -> ([dynamic]u8, u32) {
	data: [dynamic]u8
	data_length: u32 = 0
	for idx in 0 ..< len(meta.blocks) {
		data_length += write_block(meta.blocks[idx], &data)
	}
	return data, data_length
}

release_block :: proc(block: Block) {
	switch b in block {
	case Stream_Info_Block:
		delete(b.data)
	case Application_Block:
		delete(b.data)
	case Seek_Table_Block:
		delete(b.data)
	case Vorbis_Comment_Block:
		for k, vs in b.comments {
			delete(k)
			for v in vs {
				delete(v)
			}
			delete(vs)
		}
		delete(b.comments)
		delete(b.vendor_string)
	case Cuesheet_Block:
		delete(b.data)
	case Picture_Block:
		delete(b.data)
	case Unknown_Block:
		delete(b.data)
	case Padding_Block:
	// nothing
	}
}

write_block :: proc(block: Block, writer: ^[dynamic]u8) -> (result: u32) {
	data: []u8
	encoded: [dynamic]u8
	defer delete(encoded)

	switch b in block {
	case Stream_Info_Block:
		data = b.data
	case Application_Block:
		data = b.data
	case Seek_Table_Block:
		data = b.data
	case Cuesheet_Block:
		data = b.data
	case Picture_Block:
		data = b.data
	case Unknown_Block:
		data = b.data
	case Vorbis_Comment_Block:
		encoded = encode_vorbis_comment(b)
		data = encoded[:]
	case Padding_Block:
		return 0
	}

	kind := block_type(block)
	append(writer, kind) // is_last is false in this context so we ignore it.

	length := u32(len(data))
	ensure(length < 0xFFFFFF, "block size too large") // TODO: also add warn log
	length_bytes := write_u32_be_3bytes(u32(length))
	append(writer, ..length_bytes[:])
	append(writer, ..data)
	result += (length + 4)
	return result
}

// Write padding as the last block in the metadata
write_padding :: proc(size: u32, writer: ^[dynamic]u8) {
	byte: u8 = 0x80 // indicates last block
	byte |= 1 // padding kind
	append(writer, byte)

	ensure(size < 0xFFFFFF, "padding size too large") // TODO: also add warn log
	length_bytes := write_u32_be_3bytes(size)
	append(writer, ..length_bytes[:])

	padding := make([]u8, size) // by default filled with 0x00
	defer delete(padding)
	append(writer, ..padding)
}

block_type :: proc(block: Block) -> (kind: u8) {
	switch b in block {
	case Stream_Info_Block:
		kind = 0
	case Padding_Block:
		kind = 1
	case Application_Block:
		kind = 2
	case Seek_Table_Block:
		kind = 3
	case Vorbis_Comment_Block:
		kind = 4
	case Cuesheet_Block:
		kind = 5
	case Picture_Block:
		kind = 6
	case Unknown_Block:
		kind = b.kind
	}

	return kind
}

parse_stream_info :: proc(data: []u8) -> (Stream_Info_Block, Block_Error) {
	result := Stream_Info_Block{}
	idx := 0
	ok: bool
	result.data = data
	result.min_block_size, ok = endian.get_u16(data[idx:idx + 2], .Big)
	if !ok do return result, .Stream_Info_Parse_Error
	idx += 2
	result.max_block_size, ok = endian.get_u16(data[idx:idx + 2], .Big)
	if !ok do return result, .Stream_Info_Parse_Error
	idx += 2
	result.min_frame_size = read_3bytes_as_u32be([3]u8{data[idx], data[idx + 1], data[idx + 2]})
	idx += 3
	result.max_frame_size = read_3bytes_as_u32be([3]u8{data[idx], data[idx + 1], data[idx + 2]})
	idx += 3

	// next 3 values are because of inconsistencies between actual data size and bytes
	sample_first_tmp: u16
	sample_first_tmp, ok = endian.get_u16(data[idx:idx + 2], .Big)
	idx += 2
	sample_second_tmp := data[idx]
	idx += 1
	bps_tmp := data[idx]

	result.sample_rate = u32(sample_first_tmp) << 4 | u32(sample_second_tmp) >> 4
	result.num_channels = ((sample_second_tmp >> 1) & 0x7) + 1
	result.bits_per_sample = (((sample_second_tmp & 0x1) << 4) | bps_tmp >> 4) + 1

	return result, .None
}

parse_padding :: proc(data: []u8) -> (Padding_Block, Block_Error) {
	defer delete(data)
	return Padding_Block{size = len(data)}, .None
}

parse_application :: proc(data: []u8) -> (Application_Block, Block_Error) {
	return Application_Block{data = data}, .None
}

parse_seek_table :: proc(data: []u8) -> (Seek_Table_Block, Block_Error) {
	return Seek_Table_Block{data = data}, .None
}

parse_vorbis_comment :: proc(data: []u8) -> (Vorbis_Comment_Block, Block_Error) {
	defer delete(data)
	idx: u32 = 0
	result := Vorbis_Comment_Block{}

	vs_length, vs_ok := endian.get_u32(data[idx:idx + 4], .Little)
	if vs_length < 1 {
		// TODO: Log warning
		return result, .Vorbis_Comment_Parse_Error
	}
	if !vs_ok do return result, .Vorbis_Comment_Parse_Error
	idx += 4
	result.vendor_string = strings.clone_from_bytes(data[idx:idx + vs_length])
	idx += vs_length
	num_comments, ok_nc := endian.get_u32(data[idx:idx + 4], .Little)
	if !ok_nc do return result, .Vorbis_Comment_Parse_Error
	idx += 4

	comments := make(map[string][dynamic]string)
	for _ in 0 ..< num_comments {
		comment_length, ok_cl := endian.get_u32(data[idx:idx + 4], .Little)
		if !ok_cl do return result, .Vorbis_Comment_Parse_Error
		idx += 4
		comment := string(data[idx:idx + comment_length])
		kv, err := strings.split_n(comment, "=", 2)
		if err != nil do return result, .Vorbis_Comment_Parse_Error
		if len(kv) != 2 do return result, .Vorbis_Comment_Parse_Error
		defer delete(kv)
		v := strings.clone(kv[1])
		idx += comment_length
		value, ok := &comments[kv[0]]
		if ok {
			append(value, v)
		} else {
			new_val: [dynamic]string
			append(&new_val, v)
			comments[strings.clone(kv[0])] = new_val
		}
	}
	result.comments = comments

	return result, .None
}

parse_cuesheet :: proc(data: []u8) -> (Cuesheet_Block, Block_Error) {
	return Cuesheet_Block{data = data}, .None
}

parse_picture :: proc(data: []u8) -> (Picture_Block, Block_Error) {
	return Picture_Block{data = data}, .None
}

parse_unknown :: proc(block_type: u8, data: []u8) -> (Unknown_Block, Block_Error) {
	return Unknown_Block{kind = block_type, data = data}, .None
}

encode_vorbis_comment :: proc(vc: Vorbis_Comment_Block) -> [dynamic]u8 {
	result: [dynamic]u8

	vc_length: [4]u8
	ensure(
		endian.put_u32(vc_length[:], .Little, u32(len(vc.vendor_string))),
		"Failed making LE vc length",
	)
	append(&result, ..vc_length[:])
	append(&result, vc.vendor_string)

	num_comments: u32
	for _, vs in vc.comments {
		num_comments += u32(len(vs))
	}
	nc_bytes: [4]u8
	ensure(endian.put_u32(nc_bytes[:], .Little, num_comments), "Failed making LE num_comments")
	append(&result, ..nc_bytes[:])

	for k, vs in vc.comments {
		for v in vs {
			length := u32(len(k)) + 1 + u32(len(v))
			len_bytes: [4]u8
			ensure(
				endian.put_u32(len_bytes[:], .Little, length),
				"Failed making LE comment length",
			)
			append(&result, ..len_bytes[:])
			append(&result, k)
			append(&result, '=')
			append(&result, v)
		}
	}

	return result
}

// Vorbis Comments only allow specific range of UTF-8 characters (Ux0020 to ]x007E) excluding '='.
validate_comment :: proc(comment: string) -> bool {
	result := true
	for r in comment {
		if r < 0x0020 || r > 0x007E || r == 0x003D do result = false
	}
	return result
}
