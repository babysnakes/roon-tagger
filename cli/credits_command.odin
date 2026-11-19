package cli

import "core:fmt"
import "core:log"
import "core:slice"
import "core:strings"
import "project:ext/colors"
import "project:lib"
import "project:lib/metaflac"

credits_run :: proc(opts: Credits_Options) -> Cli_Error {
	// TODO (haim) Add count for number of credits added/removed.

	if len(opts.overflow) == 0 {
		log.error("At least one file should be provided")
		return lib.General_RT_Error{"At least one file should be provided"}
	}

	additions: int
	deletions: int

	roles := #load("../resources/roles.txt", string)
	lines: []string
	defer if len(lines) > 0 do delete(lines)

	if len(opts.add) > 0 && !opts.skip_validation {
		lines = strings.split_lines(roles)
	}

	for path, idx in opts.overflow {
		meta, err := metaflac.load_metadata_from_file(path)
		if err != nil {
			msg := fmt.tprintf("Error extracting metadata from '%s': %v", path, err)
			log.error(msg)
			return lib.General_RT_Error{msg}
		}
		defer metaflac.release_metadata(meta)

		// by now we assume that add/del input are formatted correctly, only trimming might be needed.
		for s in opts.add {
			parts, err1 := strings.split_n(s, ":", 2)
			if err1 != .None do fmt.panicf("[Bug]: memory error: %v", err1)
			defer delete(parts)
			roles, err2 := strings.split(parts[1], ",")
			if err1 != .None do fmt.panicf("[Bug]: memory error: %v", err2)
			defer delete(roles)
			for role in roles {
				t_role := strings.trim_space(role)
				if !opts.skip_validation {
					if !slice.contains(lines, t_role) {
						msg := fmt.tprintf(`Role "%s" is not a valid role according to Roon's wiki`, role)
						return lib.General_RT_Error{msg}
					}
				}
				r_err := lib.tags_add_credit(strings.trim_space(parts[0]), strings.trim_space(role), meta)
				if r_err != nil {
					log.error("Error adding credit:", r_err)
					return r_err
				}

				if idx == 0 do additions += 1
			}
		}

		for s in opts.del {
			parts, err1 := strings.split_n(s, ":", 2)
			if err1 != .None do fmt.panicf("[Bug]: memory error: %v", err1)
			defer delete(parts)
			roles, err2 := strings.split(parts[1], ",")
			if err1 != .None do fmt.panicf("[Bug]: memory error: %v", err2)
			defer delete(roles)
			for role in roles {
				r_err := lib.tags_del_credit(strings.trim_space(parts[0]), strings.trim_space(role), meta)
				if r_err != nil {
					log.error("Error deleting credit:", r_err)
					return r_err
				}

				if idx == 0 do deletions += 1
			}
		}

		for s in opts.del_raw {
			err := lib.tags_del_credit_raw(s, meta)
			if err != nil {
				log.error("Error deleting credit:", err)
				return err
			}

			if idx == 0 do deletions += 1
		}

		save_err := metaflac.save_metadata(meta)
		if save_err != nil {
			msg := fmt.tprintf("Error saving flac metadata '%s': %v", meta.path, save_err)
			log.error(msg)
			return lib.General_RT_Error{msg}
		}
	}

	fmt.printfln(
		"%s: Successfully added %d %s and deleted %d %s",
		colors.green("Success", context.temp_allocator),
		additions,
		lib.pluralize("credit", additions),
		deletions,
		lib.pluralize("credit", deletions),
	)

	return nil
}
