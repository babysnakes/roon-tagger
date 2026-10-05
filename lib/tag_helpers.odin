package lib

import "core:fmt"
import "core:mem"
import "core:mem/virtual"
import "core:slice"
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
