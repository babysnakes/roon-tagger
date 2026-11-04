#+feature dynamic-literals using-stmt
#+test

package lib

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

@(test)
t_get_track_ordering_behaves_correctly :: proc(t: ^testing.T) {
	meta := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(meta)

	log.info("testing: empty track should return zero values")
	tr1, tt1, cd1, ok1 := get_track_ordering(meta)
	testing.expect_value(t, tr1, 0)
	testing.expect_value(t, tt1, 0)
	testing.expect_value(t, cd1, 0)
	testing.expect_value(t, ok1, true)

	log.info("testing: fully specified ordering should return correctly")
	tags_set_int_tag(TAG_TRACK_NUMBER, 1, meta)
	tags_set_int_tag(TAG_TRACK_TOTAL, 1, meta)
	tags_set_int_tag(TAG_DISC_NUMBER, 1, meta)
	tr2, tt2, cd2, ok2 := get_track_ordering(meta)
	testing.expect_value(t, tr2, 1)
	testing.expect_value(t, tt2, 1)
	testing.expect_value(t, cd2, 1)
	testing.expect_value(t, ok2, true)

	log.info("testing: negative value returns error")
	tags_set_int_tag(TAG_TRACK_NUMBER, -1, meta)
	tr3, _, _, ok3 := get_track_ordering(meta)
	testing.expect_value(t, tr3, -1) // just to make sure test is correct
	testing.expect_value(t, ok3, false)
	tags_set_int_tag(TAG_TRACK_NUMBER, 0, meta) // reset

	log.info("testing: invalid value returns error")
	tags_set_tag(TAG_DISC_NUMBER, "invalid", meta)
	_, _, _, ok4 := get_track_ordering(meta)
	testing.expect_value(t, ok4, false)
}
@(test)
t_cmp_track_ordering_behaves_correctly :: proc(t: ^testing.T) {
	using slice.Ordering

	track1 := metaflac.get_sample_small_metadata()
	track2 := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(track1)
	defer metaflac.release_metadata(track2)

	log.info("testing: tracks with unspecified ordering are equal")
	r1 := cmp_track_ordering(track1, track2)
	testing.expect_value(t, r1, Equal)

	log.info("testing: tracks on the same disc are handled according to track order")
	tags_set_int_tag(TAG_TRACK_NUMBER, 1, track1)
	tags_set_int_tag(TAG_TRACK_NUMBER, 2, track2)
	r2 := cmp_track_ordering(track1, track2)
	testing.expect_value(t, r2, Less)
	tags_set_int_tag(TAG_DISC_NUMBER, 3, track1)
	r3 := cmp_track_ordering(track1, track2)
	testing.expect_value(t, r3, Greater)

	log.info("testing: tracks on different discs are treated according to disc order")
	tags_set_int_tag(TAG_TRACK_NUMBER, 2, track1)
	tags_set_int_tag(TAG_DISC_NUMBER, 1, track1)
	tags_set_int_tag(TAG_TRACK_NUMBER, 1, track2)
	tags_set_int_tag(TAG_DISC_NUMBER, 2, track2)
	r4 := cmp_track_ordering(track1, track2)
	testing.expect_value(t, r4, Less)
	tags_set_int_tag(TAG_DISC_NUMBER, 3, track1)
	r5 := cmp_track_ordering(track1, track2)
	testing.expect_value(t, r5, Greater)
}
