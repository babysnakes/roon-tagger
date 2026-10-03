#+feature dynamic-literals
#+test

package metaflac

import "core:bytes"
import "core:os"
import "core:slice"
import "core:strings"
import "core:testing"
import "core:mem/virtual"
import helpers "../test_helpers"

@(test)
errors_on_empty_blocks :: proc(t: ^testing.T) {
	meta := mk_empty_flac_metadata()
	result := validate_metadata(meta)
	testing.expect_value(t, result, Flac_Error.Stream_Info_Error)
	release_metadata(meta)
}

@(test)
errors_on_first_block_not_stream_info :: proc(t: ^testing.T) {
	meta := mk_empty_flac_metadata()
	append(&meta.blocks, Padding_Block{}, Stream_Info_Block{})

	result := validate_metadata(meta)
	testing.expect_value(t, result, Flac_Error.Stream_Info_Error)
	release_metadata(meta)
}

@(test)
save_metadata_and_read_it_back_should_be_equal :: proc(t: ^testing.T) {
	meta := get_sample_small_metadata()
	defer release_metadata(meta)
	data: [dynamic]u8
	defer delete(data)
	append(&data, "fLaC")
	blocks_data, length := write_blocks_data(meta)
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

	result := mk_empty_flac_metadata()
	defer release_metadata(result)
	err1 := load_metadata_from_reader(stream, result)
	ensure(err1 == nil)
	testing.expect(t, result.length == length + 512 + 4, "error calculating total metadata size") // metadata + new padding
	result_stream_info, si_ok := result.blocks[0].(Stream_Info_Block)
	meta_stream_info, _ := meta.blocks[0].(Stream_Info_Block)
	testing.expect(t, si_ok, "first block is not Stream_Info")
	testing.expect(
		t,
		slice.equal(meta_stream_info.data, result_stream_info.data),
		"stream info not equal",
	)

	meta_vorbis, _ := meta.blocks[1].(Vorbis_Comment_Block)
	result_vorbis, rv_ok := result.blocks[1].(Vorbis_Comment_Block)
	testing.expect(t, rv_ok, "second block is not vorbis")
	for k, v in meta_vorbis.comments {
		testing.expect(t, slice.equal(v[:], result_vorbis.comments[k][:]))
	}

	pad_block, pb_ok := result.blocks[3].(Padding_Block)
	testing.expect(t, pb_ok, "last block is not padding")
	testing.expect_value(t, 512, pad_block.size)
	defer free_all(context.temp_allocator)
}

@(test)
save_file_in_place_should_work_correctly :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "roon-tagger-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	flac_file := helpers.copy_fixture("minimal.flac", tmpdir, context.temp_allocator)

	meta_orig, meta_err := load_metadata_from_file(flac_file)
	allocator := virtual.arena_allocator(&meta_orig.arena)
	ensure(meta_err == nil)
	defer release_metadata(meta_orig)

	vorbis := vorbis_comment(meta_orig)
	test_value := mk_comment_value("MY_DATA", allocator)
	vorbis.comments[strings.clone("MY_TEST", allocator)] = test_value

	err1 := save_metadata(meta_orig)
	ensure(err1 == nil)
	meta_new, meta_new_err := load_metadata_from_file(flac_file)
	defer release_metadata(meta_new)
	ensure(meta_new_err == nil)
	new_vorbis := vorbis_comment(meta_new)

	testing.expect(t, slice.equal(vorbis.comments["MY_TEST"][:], new_vorbis.comments["MY_TEST"][:]))
}

@(test)
save_file_with_larger_metadata_works_correctly :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "roon-tagger-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	flac_file := helpers.copy_fixture("no-padding.flac", tmpdir, context.temp_allocator)

	meta_orig, meta_err := load_metadata_from_file(flac_file)
	allocator := virtual.arena_allocator(&meta_orig.arena)
	ensure(meta_err == nil)
	defer release_metadata(meta_orig)

	vorbis := vorbis_comment(meta_orig)
	test_value := mk_comment_value("MY_DATA", allocator)
	vorbis.comments[strings.clone("MY_TEST", allocator)] = test_value

	err1 := save_metadata(meta_orig)
	ensure(err1 == nil)
	meta_new, meta_new_err := load_metadata_from_file(flac_file)
	defer release_metadata(meta_new)
	ensure(meta_new_err == nil)
	new_vorbis := vorbis_comment(meta_new)

	testing.expect(t, slice.equal(vorbis.comments["MY_TEST"][:], new_vorbis.comments["MY_TEST"][:]))
	testing.expect(t, meta_new.length > meta_orig.length)
}

// Generate `Flac_Metadata` that is managed by arena allocator.
mk_empty_flac_metadata :: proc() -> ^Flac_Metadata {
	meta, err := virtual.arena_growing_bootstrap_new(Flac_Metadata, "arena")
	ensure(err == nil)
	allocator := virtual.arena_allocator(&meta.arena)
	path, err1 := strings.clone("", allocator)
	ensure(err1 == nil)
	blocks, err2 := make([dynamic]Block, allocator)
	ensure(err2 == nil)
	meta.path = path
	meta.blocks = blocks
	return meta
}
