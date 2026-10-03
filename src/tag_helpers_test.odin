#+feature dynamic-literals
#+test

package roon_tagger

import "core:slice"
import "core:testing"

@(test)
successfully_split_multi_value :: proc(t: ^testing.T) {
	source := "John Coltrane ,Archie Shepp"
	expected := [dynamic]string{
		"John Coltrane",
		"Archie Shepp",
	}
	defer delete(expected)

	result := split_multi_value(source, ",", context.temp_allocator)
	defer delete(result)
	testing.expect(t, slice.equal(expected[:], result[:]))
	defer free_all(context.temp_allocator)
}
