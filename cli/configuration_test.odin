#+test

package cli

import "core:fmt"
import "core:os"
import "core:path/filepath"
import "core:slice"
import "core:strings"
import "core:testing"
import "project:lib/test_helpers"

@(test)
load_sample_config_loads_editor_correctly :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "configuration-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	_ = test_helpers.copy_fixture("sample-config.json", tmpdir, CLI_CONFIG_FILE_NAME_V1)
	conf, err := load_config(tmpdir, context.temp_allocator)
	fmt.ensuref(err == nil, "error loading config: %v", err)
	expected := []string{"zed", "-w", "-n"}
	editor := conf.editor.([]string)
	testing.expect(t, slice.equal(expected, editor))
}

@(test)
load_config_without_editor_sets_nil_editor :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "configuration-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	_ = test_helpers.copy_fixture("empty-config.json", tmpdir, CLI_CONFIG_FILE_NAME_V1)
	conf, err := load_config(tmpdir, context.temp_allocator)
	fmt.ensuref(err == nil, "error loading config: %v", err)
	expected: Maybe([]string) = nil
	testing.expect(t, conf.editor == nil, "editor is not nil")
}

@(test)
load_config_with_empty_editor_sets_nil_editor :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "configuration-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	_ = test_helpers.copy_fixture("empty-editor.json", tmpdir, CLI_CONFIG_FILE_NAME_V1)
	conf, err := load_config(tmpdir, context.temp_allocator)
	fmt.ensuref(err == nil, "error loading config: %v", err)
	expected: Maybe([]string) = nil
	testing.expect(t, conf.editor == nil, "editor is not nil")
}

@(test)
load_non_existent_config_generates_nil_editor :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "configuration-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	conf, err := load_config(tmpdir, context.temp_allocator)
	fmt.ensuref(err == nil, "error loading config: %v", err)
	expected: Maybe([]string) = nil
	testing.expect(t, conf.editor == nil, "editor is not nil")
}

@(test)
saves_config_correctly :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "configuration-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	conf := Cli_Config_V1{{"zed", "-w", "-n"}}
	save_err := save_config(&conf, tmpdir)
	ensure(save_err == nil)
	config_path, a_err := filepath.join({tmpdir, CLI_CONFIG_FILE_NAME_V1}, context.temp_allocator)
	ensure(a_err == nil)
	contents, f_err := os.read_entire_file(config_path, context.temp_allocator)
	fmt.ensuref(f_err == nil, "error reading config file: %v", f_err)
	testing.expect(t, strings.contains(string(contents), "zed"), "not contains editor")
}

@(test)
save_null_editor_if_editor_is_nil :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "configuration-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	conf := Cli_Config_V1{nil}
	save_err := save_config(&conf, tmpdir)
	ensure(save_err == nil)
	config_path, a_err := filepath.join({tmpdir, CLI_CONFIG_FILE_NAME_V1}, context.temp_allocator)
	ensure(a_err == nil)
	contents, f_err := os.read_entire_file(config_path, context.temp_allocator)
	fmt.ensuref(f_err == nil, "error reading config file: %v", f_err)
	// log.info("contents:", string(contents))
	testing.expect(t, strings.contains(string(contents), `"editor": null`), "not contains editor")
}

@(test)
save_normalize_empty_list_editor_as_nil_editor :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "configuration-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	conf := Cli_Config_V1{{}}
	save_err := save_config(&conf, tmpdir)
	ensure(save_err == nil)
	config_path, a_err := filepath.join({tmpdir, CLI_CONFIG_FILE_NAME_V1}, context.temp_allocator)
	ensure(a_err == nil)
	contents, f_err := os.read_entire_file(config_path, context.temp_allocator)
	fmt.ensuref(f_err == nil, "error reading config file: %v", f_err)
	// log.info("contents:", string(contents))
	testing.expect(t, strings.contains(string(contents), `"editor": null`), "not contains editor")
}

@(test)
saves_config_creates_backup :: proc(t: ^testing.T) {
	defer free_all(context.temp_allocator)
	tmpdir, tmp_err := os.mkdir_temp("", "configuration-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	// original config
	conf := Cli_Config_V1{{"zed", "-w", "-n"}}
	save_err := save_config(&conf, tmpdir)
	ensure(save_err == nil)
	// second config should create backup
	updated_conf := Cli_Config_V1{{"nvim"}}
	save_err = save_config(&updated_conf, tmpdir)
	ensure(save_err == nil)

	config_path, a_err := filepath.join({tmpdir, CLI_CONFIG_FILE_NAME_V1}, context.temp_allocator)
	ensure(a_err == nil)
	bak_path, bak_err := strings.concatenate({config_path, ".bak"}, context.temp_allocator)
	ensure(bak_err == nil)

	conf_contents, err1 := os.read_entire_file(config_path, context.temp_allocator)
	fmt.ensuref(err1 == nil, "error reading config file: %v", err1)

	bak_contents, err2 := os.read_entire_file(bak_path, context.temp_allocator)
	fmt.ensuref(err2 == nil, "error reading backup config file: %v", err2)

	testing.expect(t, strings.contains(string(conf_contents), `"nvim"`), "config not correct")
	testing.expect(t, strings.contains(string(bak_contents), `"zed"`), "backup not correct")
}
