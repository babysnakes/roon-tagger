package cli

import "core:os"
import "project:lib"

Cli_Error :: union {
	os.Error,
	lib.General_RT_Error,
	lib.Lib_Error,
}
