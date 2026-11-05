package lib

import "core:fmt"
import "core:log"
import "core:mem"
import "core:mem/virtual"
import "core:slice"
import "core:strconv"
import "core:strings"
import "metaflac"

TAG_ARTIST :: "ARTIST"
TAG_ALBUM_ARTIST :: "ALBUMARTIST"
TAG_COMPOSER :: "COMPOSER"
TAG_CONDUCTOR :: "CONDUCTOR"
TAG_IMPORTDATE :: "IMPORTDATE"
TAG_RELEASEDATE :: "ORIGINALRELEASEDATE"
TAG_PERSONNEL :: "PEERSONNEL"
TAG_TITLE :: "TITLE"
TAG_YEAR :: "YEAR"
TAG_TRACK_NUMBER :: "TRACKNUMBER"
TAG_TRACK_TOTAL :: "TRACKTOTAL"
TAG_DISC_NUMBER :: "DISCNUMBER"
TAG_DISC_TOTAL :: "DISCTOTAL"

split_multi_value :: proc(s: string, sep: string = ";", allocator: mem.Allocator) -> [dynamic]string {
	result := make([dynamic]string, allocator)
	s := s
	for str in strings.split_iterator(&s, sep) {
		append(&result, strings.clone(strings.trim_space(str), allocator))
	}

	return result
}

// Adds a specific `PERSONNEL` credit from the supplied `person` / `credit` if not already exists.
tags_add_credit :: proc(person, credit: string, meta: ^metaflac.Flac_Metadata) -> metaflac.Metaflac_Error {
	allocator := virtual.arena_allocator(&meta.arena)
	credit_line := strings.concatenate({person, " - ", credit}, allocator) or_return
	vc := metaflac.vorbis_comment(meta)

	if credits, ok := &vc.comments[TAG_PERSONNEL]; ok {
		if slice.contains(credits[:], credit_line) do return nil
		append(credits, credit_line)
	} else {
		new_credits := make([dynamic]string, allocator) or_return
		append(&new_credits, credit_line)
		vc.comments[TAG_PERSONNEL] = new_credits
	}

	return nil
}

// Set/Replace the tag to a single value. Sets the tag to _upper_case_ and trims the value.
//
// **Important**: It's the caller responsibility to have the value lifetime
// longer then the lifetime of the supplied `Flac_Metadata`
tags_set_tag :: proc(tag, value: string, meta: ^metaflac.Flac_Metadata) -> metaflac.Metaflac_Error {
	allocator := virtual.arena_allocator(&meta.arena)
	vc := metaflac.vorbis_comment(meta)
	tag_name := strings.to_upper(tag, allocator)

	if tag_value, ok := &vc.comments[tag_name]; ok {
		clear(tag_value)
		append(tag_value, strings.trim_space(value))
	} else {
		new_value := make([dynamic]string, allocator) or_return
		append(&new_value, strings.trim_space(value))
		vc.comments[tag_name] = new_value
	}

	return nil
}

tags_set_int_tag :: proc(tag: string, value: int, meta: ^metaflac.Flac_Metadata) -> metaflac.Metaflac_Error {
	allocator := virtual.arena_allocator(&meta.arena)
	new_value := strings.clone(fmt.tprint(value), allocator)
	return tags_set_tag(tag, new_value, meta)
}

// Extracts the track, total_tracks, and disc number from the metadata.
// Returns '0' for every missing value.
// Returns `!ok` if value exists but not number.
get_track_ordering :: proc(meta: ^metaflac.Flac_Metadata) -> (track, total_tracks, disc: int, ok: bool) {
	log.debugf("Extracting ordering of track: %s", meta.path)
	vc := metaflac.vorbis_comment(meta)
	track = extract_num_tag(TAG_TRACK_NUMBER, vc) or_return
	total_tracks = extract_num_tag(TAG_TRACK_TOTAL, vc) or_return
	disc = extract_num_tag(TAG_DISC_NUMBER, vc) or_return
	ok = track >= 0 && total_tracks >= 0 && disc >= 0
	log.debugf("track: %d, total_tracks: %d, disc: %d, err: %d", track, total_tracks, disc, ok)
	return
}

// Compare the ordering of 2 tracks. Track without specified track/disc is considered as track 0 and disc 0.
//
// Note: ignores invalid values, treats them as 0
cmp_track_ordering :: proc(track1, track2: ^metaflac.Flac_Metadata) -> slice.Ordering {
	tr1, _, disc1, ok1 := get_track_ordering(track1)
	if !ok1 do tr1, disc1 = 0, 0
	tr2, _, disc2, ok2 := get_track_ordering(track2)
	if !ok2 do tr2, disc2 = 0, 0

	if tr1 == tr2 && disc1 == disc2 do return slice.Ordering.Equal
	if disc1 == disc2 {
		if tr1 > tr2 do return .Greater
		if tr1 < tr2 do return .Less
		unreachable()
	} else {
		if disc1 > disc2 do return .Greater
		if disc1 < disc2 do return .Less
		unreachable()
	}
}

extract_num_tag :: proc(tag: string, vorbis: ^metaflac.Vorbis_Comment_Block) -> (val: int, ok: bool) {
	raw := vorbis.comments[tag]
	if len(raw) == 0 do return 0, true
	return strconv.parse_int(raw[0])
}
