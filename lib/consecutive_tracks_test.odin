#+test
package lib

import "core:slice"
import "core:testing"
import "metaflac"

@(test)
t_to_consecutive_tracks_behaves_correctly :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	track1 := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(track1)
	track2 := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(track2)
	track3 := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(track3)
	track4 := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(track4)
	track5 := metaflac.get_sample_small_metadata()
	defer metaflac.release_metadata(track5)

	if ok := set_ordering_for_track(1, 3, 1, track1); ok != nil do testing.fail(t)
	if ok := set_ordering_for_track(2, 3, 1, track2); ok != nil do testing.fail(t)
	if ok := set_ordering_for_track(3, 3, 1, track3); ok != nil do testing.fail(t)
	if ok := set_ordering_for_track(1, 5, 2, track4); ok != nil do testing.fail(t)
	if ok := set_ordering_for_track(2, 5, 2, track5); ok != nil do testing.fail(t)

	consecutive: []^metaflac.Flac_Metadata = {track1, track2, track3, track4}
	non_consecutive1: []^metaflac.Flac_Metadata = {track1, track2, track3, track5}
	non_consecutive2: []^metaflac.Flac_Metadata = {track1, track3}
	expected: Consecutive_Tracks = {track1, track2, track3, track4}

	res1, ok1 := to_consecutive_tracks(consecutive, context.temp_allocator)
	testing.expect_value(t, ok1, true)
	testing.expect(t, slice.equal(res1, expected))
	_, ok2 := to_consecutive_tracks(non_consecutive1, context.temp_allocator)
	testing.expect_value(t, ok2, false)
	_, ok3 := to_consecutive_tracks(non_consecutive1, context.temp_allocator)
	testing.expect_value(t, ok3, false)
}

set_ordering_for_track :: proc(
	track_num, total_track, disc: int,
	track: ^metaflac.Flac_Metadata,
) -> metaflac.Metaflac_Error {
	tags_set_int_tag(TAG_TRACK_NUMBER, track_num, track) or_return
	tags_set_int_tag(TAG_TRACK_TOTAL, total_track, track) or_return
	tags_set_int_tag(TAG_DISC_NUMBER, disc, track) or_return
	return nil
}
