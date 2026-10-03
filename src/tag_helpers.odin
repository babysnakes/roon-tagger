package roon_tagger

import "core:mem"
import "core:fmt"
import "core:strings"

TAG_ARTIST :: "ARTIST"
TAG_ALBUM_ARTIST :: "ALBUMARTIST"
TAG_COMPOSER :: "COMPOSER"
TAG_CONDUCTOR :: "CONDUCTOR"
TAG_YEAR :: "YEAR"

split_multi_value :: proc(s: string, sep: string = ";", allocator: mem.Allocator) -> [dynamic]string {
	result := make([dynamic]string, allocator)
	s := s
	for str in strings.split_iterator(&s, sep) {
		append(&result, strings.clone(strings.trim_space(str), allocator))
	}

	return result
}

int_to_tag_value :: proc(n: int) -> (result: [dynamic]string) {
	append(&result, strings.clone(fmt.tprint(n)))
	return
}
