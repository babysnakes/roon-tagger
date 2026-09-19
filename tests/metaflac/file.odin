#+feature using-stmt dynamic-literals
package metaflac_tests

import "core:fmt"
import "core:slice"
import "../../src/metaflac"
import "core:bytes"
import "core:testing"

@(test)
errors_on_empty_blocks :: proc(t: ^testing.T) {
	using metaflac

	meta := Flac_Metadata {
		path   = "",
		blocks = [dynamic]Block{},
	}

	result := validate_metadata(&meta)
	testing.expect_value(t, result, Flac_Error.Stream_Info_Error)
	release_metadata(&meta)
}

@(test)
errors_on_first_block_not_stream_info :: proc(t: ^testing.T) {
	using metaflac

	meta := Flac_Metadata {
		path   = "",
		blocks = [dynamic]Block{Padding_Block{}, Stream_Info_Block{}},
	}

	result := metaflac.validate_metadata(&meta)
	testing.expect_value(t, result, Flac_Error.Stream_Info_Error)
	release_metadata(&meta)
}

@(test)
save_metadata_and_read_it_back_should_be_equal :: proc(t: ^testing.T) {
	using metaflac

	meta := get_sample_small_metadata()
	defer release_metadata(&meta)
	data: [dynamic]u8
	defer delete(data)
	append(&data, "fLaC")
	blocks_data, length := write_blocks_data(&meta)
	defer delete(blocks_data)
	testing.expect_value(t, length, 38 + 150 + 22)
	append(&data, ..blocks_data[:])
	write_padding(512, &data)
	// simulate Audio data
	append(&data, "AUDIO_AUDIO_AUDIO_AUDIO")

	// convert to stream so we can read it back
	buf: bytes.Buffer
	defer bytes.buffer_destroy(&buf)
	bytes.buffer_init(&buf, data[:])
	stream := bytes.buffer_to_stream(&buf)

	result: Flac_Metadata
	defer release_metadata(&result)
	err1 := load_metadata_from_reader(stream, &result)
	ensure(err1 == nil)
	testing.expect(t, result.length == length + 512 + 4, "error calculating total metadata size") // metadata + new padding
	result_stream_info, si_ok := result.blocks[0].(Stream_Info_Block)
	meta_stream_info, _ := meta.blocks[0].(Stream_Info_Block)
	testing.expect(t, si_ok, "first block is not Stream_Info")
	testing.expect(t, slice.equal(meta_stream_info.data, result_stream_info.data), "stream info not equal")

	meta_vorbis, _ := meta.blocks[1].(Vorbis_Comment_Block)
	result_vorbis, rv_ok := result.blocks[1].(Vorbis_Comment_Block)
	testing.expect(t, rv_ok, "second block is not vorbis")
	for k, v in meta_vorbis.comments {
		testing.expect(t, slice.equal(v[:], result_vorbis.comments[k][:]))
	}

	pad_block, pb_ok := result.blocks[3].(Padding_Block)
	testing.expect(t, pb_ok, "last block is not padding")
	testing.expect_value(t, 512, pad_block.size)
}
