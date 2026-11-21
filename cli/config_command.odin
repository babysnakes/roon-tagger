package cli

import "base:runtime"
import "core:fmt"
import "core:io"
import "core:log"
import "core:os"
import "core:path/filepath"
import "core:strings"
import "project:ext/colors"
import "project:lib"

config_run :: proc(opts: Config_Options) -> Cli_Error {
	editor_defined := len(opts.editor) > 0
	edited: bool

	if opts.no_editor && editor_defined {
		return lib.General_RT_Error{"specifying both `no-editor` and `editor` does not make sense"}
	}

	// TODO (haim) consider making it more explicit
	config_dir_name := "roon-tagger"
	when ODIN_DEBUG {
		config_dir_name = "roon-tagger-dev"
	}

	user_config_dir, u_err := os.user_config_dir(context.temp_allocator)
	switch e in u_err {
	case runtime.Allocator_Error:
		fmt.panicf("[Bug]: memory error: %v", e)
	case os.General_Error, io.Error, os.Platform_Error:
		msg := fmt.tprintf("fetching config dir: %v", u_err)
		log.error("Error", msg)
		return lib.General_RT_Error{msg}
	}
	config_dir, d_err := filepath.join({user_config_dir, config_dir_name}, context.temp_allocator)
	if d_err != .None do fmt.panicf("[Bug]: memory error: %v", d_err)

	config, c_err := load_config(config_dir, context.temp_allocator)
	if c_err != nil {
		msg := fmt.tprintf("loading config: %v", c_err)
		log.error(msg)
		return lib.General_RT_Error{msg}
	}

	switch {
	case opts.no_editor:
		config.editor = nil
		edited = true
	case editor_defined:
		cmd, s_err := strings.split(opts.editor, ",", context.temp_allocator)
		if s_err != .None do fmt.panicf("[Bug]: memory error: %v", s_err)
		for &c in cmd {
			c = strings.trim_space(c)
		}
		config.editor = cmd
		edited = true
	case opts.show:
	// NOOP, but treat it as an operation to allow show without any other operation.
	case:
		return lib.General_RT_Error{"no config operation specified"}
	}

	if opts.show {
		// fmt.printfln("%s %v\n", colors.blue("Config:"), config)
		print_config(config)
	}

	if edited {
		s_err := save_config(&config, config_dir)
		if s_err != nil {
			msg := fmt.tprintf("saving config: %v", s_err)
			log.error(msg)
			return lib.General_RT_Error{msg}
		}
	}

	fmt.printfln("\n%s: operation finished successfully", colors.green("Success", context.temp_allocator))
	return nil
}

print_config :: proc(config: Cli_Config_V1) {
	fmt.printfln("\n%s %T", colors.blue("Config:"), config)
	fmt.printfln("  * %s: %v", colors.blue("editor"), config.editor)
}
