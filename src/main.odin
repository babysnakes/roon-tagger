package roon_tagger

import "core:flags"
import "core:fmt"
import "core:mem"
import "core:os"
import "core:sys/windows"

main :: proc() {
	verbose: bool

	// track memory error, not sure if you must also add -sanitize:address...
	when ODIN_DEBUG {
		track: mem.Tracking_Allocator
		mem.tracking_allocator_init(&track, context.allocator)
		context.allocator = mem.tracking_allocator(&track)

		defer {
			if len(track.allocation_map) > 0 {
				total: u32
				for _, entry in track.allocation_map {
					total += u32(entry.size)
				}

				if !verbose do fmt.printfln("\n\n Memory leaks of %d bytes exists! Run with -v for details", total)
				if verbose do fmt.eprintfln("\n\nSee memory leaks below (total: %d bytes):", total)
				for _, entry in track.allocation_map {
					if verbose do fmt.eprintf("%v leaked %v bytes\n", entry.location, entry.size)
				}
			}
			mem.tracking_allocator_destroy(&track)
		}
	}

	// Display UTF-8 characters correctly in the console
	when ODIN_OS == .Windows {
		windows.SetConsoleOutputCP(.UTF8)
	}

	args: []string
	if (len(os.args) > 1) {
		args = os.args[:2]
	} else {
		args = os.args
	}

	main_opts: Main_Options
	flags.register_type_setter(main_command_type_setter)
	flags.parse_or_exit(&main_opts, args, .Unix)

	// now we have to parse again because the base command has passed
	switch main_opts.command {
	case .view:
		flags.register_type_setter(nil)
		flags.register_flag_checker(view_cmd_flag_checker)
		opts: View_Options
		flags.parse_or_exit(&opts, os.args, .Unix)
		verbose = opts.verbose
		ensure(view_flac(opts.file))
	case .set_tags:
		flags.register_type_setter(set_tags_command_type_setter)
		opts: Set_Tags_Options
		flags.parse_or_exit(&opts, os.args, .Unix)
		defer delete(opts.overflow)
		verbose = opts.verbose
		ensure(set_tags_run(opts))
	}
}
