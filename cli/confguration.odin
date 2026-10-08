package cli

import "core:log"
import "core:encoding/json"
import "core:fmt"
import "core:mem"
import "core:os"
import "core:path/filepath"
import "core:strings"

CLI_CONFIG_FILE_NAME_V1 :: "cli_config_v1.json"

Cli_Config_V1 :: struct {
	editor: Maybe([]string),
}

// Load the config file from within the specified directory. Normalizes the config.
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
	config_file := filepath.join({config_dir, CLI_CONFIG_FILE_NAME_V1}, context.temp_allocator) or_return
	if !os.exists(config_file) do return
	data := os.read_entire_file(config_file, context.temp_allocator) or_return

	j_err := json.unmarshal(data, &config, allocator = allocator)
	if j_err != nil do err = General_Error{fmt.tprintf("Error parsing config file: %v", j_err)}
	_normalize_config(&config)
	return
}

// Saves the config into the specified directory. Normalizes the config.
//
// This function allocates temporary data to the `temp_allocator`, it's
// advisable to free it after.
save_config :: proc(config: ^Cli_Config_V1, config_dir: string) -> (err: Cli_Error) {
	config_file := filepath.join({config_dir, CLI_CONFIG_FILE_NAME_V1}, context.temp_allocator) or_return
	_normalize_config(config)

	if os.exists(config_file) {
		log.info("Creating a backup of existing config")
		bak_file := strings.concatenate({config_file, ".bak"}, context.temp_allocator)
		os.copy_file(bak_file, config_file) or_return
	}

	json_data, j_err := json.marshal(config^, {pretty = true}, context.temp_allocator)
	if j_err != nil {
		err = General_Error{fmt.tprintf("Error marshaling config: %v", j_err)}
		return
	}
	os.write_entire_file(config_file, json_data) or_return
	return nil
}

@(private="file")
_normalize_config :: proc(conf: ^Cli_Config_V1) {
	editor, defined := conf.editor.?
	if defined && len(editor) == 0 do conf.editor = nil
}
