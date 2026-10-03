#+feature dynamic-literals
#+test

package metaflac

import "core:mem"
import "core:mem/virtual"
import "core:slice"
import "core:strings"

get_sample_small_metadata :: proc() -> ^Flac_Metadata {

	result := mk_empty_flac_metadata()
	allocator := virtual.arena_allocator(&result.arena)

	si_block := Stream_Info_Block {
		min_block_size  = 4096,
		max_block_size  = 4096,
		min_frame_size  = 3249,
		max_frame_size  = 3249,
		sample_rate     = 44100,
		num_channels    = 1,
		bits_per_sample = 16,
		data            = slice.clone(
			[]u8 {
				0x10,
				0x00,
				0x10,
				0x00,
				0x00,
				0x0C,
				0xB1,
				0x00,
				0x0C,
				0xB1,
				0x0A,
				0xC4,
				0x40,
				0xF0,
				0x00,
				0x00,
				0x08,
				0xB4,
				0xFC,
				0x02,
				0x1A,
				0xD0,
				0x9C,
				0x00,
				0xBE,
				0x0B,
				0xEB,
				0xA4,
				0xAB,
				0xC5,
				0xE4,
				0xC0,
				0xA1,
				0x02,
			},
			allocator,
		),
	}
	comments := make(map[string][dynamic]string, allocator)
	comments[strings.clone("TITLE", allocator)] = mk_comment_value("empty", allocator)
	comments[strings.clone("COMMENT", allocator)] = mk_comment_value(
		"fre:ac - free audio converter <https://www.freac.org/>",
		allocator,
	)
	comments[strings.clone("ENCODER", allocator)] = mk_comment_value("fre:ac v1.1.4", allocator)
	vc_block := Vorbis_Comment_Block {
		vendor_string = strings.clone("reference libFLAC 1.3.3 20190804", allocator),
		comments      = comments,
	}
	sk_block := Seek_Table_Block {
		data = slice.clone(
			[]u8 {
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x00,
				0x08,
				0xB4,
			},
			allocator,
		),
	}
	pad_block := Padding_Block {
		size = 512,
	}

	result.blocks = make([dynamic]Block, allocator)
	append(&result.blocks, si_block, vc_block, sk_block, pad_block)
	result.length = 726

	return result
}

mk_comment_value :: proc(v: string, allocator: mem.Allocator) -> [dynamic]string {
	result := make([dynamic]string, allocator)
	append(&result, v)
	return result
}
