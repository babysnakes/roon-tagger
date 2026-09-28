package roon_tagger

import "core:fmt"
import "base:runtime"
import "core:os"

// Available sub-commands
Command :: enum {
	view,
	set_tags,
}

// Main Command
Main_Options :: struct {
	command:  Command `args:"pos=0,required" usage:"Valid commands: view | set-tags"`,
	overflow: [dynamic]string `usage:"Command arguments"`,
}

View_Options :: struct {
	command: string `args:"pos=0,required" usage:"view"`,
	file:    string `args:"pos=1,required" usage:"Flac file to view"`,
	verbose: bool `args:"name=v" usage:"Display memory leaks details"`,
}

main_command_type_setter :: proc(
	data: rawptr,
	data_type: typeid,
	unparsed_value: string,
	args_tag: string,
) -> (
	error: string,
	handled: bool,
	alloc_error: runtime.Allocator_Error,
) {
	if data_type == Command {
		handled = true
		ptr := cast(^Command)data

		if unparsed_value == "view" {
			ptr^ = .view
		} else if unparsed_value == "set-tags" {
			ptr^ = .set_tags
		} else {
			error = "Invalid command"
		}

	}
	return
}

view_cmd_flag_checker :: proc(
	model: rawptr,
	name: string,
	value: any,
	args_tag: string,
) -> (
	error: string,
) {
	if name == "file" {
		file := value.(string)
		if !os.exists(file) {
			error = fmt.tprintf("file '%s' does not exist!", file)
		}
	}

	return
}
