package cli

import "base:runtime"
import "core:encoding/json"
import "core:fmt"
import "core:io"
import "core:log"
import "core:mem"
import "core:os"
import "core:path/filepath"
import "core:strings"
import "project:lib"

CLI_CONFIG_FILE_NAME_V1 :: "cli_config_v1.json"

Cli_Config_V1 :: struct {
	editor: Maybe([]string),
}

// Load the config file from within the specified directory. Normalizes the config.
// Returns default config if not exists.
//
// This function allocates temporary data to the `temp_allocator`, it's
// advisable to free it after.
load_config :: proc(
	config_dir: string,
	allocator: mem.Allocator = context.allocator,
) -> (
	config: Cli_Config_V1,
	err: Cli_Error,
) {
	config_file, p_err := filepath.join({config_dir, CLI_CONFIG_FILE_NAME_V1}, context.temp_allocator)
	if p_err != .None do fmt.panicf("[Bug]: memory error: %v", p_err)
	if !os.exists(config_file) do return
	data := os.read_entire_file(config_file, context.temp_allocator) or_return

	j_err := json.unmarshal(data, &config, allocator = allocator)
	if j_err != nil do err = lib.General_RT_Error{fmt.tprintf("Error parsing config file: %v", j_err)}
	_normalize_config(&config)
	return
}

// Saves the config into the specified directory. Normalizes the config.
//
// This function allocates temporary data to the `temp_allocator`, it's
// advisable to free it after.
save_config :: proc(config: ^Cli_Config_V1, config_dir: string) -> (err: Cli_Error) {
	config_file, p_err := filepath.join({config_dir, CLI_CONFIG_FILE_NAME_V1}, context.temp_allocator)
	if p_err != .None do fmt.panicf("[Bug]: memory error: %v", p_err)
	_normalize_config(config)

	if os.exists(config_file) {
		log.info("Creating a backup of existing config")
		bak_file := strings.concatenate({config_file, ".bak"}, context.temp_allocator)
		os.copy_file(bak_file, config_file) or_return
	}

	if !os.exists(config_dir) {
		log.debug("creating config directory:", config_dir)
		err := os.make_directory_all(config_dir)
		if err != nil do return lib.General_RT_Error{fmt.tprintf("creating config directory: %v", err)}
	}

	json_data, j_err := json.marshal(config^, {pretty = true}, context.temp_allocator)
	if j_err != nil {
		err = lib.General_RT_Error{fmt.tprintf("Error marshaling config: %v", j_err)}
		return
	}
	os.write_entire_file(config_file, json_data) or_return
	return nil
}

// Extracts the user config directory (cross platform) based on dev/release mode.
//
// Note: Uses `context.temp_allocator`
get_config_dir :: proc() -> (result: string, err: Cli_Error) {
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
		return result, lib.General_RT_Error{msg}
	}
	config_dir, d_err := filepath.join({user_config_dir, config_dir_name}, context.temp_allocator)
	if d_err != .None do fmt.panicf("[Bug]: memory error: %v", d_err)
	return config_dir, nil
}

@(private = "file")
_normalize_config :: proc(conf: ^Cli_Config_V1) {
	editor, defined := conf.editor.?
	if defined && len(editor) == 0 do conf.editor = nil
}
