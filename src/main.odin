package roon_tagger

import "core:fmt"
import "core:mem"
import "core:os"
import "core:path/filepath"
import "metaflac"

main :: proc() {
	// track memory error, not sure if you must also add -sanitize:address...
	when ODIN_DEBUG {
		track: mem.Tracking_Allocator
		mem.tracking_allocator_init(&track, context.allocator)
		context.allocator = mem.tracking_allocator(&track)

		defer {
			if len(track.allocation_map) > 0 {
				fmt.eprintln("\n\nSee memory leaks below:")
				for _, entry in track.allocation_map {
					fmt.eprintf("%v leaked %v bytes\n", entry.location, entry.size)
				}
			}
			mem.tracking_allocator_destroy(&track)
		}
	}

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
			metaflac.print_block(b)
		}
	}
}
