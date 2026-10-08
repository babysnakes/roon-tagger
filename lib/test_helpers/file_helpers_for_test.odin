#+test

package test_helpers

import "base:runtime"
import "core:fmt"
import "core:os"
import "core:path/filepath"

// Copies a named fixture to temp directory for testing and manipulating.
// Optional target file name. Using `context.temp_allocator`.
copy_fixture :: proc(name, target_dir: string, target_name: string = "") -> string {
	t_name := target_name
	if target_name == "" do t_name = name
	target, err1 := filepath.join({target_dir, t_name}, context.temp_allocator)
	fmt.ensuref(err1 == nil, "error joining target path: %v", err1)
	fixture, err2 := filepath.join({"fixtures", name}, context.temp_allocator)
	fmt.ensuref(err2 == nil, "error joining fixture path: %v", err2)
	err3 := os.copy_file(target, fixture)
	fmt.ensuref(err3 == nil, "error copying file: %v", err3)
	return target
}
