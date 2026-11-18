package cli

import "base:runtime"
import "core:fmt"
import "core:log"
import "core:mem"
import "core:os"
import "core:slice"
import "core:sys/windows"
import flags "project:ext/pflags"
import "project:lib"

LOG_FILE :: "roon-tagger.log"

main :: proc() {
	// TODO (haim) this is a terrible hack but it will work for now until I'll implement main command options correctly
	verbose := slice.contains(os.args, "-v")

	// track memory error,
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

	// Logging
	logger := log.nil_logger()
	if verbose {
		handle, err := os.open(LOG_FILE, os.O_RDWR | os.O_APPEND | os.O_CREATE)
		fmt.ensuref(err == nil, "Error opening log file: %v", err)
		logger = log.create_file_logger(handle)
		logger.lowest_level = .Debug
	}
	defer if verbose do log.destroy_file_logger(logger)
	context.logger = logger

	// Display UTF-8 characters correctly in the console
	// TODO (haim) - consider removing once using a proper terminal UI library.
	when ODIN_OS == .Windows {
		windows.SetConsoleOutputCP(.UTF8)
	}

	err := run()
	if err != nil {
		report_cli_error(err)
		os.exit(1)
	}
}

run :: proc() -> Cli_Error {
	args: []string
	if (len(os.args) > 1) {
		args = os.args[:2]
	} else {
		args = os.args
	}

	// TODO (haim) this should probably removed once we finalize argument
	//      parsing, we should manually check for subcommand.
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
		view_flac(opts.file) or_return
	case .set_tags:
		flags.register_type_setter(set_tags_command_type_setter)
		opts: Set_Tags_Options
		flags.parse_or_exit(&opts, os.args, .Unix)
		defer delete(opts.overflow)
		set_tags_run(opts) or_return
	case .edit_titles:
		flags.register_type_setter(nil) // reset type setter
		opts: Edit_Titles_Options
		flags.parse_or_exit(&opts, os.args, .Unix)
		defer delete(opts.overflow)
		edit_titles_run(opts) or_return
	case .config:
		flags.register_type_setter(nil) // reset type setter
		opts: Config_Options
		flags.parse_or_exit(&opts, os.args, .Unix)
		config_run(opts) or_return
	case .credits:
		flags.register_type_setter(nil)
		flags.register_flag_checker(credits_cmd_flag_checker)
		opts: Credits_Options
		flags.parse_or_exit(&opts, os.args, .Unix)
		defer {
			delete(opts.add)
			delete(opts.del)
			delete(opts.del_raw)
			delete(opts.overflow)
		}
		credits_run(opts) or_return
	}
	return nil
}

report_cli_error :: proc(err: Cli_Error) {
	switch e in err {
	case lib.General_RT_Error:
		fmt.eprintln(e.error)
	case os.Error:
		fmt.eprintfln("OS Error: %v", e)
	case runtime.Allocator_Error:
		fmt.panicf("[BUG] memory error: %v", e)
	case lib.Lib_Error:
		report_lib_error(e)
	}
}

report_lib_error :: proc(err: lib.Lib_Error) {
	switch e in err {
	case mem.Allocator_Error:
		fmt.panicf("[BUG] memory error: %v", e)
	case lib.General_RT_Error:
		fmt.eprintln(e.error)
	}
}
