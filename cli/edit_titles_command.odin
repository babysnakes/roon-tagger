package cli

import "core:fmt"
import "project:lib"

edit_titles_run :: proc(opts: Edit_Titles_Options) -> Cli_Error {
	fmt.printfln("opts: %v\n", opts)
	return lib.General_RT_Error{"NOT IMPLEMENTED"}
}
