package cli

import "core:fmt"
config_run :: proc(opts: Config_Options) -> bool {
	fmt.printfln("Options: %v'\n", opts)
	fmt.println("NOT IMPLEMENTED!")
	return false
}
