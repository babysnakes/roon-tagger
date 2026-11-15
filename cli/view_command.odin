package cli

import "core:fmt"
import "core:log"
import "core:path/filepath"
import "project:lib"
import "project:lib/metaflac"

view_flac :: proc(path: string) -> Cli_Error {
	full_path, pathErr := filepath.abs(path)
	if pathErr != nil {
		msg := fmt.tprintf("Error parsing path (%s) %v", path, pathErr)
		log.error(msg)
		return lib.General_RT_Error{msg}
	}
	defer delete(full_path)

	fmt.printfln("Parsing file: %s", full_path)
	meta, err := metaflac.load_metadata_from_file(full_path)
	defer metaflac.release_metadata(meta)

	if err != nil {
		msg := fmt.tprintf("Error loading metadata from %v: %v", meta.path, err)
		log.error(msg)
		return lib.General_RT_Error{msg}
	} else {
		fmt.printfln("Total metadata size: %d bytes", meta.length)
		fmt.println("\nBlocks:")
		for b in meta.blocks {
			metaflac.print_block(b)
			fmt.println("")
		}
	}

	return nil
}
