#+feature using-stmt dynamic-literals
package metaflac_tests

import "../../src/metaflac"
import "core:testing"

@(test)
errors_on_empty_blocks :: proc(t: ^testing.T) {
	using metaflac

	meta := Flac_Metadata {
		path   = "",
		blocks = [dynamic]Block{},
	}

	result := validate_metadata(&meta)
	testing.expect_value(t, result, Flac_Error.Stream_Info_Error)
	release_metadata(&meta)
}

@(test)
errors_on_first_block_not_stream_info :: proc(t: ^testing.T) {
	using metaflac

	meta := Flac_Metadata {
		path   = "",
		blocks = [dynamic]Block{Padding_Block{}, Stream_Info_Block{}},
	}

	result := metaflac.validate_metadata(&meta)
	testing.expect_value(t, result, Flac_Error.Stream_Info_Error)
	release_metadata(&meta)
}
