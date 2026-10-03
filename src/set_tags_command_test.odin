#+test

package roon_tagger

import "core:fmt"
import "core:log"
import "core:os"
import "core:slice"
import "core:testing"
import "core:time/datetime"
import "dates"
import "metaflac"
import "test_helpers"

@(test)
set_tags_all_fields_should_work :: proc(t: ^testing.T) {
	tmpdir, tmp_err := os.mkdir_temp("", "set-tags-test", context.temp_allocator)
	ensure(tmp_err == nil, "error creating tmp directory")
	defer os.remove_all(tmpdir)
	test_flac := test_helpers.copy_fixture("with-credits.flac", tmpdir, context.temp_allocator)

	overflow := make([dynamic]string, context.temp_allocator)
	append(&overflow, test_flac)
	opts := Set_Tags_Options {
		overflow     = overflow,
		composer     = "My Composer",
		conductor    = "My Conductor, Other Conductor",
		title        = "A new title",
		year         = 1999,
		import_date  = dates.Date_Tag{datetime.Date{2020, 7, 13}},
		release_date = dates.Date_Tag{datetime.Date{2018, 1, 1}},
	}

	result := set_tags_run(opts)
	testing.expect(t, result, "failed running set-tags")

	meta, err := metaflac.load_metadata_from_file(test_flac)
	fmt.ensuref(err == nil, "failed reading tmp file: %v", err)
	comments := metaflac.vorbis_comment(meta).comments

	log.info("testing title ...")
	testing.expect_value(t, 1, len(comments[TAG_TITLE]))
	testing.expect_value(t, "A new title", comments[TAG_TITLE][0])

	log.info("testing year ...")
	testing.expect_value(t, 1, len(comments[TAG_YEAR]))
	testing.expect_value(t, "1999", comments[TAG_YEAR][0])

	log.info("testing import date ...")
	testing.expect_value(t, 1, len(comments[TAG_IMPORTDATE]))
	testing.expect_value(t, "2020-7-13", comments[TAG_IMPORTDATE][0])

	log.info("testing release date ...")
	testing.expect_value(t, 1, len(comments[TAG_RELEASEDATE]))
	testing.expect_value(t, "2018-1-1", comments[TAG_RELEASEDATE][0])

	log.info("testing conductor ...")
	testing.expect_value(t, 2, len(comments[TAG_CONDUCTOR]))
	testing.expect_value(t, "My Conductor", comments[TAG_CONDUCTOR][0])
	testing.expect_value(t, "Other Conductor", comments[TAG_CONDUCTOR][1])
	testing.expect(
		t,
		slice.contains(comments[TAG_PERSONNEL][:], "My Conductor - Conductor"),
		"missing conductor credit",
	)
	testing.expect(
		t,
		slice.contains(comments[TAG_PERSONNEL][:], "Other Conductor - Conductor"),
		"missing conductor credit",
	)

	log.info("testing composer ...")
	testing.expect_value(t, 1, len(comments[TAG_COMPOSER]))
	testing.expect_value(t, "My Composer", comments[TAG_COMPOSER][0])
	testing.expect(
		t,
		slice.contains(comments[TAG_PERSONNEL][:], "My Composer - Composer"),
		"missing composer credit",
	)
}
