#+test

package cli

import "core:fmt"
import "core:log"
import "core:os"
import "core:slice"
import "core:strings"
import "core:testing"
import "project:lib"
import "project:lib/metaflac"
import "project:lib/test_helpers"

@(test)
t_credits_cmd_requires_files_parameter :: proc(t: ^testing.T) {
	adds := make([dynamic]string, context.temp_allocator)
	append(&adds, "John Coltrane:Saxophone")
	opts := Credits_Options {
		add = adds,
	}

	// Odin tests fail on error logging
	orig_logger := context.logger
	defer context.logger = orig_logger // just in case something fails
	context.logger = log.nil_logger()

	res_err := credits_run(opts)

	context.logger = orig_logger
	err := res_err.(lib.General_RT_Error) // fails on error
	testing.expect(t, strings.contains(err.error, "At least one"), "error text doesn't match")
}

@(test)
t_credits_cmd_trims_person_and_roles :: proc(t: ^testing.T) {
	tmpdir, tmp_err := os.mkdir_temp("", "credits-trim-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	test_flac := test_helpers.copy_fixture("with-credits.flac", tmpdir)

	log.info("testing: adding credits with spaces around person and role")
	overflow := make([dynamic]string, context.temp_allocator)
	append(&overflow, test_flac)
	adds := make([dynamic]string, context.temp_allocator)
	append(&adds, "John Coltrane:Saxophone")
	append(&adds, " Archie Shepp: Tenor Saxophone")
	add_opts := Credits_Options {
		add      = adds,
		overflow = overflow,
	}

	err := credits_run(add_opts)
	fmt.ensuref(err == nil, "failure adding credits: %v", err)

	meta1, r_err1 := metaflac.load_metadata_from_file(test_flac)
	fmt.ensuref(r_err1 == nil, "failed reading tmp file: %v", err)
	comments1 := metaflac.vorbis_comment(meta1).comments
	testing.expect(
		t,
		slice.contains(comments1[lib.TAG_PERSONNEL][:], "John Coltrane - Saxophone"),
		"does not contain correct credit (john coltrane)",
	)
	testing.expect(
		t,
		slice.contains(comments1[lib.TAG_PERSONNEL][:], "Archie Shepp - Tenor Saxophone"),
		"does not contain untrimmed credit (archie shepp)",
	)

	log.info("testing: deleting credits with spaces around person and role")
	deletions := make([dynamic]string, context.temp_allocator)
	append(&deletions, " Archie Shepp: Tenor Saxophone")
	del_opts := Credits_Options {
		del      = deletions,
		overflow = overflow,
	}

	del_err := credits_run(del_opts)
	fmt.ensuref(del_err == nil, "failure adding credits: %v", del_err)

	meta2, r_err2 := metaflac.load_metadata_from_file(test_flac)
	fmt.ensuref(r_err2 == nil, "failed reading tmp file: %v", del_err)
	comments2 := metaflac.vorbis_comment(meta2).comments
	testing.expect(
		t,
		slice.contains(comments2[lib.TAG_PERSONNEL][:], "John Coltrane - Saxophone"),
		"does not contain correct credit (john coltrane)",
	)
	testing.expect(
		t,
		!slice.contains(comments2[lib.TAG_PERSONNEL][:], "Archie Shepp - Tenor Saxophone"),
		"didn't remove correctly the untrimmed credit (archie shepp)",
	)

}
