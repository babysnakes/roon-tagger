#+feature dynamic-literals
#+test

package metaflac

import "core:slice"
import "core:strings"

get_sample_small_metadata :: proc() -> Flac_Metadata {

	result: Flac_Metadata
	result.path = ""
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
		),
	}
	vc_block := Vorbis_Comment_Block {
		vendor_string = strings.clone("reference libFLAC 1.3.3 20190804"),
		comments = map[string][dynamic]string {
			strings.clone("TITLE") = {strings.clone("empty")},
			strings.clone("COMMENT") = {
				strings.clone("fre:ac - free audio converter <https://www.freac.org/>"),
			},
			strings.clone("ENCODER") = {strings.clone("fre:ac v1.1.4")},
		},
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
		),
	}
	pad_block := Padding_Block {
		size = 512,
	}

	result.blocks = [dynamic]Block{si_block, vc_block, sk_block, pad_block}
	result.length = 726

	return result
}
