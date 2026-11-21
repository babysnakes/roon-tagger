#+private file

package cli

import "core:fmt"
import "core:log"
import "core:os"
import "core:path/filepath"
import "core:slice"
import "core:strings"
import "project:ext/colors"
import "project:lib"
import "project:lib/metaflac"

MAX_FILES :: 100
TITLES_FILE_NAME :: "titles.tmp"

@(private)
edit_titles_run :: proc(opts: Edit_Titles_Options) -> Cli_Error {
	editor_defined := len(opts.editor) > 0
	requested_editor: []string
	if editor_defined do requested_editor = extract_editor(opts.editor)
	files_count := len(opts.overflow)
	buf_files: [MAX_FILES]^metaflac.Flac_Metadata
	buf_titles: [MAX_FILES]string


	if files_count == 0 {
		log.error("At least one file should be specified")
		return lib.General_RT_Error{"At least one file should be specified"}
	}
	if opts.wait && editor_defined {
		return lib.General_RT_Error{"Editor and wait are mutually exclusive"}
	}
	if files_count > MAX_FILES {
		msg := fmt.tprintf("Too many flac files specified (%d), limit is: %d", files_count, MAX_FILES)
		log.error(msg)
		return lib.General_RT_Error{msg}
	}

	config_dir := get_config_dir() or_return
	config, c_err := load_config(config_dir, context.temp_allocator)
	if c_err != nil {
		msg := fmt.tprintf("loading config: %v", c_err)
		log.error(msg)
		return lib.General_RT_Error{msg}
	}
	editor, config_editor_defined := config.editor.?
	files := buf_files[:files_count]
	titles := buf_titles[:files_count]

	for path, idx in opts.overflow {
		meta, err := metaflac.load_metadata_from_file(path)
		if err != nil {
			msg := fmt.tprintf("Error extracting metadata from '%s': %v", path, err)
			log.error(msg)
			return lib.General_RT_Error{msg}
		}
		files[idx] = meta
	}
	tracks, ok := lib.to_consecutive_tracks(files, context.temp_allocator)
	if !ok do return lib.General_RT_Error{"Provided tracks are not consecutive"}
	defer for f in tracks {
		metaflac.release_metadata(f)
	}

	for m, idx in tracks {
		vorbis := metaflac.vorbis_comment(m)
		title, title_defined := lib.extract_single_value(lib.TAG_TITLE, vorbis)
		if !title_defined {
			log.error("Track '", m.path, "' has no title", sep = "")
			return lib.General_RT_Error{fmt.tprintf("Track '%s' has no title", m.path)}
		}
		titles[idx] = title
	}

	titles_str := strings.join(titles, "\n")
	defer delete(titles_str)
	write_titles(transmute([]byte)titles_str) or_return
	defer {
		d_err := os.remove(TITLES_FILE_NAME)
		if d_err != nil do fmt.panicf("Deleting temporary titles file: %v", d_err)
	}

	switch {
	case opts.wait:
		log.debug("Running with `wait` requested")
		run_with_wait() or_return
	case editor_defined:
		log.debug("Running with `editor` requested")
		run_with_cmd(requested_editor) or_return
	case config_editor_defined:
		log.debug("Running with `editor` by default (editor is defined in config)")
		run_with_cmd(editor) or_return
	case:
		fmt.println("running with `wait` as no editor defined")
		run_with_wait() or_return
	}

	new_titles := read_titles() or_return

	if slice.equal(titles, new_titles) {
		fmt.printfln("\n%s: No change Required", colors.green("Success", context.temp_allocator))
		return nil
	}

	if len(titles) != len(new_titles) {
		return lib.General_RT_Error {
			fmt.tprintf("Number of titles doesn't match (expected %d, got %d)", len(titles), len(new_titles)),
		}
	}

	for nt, idx in new_titles {
		if nt != titles[idx] {
			meta := tracks[idx]
			log.debug("Updating modified title in '", meta.path, "' to '", nt, "'", sep = "")
			lib.tags_set_tag(lib.TAG_TITLE, nt, meta) or_return
			err := metaflac.save_metadata(meta)
			if err != nil {
				log.error("error saving file (", meta.path, "): ", err, sep = "")
				return lib.General_RT_Error{fmt.tprintf("Saving file (%s): %v", meta.path, err)}
			}
		}
	}

	fmt.printfln("\n%s: Updated titles", colors.green("Success", context.temp_allocator))
	return nil
}

// Writes the provided contents to TITLES_FILE_NAME. Fails if already exists.
write_titles :: proc(contents: []byte) -> Cli_Error {
	permissions := os.Permissions_Read_All + {.Write_User}
	f, err := os.open(TITLES_FILE_NAME, {.Write, .Create, .Excl}, permissions)
	if err != nil {
		msg := fmt.tprintf("Creating temporary titles file (%x): %v", TITLES_FILE_NAME, err)
		log.error(msg)
		return lib.General_RT_Error{msg}
	}
	defer os.close(f)
	_, err = os.write(f, contents)
	if err != nil {
		msg := fmt.tprintf("Writing to temporary titles file (%x): %v", TITLES_FILE_NAME, err)
		log.error(msg)
		return lib.General_RT_Error{msg}
	}

	return nil
}

read_titles :: proc() -> (result: []string, err: Cli_Error) {
	data, r_err := os.read_entire_file(TITLES_FILE_NAME, context.temp_allocator)
	if r_err != nil {
		log.error("error reading file", r_err)
		return result, lib.General_RT_Error{fmt.tprintf("Reading titles file: %v", r_err)}
	}
	s := string(data)
	s = strings.trim_right_space(s)
	s = strings.trim_suffix(s, "\n")
	s = strings.trim_suffix(s, "\r\n")
	result = strings.split(s, "\n", context.temp_allocator)
	for &t in result {
		t = strings.trim_suffix(t, "\r") // if external editor converted to '\r\n'
		t = strings.trim_space(t)
	}
	return
}

run_with_wait :: proc() -> Cli_Error {
	path, err := filepath.abs(TITLES_FILE_NAME, context.temp_allocator)
	if err != nil do fmt.panicf("Error converting to absolute path: %v", err)
	fmt.printfln(
		"%s: Edit titles file %s in an external application and press ENTER once done to continue",
		colors.blue("Continue:", context.temp_allocator),
		path,
	)

	buf: [1]byte
	os.read(os.stdin, buf[:])
	return nil
}

run_with_cmd :: proc(cmd: []string) -> Cli_Error {
	file_slice := []string{TITLES_FILE_NAME}
	full_cmd := slice.concatenate([][]string{cmd, file_slice})
	defer delete(full_cmd)
	process, err := os.process_start({command = full_cmd, stdin = os.stdin, stdout = os.stdout, stderr = os.stderr})
	if err != nil do return lib.General_RT_Error{fmt.tprintf("Failed to start cmd: %v", err)}
	state, wait_err := os.process_wait(process)
	if wait_err != nil do return lib.General_RT_Error{fmt.tprintf("Wait failed: %v", wait_err)}
	if state.exit_code != 0 do return lib.General_RT_Error{fmt.tprintf("Editor exited with non-zero exit code: %d", state.exit_code)}
	_ = os.process_terminate(process)
	return nil
}
