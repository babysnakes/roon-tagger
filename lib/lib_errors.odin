package lib

import "core:mem"

// A catch all lib error
Lib_Error :: union {
	mem.Allocator_Error,
	General_RT_Error,
}

// A general string error
General_RT_Error :: struct {
	error: string,
}
