package test_helpers

import "base:runtime"
import "core:os"
import "core:path/filepath"

// Copies a named fixture to temp directory for testing and manipulating
copy_fixture :: proc(name, target_dir: string, allocator: runtime.Allocator) -> string {
	target, err1 := filepath.join({target_dir, name}, allocator)
	ensure(err1 == nil)
	fixture, err2 := filepath.join({"tests", "fixtures", name}, allocator)
	ensure(err2 == nil)
	err3 := os.copy_file(target, fixture)
	ensure(err3 == nil)
	return target
}
