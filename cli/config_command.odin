package cli

import "core:fmt"
import "core:log"
import "core:strings"
import "project:ext/colors"
import "project:lib"

config_run :: proc(opts: Config_Options) -> Cli_Error {
	editor_defined := len(opts.editor) > 0
	edited: bool

	if opts.no_editor && editor_defined {
		return lib.General_RT_Error{"specifying both `no-editor` and `editor` does not make sense"}
	}

	config_dir := get_config_dir() or_return
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
		cmd := extract_editor(opts.editor)
		config.editor = cmd
		edited = true
	case opts.show:
	// NOOP, but treat it as an operation to allow show without any other operation.
	case:
		return lib.General_RT_Error{"no config operation specified"}
	}

	if opts.show {
		fmt.printfln("\n%s %T", colors.blue("Config:"), config)
		fmt.printfln("  * %s: %v", colors.blue("editor"), config.editor)
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

// Extracts an editor from a string.
//
// Note: forwards the code location in case of panic
extract_editor :: proc(editor: string, loc := #caller_location) -> []string {
	cmd, s_err := strings.split(editor, ",", context.temp_allocator)
	if s_err != .None do fmt.panicf("[Bug]: memory error: %v", s_err, loc = loc)
	for &c in cmd {
		c = strings.trim_space(c)
	}
	return cmd
}
