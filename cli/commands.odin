package cli

import "base:runtime"
import "core:fmt"
import "core:os"
import "core:strings"
import "project:lib/dates"

// Available sub-commands
Command :: enum {
	view,
	set_tags,
	edit_titles,
	config,
	credits,
}

// Main Command
Main_Options :: struct {
	command:  Command `args:"pos=0,required" usage:"Valid commands: view | set-tags | edit-titles | config | credits"`,
	overflow: [dynamic]string `usage:"Command arguments"`,
}

View_Options :: struct {
	command: string `args:"pos=0,required" usage:"view"`,
	file:    string `args:"pos=1,required" usage:"Flac file to view"`,
	verbose: bool `args:"short=v" usage:"Display memory leaks details"`,
}

Set_Tags_Options :: struct {
	command:      string `args:"pos=0,required" usage:"set-tags"`,
	title:        string `usage:"The title of the song"`,
	import_date:  Maybe(dates.Date_Tag) `args:"short=I" usage:"Import date (format: 'yyyy-mm-dd' or 'today')"`,
	release_date: Maybe(dates.Date_Tag) `args:"short=R" usage:"Import date (format: 'yyyy-mm-dd' or 'today')"`,
	year:         int `usage:"Year of release"`,
	composer:     string `usage:"Add composer tag and credit, Separate multiple values by comma"`,
	conductor:    string `usage:"Add conductor tag and credit, Separate multiple values by comma"`,
	verbose:      bool `args:"short=v" usage:"Display memory leaks details"`,
	overflow:     [dynamic]string `usage:"Files to process"`,
}

Edit_Titles_Options :: struct {
	command:  string `args:"pos=0,required" usage:"edit-titles"`,
	wait:     bool `args:"short=w" usage:"Invoke editor externally, Wait for ENTER to parse new titles"`,
	editor:   string `usage:"Use specified editor to edit titles - command arguments should be separated by commas"`,
	verbose:  bool `args:"short=v" usage:"Display memory leaks details"`,
	overflow: [dynamic]string `usage:"Files to process"`,
}

Config_Options :: struct {
	command:   string `args:"pos=0,required" usage:"config"`,
	editor:    string `usage:"Comma separated list of command + options (e.g., \"code,-w,-n\"), should accept file as last argument"`,
	no_editor: bool `args:"short=E" usage:"Delete saved editor"`,
	show:      bool `usage:"Display configuration"`,
	verbose:   bool `args:"short=v" usage:"Display memory leaks details"`,
}

Credits_Options :: struct {
	command:         string `args:"pos=0,required" usage:"credits"`,
	add:             [dynamic]string `usage:"Add credit in the form of \"person:role1,role2\", multiple occurrences allowed."`,
	del:             [dynamic]string `usage:"Delete credit(s) in the form of \"person:role1,role2\", multiple occurrences allowed."`,
	del_raw:         [dynamic]string `usage:"Delete an exact credit entry, multiple occurrences allowed."`,
	skip_validation: bool `usage:"Don't validate credits against Roon's supported roles"`,
	verbose:         bool `args:"short=v" usage:"Display memory leaks details"`,
	overflow:        [dynamic]string `usage:"Files to process"`,
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

		switch unparsed_value {
		case "view":
			ptr^ = .view
		case "set-tags":
			ptr^ = .set_tags
		case "edit-titles":
			ptr^ = .edit_titles
		case "config":
			ptr^ = .config
		case "credits":
			ptr^ = .credits
		case:
			error = "Invalid command"
		}
	}
	return
}

view_cmd_flag_checker :: proc(model: rawptr, name: string, value: any, args_tag: string) -> (error: string) {
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
	if data_type == Maybe(dates.Date_Tag) {
		handled = true
		ptr := cast(^Maybe(dates.Date_Tag))data
		// TODO (haim) consider returning string as error for easier reporting
		dt, err := dates.parse_date(unparsed_value)
		if err != .None {
			error = fmt.tprintf("Error parsing date: %v", err)
		} else {
			ptr^ = dt
		}
	}

	return
}

credits_cmd_flag_checker :: proc(model: rawptr, name: string, value: any, args_tag: string) -> (error: string) {
	switch name {
	case "add", "del":
		credits := value.([dynamic]string)
		lc := len(credits)
		parts, err1 := strings.split_n(credits[lc - 1], ":", 2)
		if err1 != .None do fmt.panicf("[Bug]: memory error: %v", err1)
		defer delete(parts)
		if len(strings.trim_space(parts[0])) == 0 do return fmt.tprintf("--%s: missing name", name)
		if len(parts) < 2 do return fmt.tprintf("--%s: missing credit roles", name)
		roles, err2 := strings.split(parts[1], ",")
		if err2 != .None do fmt.panicf("[Bug]: memory error: %v", err2)
		defer delete(roles)
		for r in roles {
			if len(strings.trim_space(r)) == 0 do return fmt.tprintf("--%s: empty role supplied", name)
		}

	}
	return
}
