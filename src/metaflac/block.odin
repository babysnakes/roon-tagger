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
				fmt.printfln("      - %s:", v)
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
		comment := strings.clone_from_bytes(data[idx:idx + comment_length])
		kv, err := strings.split_n(comment, "=", 2)
		if err != nil do return result, .Vorbis_Comment_Parse_Error
		if len(kv) != 2 do return result, .Vorbis_Comment_Parse_Error
		k, v := kv[0], kv[1]
		idx += comment_length
		value, ok := &comments[k]
		if ok {
			append(value, v)
		} else {
			new_val: [dynamic]string
			append(&new_val, v)
			comments[k] = new_val
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
