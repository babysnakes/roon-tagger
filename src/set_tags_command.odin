package roon_tagger

import "core:fmt"
import "core:strings"
import "metaflac"

set_tags_run :: proc(opts: Set_Tags_Options) -> bool {
	if len(opts.overflow) == 0 {
		fmt.eprintfln("At least one file should be specified")
		return false
	}

	for path in opts.overflow {
		meta, err := metaflac.load_metadata_from_file(path)
		if err != nil {
			fmt.eprintfln("Error extracting metadata from '%s': %v", path, err)
			return false
		}
		defer metaflac.release_metadata(&meta)
		tags := metaflac.vorbis_comment(&meta)

		// TODO: Need to also add credits for composer/conductor
		if len(opts.composer) > 0 do tags.comments[strings.clone(TAG_COMPOSER)] = split_multi_value(opts.composer, ",")
		if len(opts.conductor) > 0 do tags.comments[strings.clone(TAG_CONDUCTOR)] = split_multi_value(opts.conductor, ",")
		if opts.year > 0 do tags.comments[strings.clone(TAG_YEAR)] = int_to_tag_value(opts.year)

		save_err := metaflac.save_metadata(&meta)
		if save_err != nil {
			fmt.eprintfln("Error saving flac metadata '%s': %v", meta.path, save_err)
			return false
		}

	}

	return true
}
