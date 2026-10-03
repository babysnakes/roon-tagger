#+feature dynamic-literals
#+test

package roon_tagger

import "core:fmt"
import "core:log"
import "core:slice"
import "core:testing"
import "metaflac"

@(test)
t_successfully_split_multi_value :: proc(t: ^testing.T) {
	source := "John Coltrane ,Archie Shepp"
	expected := [dynamic]string{"John Coltrane", "Archie Shepp"}
	defer delete(expected)

	result := split_multi_value(source, ",", context.temp_allocator)
	defer delete(result)
	testing.expect(t, slice.equal(expected[:], result[:]))
	defer free_all(context.temp_allocator)
}

@(test)
t_tags_add_credit_adds_correct_credit_on_empty_personnel :: proc(t: ^testing.T) {
	meta := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(meta)
	err := tags_add_credit("John Coltrane", "Saxophone", meta)
	ensure(err == nil)
	vc := metaflac.vorbis_comment(meta)
	testing.expect_value(t, len(vc.comments[TAG_PERSONNEL]), 1)
	testing.expect_value(t, vc.comments[TAG_PERSONNEL][0], "John Coltrane - Saxophone")
}

@(test)
t_tags_add_credit_adds_correct_credit_when_not_exists :: proc(t: ^testing.T) {
	meta := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(meta)
	err := tags_add_credit("Archie Shepp", "Saxophone", meta)
	ensure(err == nil)
	err = tags_add_credit("Dave Holland", "Bass", meta)
	ensure(err == nil)
	err = tags_add_credit("John Coltrane", "Saxophone", meta)
	ensure(err == nil)
	vc := metaflac.vorbis_comment(meta)
	testing.expect_value(t, len(vc.comments[TAG_PERSONNEL]), 3)
	testing.expect(t, slice.contains(vc.comments[TAG_PERSONNEL][:], "John Coltrane - Saxophone"))
}

@(test)
t_tags_add_credit_does_not_cause_duplicate_credits :: proc(t: ^testing.T) {
	meta := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(meta)
	err := tags_add_credit("Archie Shepp", "Saxophone", meta)
	ensure(err == nil)
	err = tags_add_credit("John Coltrane", "Saxophone", meta)
	ensure(err == nil)
	err = tags_add_credit("John Coltrane", "Saxophone", meta)
	ensure(err == nil)
	vc := metaflac.vorbis_comment(meta)
	testing.expect_value(t, len(vc.comments[TAG_PERSONNEL]), 2)
	testing.expect(t, slice.contains(vc.comments[TAG_PERSONNEL][:], "John Coltrane - Saxophone"))
}

@(test)
t_tags_set_tag_behaves_correctly :: proc(t: ^testing.T) {
	meta := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(meta)
	vc := metaflac.vorbis_comment(meta)

	log.info("testing: uppercase tag names ...")
	err := tags_set_tag("subject1", "my subject", meta)
	fmt.ensuref(err == nil, "tags_set_tag error: %v", err)
	testing.expect_value(t, 1, len(vc.comments["SUBJECT1"]))
	testing.expect_value(t, "my subject", vc.comments["SUBJECT1"][0])

	log.info("testing: trims value ...")
	err = tags_set_tag("SUBJECT2", "\tmy subject ", meta)
	fmt.ensuref(err == nil, "tags_set_tag error: %v", err)
	testing.expect_value(t, 1, len(vc.comments["SUBJECT2"]))
	testing.expect_value(t, "my subject", vc.comments["SUBJECT2"][0])

	log.info("testing: replacing values ...")
	err = tags_set_tag("SUBJECT1", "my other subject", meta)
	fmt.ensuref(err == nil, "tags_set_tag error: %v", err)
	testing.expect_value(t, 1, len(vc.comments["SUBJECT1"]))
	testing.expect_value(t, "my other subject", vc.comments["SUBJECT1"][0])
}
