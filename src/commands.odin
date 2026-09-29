package roon_tagger

import "base:runtime"
import "core:fmt"
import "core:os"
import "dates"

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

Set_Tags_Options :: struct {
	command:      string `args:"pos=0,required" usage:"set-tags"`,
	import_date:  dates.Date_Tag `args:"name=I" usage:"Import date (format: 'yyyy-mm-dd' or 'today')"`,
	release_date: dates.Date_Tag `args:"name=R" usage:"Import date (format: 'yyyy-mm-dd' or 'today')"`,
	composer:     string `usage:"Add composer tag and credit, Separate multiple values by comma"`,
	conductor:    string `usage:"Add conductor tag and credit, Separate multiple values by comma"`,
	year:         int `usage:"Year of release"`,
	verbose: bool `args:"name=v" usage:"Display memory leaks details"`,
	overflow:     [dynamic]string `usage:"Files to process"`,
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
			error = fmt.tprintf("File '%s' does not exist!", file)
		}
	}

	return
}

set_tags_command_type_setter :: proc(
	data: rawptr,
	data_type: typeid,
	unparsed_value: string,
	args_tag: string,
) -> (
	error: string,
	handled: bool,
	alloc_error: runtime.Allocator_Error,
) {
	if data_type == dates.Date_Tag {
		handled = true
		ptr := cast(^dates.Date_Tag)data
		dt, err := dates.parse_date(unparsed_value)
		if err != .None {
			error = fmt.tprintf("Error parsing date: %v", err)
		} else {
			ptr^ = dt
		}
	}

	return
}
