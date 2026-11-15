package cli

import "core:fmt"
import "project:lib"

config_run :: proc(opts: Config_Options) -> Cli_Error {
	fmt.printfln("Options: %v'\n", opts)
	return lib.General_RT_Error{"NOT IMPLEMENTED"}
}
