package lib

import "project:lib/metaflac"

// A catch all lib error
Lib_Error :: union {
	metaflac.Flac_Error,
	General_RT_Error,
}

// A general string error
General_RT_Error :: struct {
	error: string,
}
