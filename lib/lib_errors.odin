package lib

// TODO (haim) If this stays single type, consider deleting this type.
// A catch all lib error
Lib_Error :: union {
	General_RT_Error,
}

// A general string error
General_RT_Error :: struct {
	error: string,
}
