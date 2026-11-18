package lib

import "core:strings"
// A naive pluralize function that adds an 's' the word if not pluralize form provided.
// Note: uses `context.temp_allocator` by default.
pluralize :: proc(s: string, count: int, plural: Maybe(string) = nil, allocator := context.temp_allocator) -> string {
	pl, provided := plural.?
	switch {
	case count == 1:
		return strings.clone(s, allocator)
	case provided:
		return strings.clone(pl, allocator)
	case:
		return strings.concatenate({s, "s"}, allocator)
	}
}
