package lib

import "core:log"
import "core:slice"
import "metaflac"

Consecutive_Tracks :: distinct []^metaflac.Flac_Metadata

to_consecutive_tracks :: proc(files: []^metaflac.Flac_Metadata, allocator := context.allocator) -> (fs: Consecutive_Tracks, ok: bool) {
	result := slice.clone(files, allocator)
	slice.sort_by_cmp(result, cmp_track_ordering)

	for cur, idx in result {
		if idx + 1 < len(files) {
			next := files[idx + 1]
			validate_consecutive_track(cur, next) or_return
		}
	}

	return Consecutive_Tracks(result), true
}

@(private="file")
// Note, assumes ordered tracks
validate_consecutive_track :: proc(track1, track2: ^metaflac.Flac_Metadata) -> bool {
	log.debugf("Validating consecutiveness on %s and %s", track1.path, track2.path)
	tr1, tt1, disc1 := get_track_ordering(track1) or_return
	tr2, tt2, disc2 := get_track_ordering(track2) or_return
	log.debugf("track1: track: %d, total: %d, disk: %d", tr1, tt1, disc1)
	log.debugf("track2: track: %d, total: %d, disk: %d", tr2, tt2, disc2)

	if disc1 == disc2 do return tr2 - tr1 == 1
	if disc2 - disc1 > 1 do return false
	return tr1 == tt1 && tr2 == 1
}
