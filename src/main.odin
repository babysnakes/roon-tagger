package roon_tagger

import "core:reflect"
import "core:fmt"
import "core:os"
import "core:path/filepath"
import "metaflac"

main :: proc() {
	if len(os.args) != 2 {
		fmt.eprintln("Err: invalid number of arguments!")
		fmt.eprintfln("\nUsage: %s <path-to-flac-file>", os.args[0])
		os.exit(1)
	}

	path, pathErr := filepath.abs(os.args[1])
	if pathErr != nil {
		fmt.eprintfln("Error parsing path: %v", pathErr)
		os.exit(1)
	}

	fmt.printfln("Parsing file: %s", path)
	meta, err := metaflac.load_metadata_from_file(path)

	if err != nil {
		fmt.eprintfln("Error loading metadata from %v: %v", meta.path, err)
	} else {
		fmt.printfln("Blocks in: '%v':\n", meta.path)
		for b in meta.blocks {
			fmt.printfln("  * %v: size: %v", reflect.union_variant_typeid(b), metaflac.calculate_block_size(b))
		}
	}
}
