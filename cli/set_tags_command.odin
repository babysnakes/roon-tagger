package cli

import "core:fmt"
import "core:mem/virtual"
import "core:strings"
import "project:lib"
import "project:lib/dates"
import "project:lib/metaflac"

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
		allocator := virtual.arena_allocator(&meta.arena)
		defer metaflac.release_metadata(meta)
		tags := metaflac.vorbis_comment(meta)

		if len(opts.composer) > 0 {
			composers := lib.split_multi_value(opts.composer, ",", allocator)
			tags.comments[lib.TAG_COMPOSER] = composers
			for c in composers {
				lib.tags_add_credit(c, "Composer", meta)
			}
		}
		if len(opts.conductor) > 0 {
			conductors := lib.split_multi_value(opts.conductor, ",", allocator)
			tags.comments[lib.TAG_CONDUCTOR] = conductors
			for c in conductors {
				lib.tags_add_credit(c, "Conductor", meta)
			}
		}
		if opts.year > 0 do lib.tags_set_int_tag(lib.TAG_YEAR, opts.year, meta)
		if len(opts.title) > 0 do lib.tags_set_tag(lib.TAG_TITLE, opts.title, meta)

		i_date, i_ok := opts.import_date.?
		if i_ok {
			date_str := dates.as_string(i_date)
			defer delete(date_str)
			lib.tags_set_tag(lib.TAG_IMPORTDATE, strings.clone(date_str, allocator), meta)
		}
		r_date, r_ok := opts.release_date.?
		if r_ok {
			date_str := dates.as_string(r_date)
			defer delete(date_str)
			lib.tags_set_tag(lib.TAG_RELEASEDATE, strings.clone(date_str, allocator), meta)
		}

		save_err := metaflac.save_metadata(meta)
		if save_err != nil {
			fmt.eprintfln("Error saving flac metadata '%s': %v", meta.path, save_err)
			return false
		}

	}

	return true
}
