package blocks

import "../../src/metaflac"
import "core:testing"

@(test)
valid_comments :: proc(t: ^testing.T) {
	valid_comments: []string = {"~~~~~~~~~~", "`Hello World`"}
	for c in valid_comments {
		testing.expectf(t, metaflac.validate_comment(c), "Expected %s to be valid", c)
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
		testing.expectf(t, metaflac.validate_comment(c) == false, "Expected '%s' to be invalid", c)
	}
}
