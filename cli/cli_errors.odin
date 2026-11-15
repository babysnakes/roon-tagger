package cli

import "base:runtime"
import "core:os"
import "project:lib"

Cli_Error :: union {
	os.Error,
	runtime.Allocator_Error,
	lib.General_RT_Error,
}
