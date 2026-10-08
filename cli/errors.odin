package cli

import "base:runtime"
import "core:os"

Cli_Error :: union {
	os.Error,
	runtime.Allocator_Error,
	General_Error,
}

General_Error :: struct {
	error: string,
}
