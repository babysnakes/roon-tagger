package roon_tagger

import "core:fmt"
import "core:mem"
import "core:os"
import "core:path/filepath"
import "core:sys/windows"
import "metaflac"

main :: proc() {
	verbose: bool

	// track memory error, not sure if you must also add -sanitize:address...
	when ODIN_DEBUG {
		track: mem.Tracking_Allocator
		mem.tracking_allocator_init(&track, context.allocator)
		context.allocator = mem.tracking_allocator(&track)

		defer {
			if len(track.allocation_map) > 0 {
				total: u32
				for _, entry in track.allocation_map {
					total += u32(entry.size)
				}

				if !verbose do fmt.printfln("\n\n Memory leaks of %d bytes exists! Run with -v for details", total)
				if verbose do fmt.eprintfln("\n\nSee memory leaks below (total: %d bytes):", total)
				for _, entry in track.allocation_map {
					if verbose do fmt.eprintf("%v leaked %v bytes\n", entry.location, entry.size)
				}
			}
			mem.tracking_allocator_destroy(&track)
		}
	}

	// Display UTF-8 characters correctly in the console
	when ODIN_OS == .Windows {
		windows.SetConsoleOutputCP(.UTF8)
	}

	if len(os.args) < 2 {
		fmt.eprintln("Err: Missing arguments!")
		fmt.eprintfln("\nUsage: %s <path-to-flac-file> [-v]", os.args[0])
		os.exit(1)
	}

	path, pathErr := filepath.abs(os.args[1])
	if pathErr != nil {
		fmt.eprintfln("Error parsing path: %v", pathErr)
		os.exit(1)
	}

	if len(os.args) > 2 && os.args[2] == "-v" do verbose = true

	fmt.printfln("Parsing file: %s", path)
	meta, err := metaflac.load_metadata_from_file(path)

	if err != nil {
		fmt.eprintfln("Error loading metadata from %v: %v", meta.path, err)
	} else {
		fmt.printfln("Total metadata size: %d bytes", meta.length)
		fmt.println("\nBlocks:\n")
		for b in meta.blocks {
			metaflac.print_block(b)
			fmt.println("")
		}
	}
}
