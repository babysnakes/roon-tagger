package roon_tagger

import "core:fmt"
import "core:path/filepath"
import "metaflac"

view_flac :: proc(path: string) -> bool {
	full_path, pathErr := filepath.abs(path)
	if pathErr != nil {
		fmt.eprintfln("Error parsing path: %v", pathErr)
		return false
	}

	fmt.printfln("Parsing file: %s", full_path)
	meta, err := metaflac.load_metadata_from_file(full_path)
	defer metaflac.release_metadata(meta)

	if err != nil {
		fmt.eprintfln("Error loading metadata from %v: %v", meta.path, err)
		return false
	} else {
		fmt.printfln("Total metadata size: %d bytes", meta.length)
		fmt.println("\nBlocks:")
		for b in meta.blocks {
			metaflac.print_block(b)
			fmt.println("")
		}
	}

	return true
}
