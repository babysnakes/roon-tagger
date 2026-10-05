#+test

package metaflac

import "core:encoding/endian"
import "core:fmt"
import "core:testing"

@(test)
valid_comments :: proc(t: ^testing.T) {
	valid_comments: []string = {"~~~~~~~~~~", "`Hello World`"}
	for c in valid_comments {
		testing.expectf(t, validate_comment(c), "Expected %s to be valid", c)
	}
}


@(test)
invalid_comments :: proc(t: ^testing.T) {
	invalid_comments := []string {
		"\u201CHello there!\u201D",
		"It\u2019s a nice day.",
		"Resume\u00A0text here.",
		"Caf\u00E9 is open.",
		"Click\u200Bhere.",
		"Price: 100\u20AC.",
		"Wait\u2026 what?",
		"Hello!\tWorld",
		"Hello!=World",
		"Na\u00EFve concept.",
		"Done.\n",
	}
	for c in invalid_comments {
		testing.expectf(t, validate_comment(c) == false, "Expected '%s' to be invalid", c)
	}
}

@(test)
zero_length_vorbis_comment_is_illegal :: proc(t: ^testing.T) {
	zero_length_header := make([]u8, 8, context.temp_allocator) // we actually need 4 bytes but we'll leave some spare just in case
	ensure(endian.put_u32(zero_length_header[0:4], .Little, 0))
	_, err := parse_vorbis_comment(zero_length_header, context.temp_allocator)
	defer free_all(context.temp_allocator)
	testing.expect_value(t, err, Block_Error.Vorbis_Comment_Parse_Error)
}

@(test)
vorbis_comment_returns_instance_even_if_not_exists_originally :: proc(t: ^testing.T) {
	meta := mk_empty_flac_metadata()
	append(&meta.blocks, Stream_Info_Block{}, Padding_Block{}) // no vorbis comment
	vc := vorbis_comment(meta) // this is enough to test, it would fail it not returning the right type
}
