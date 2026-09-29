#+feature dynamic-literals
#+test

package dates

import "core:testing"
import "core:time/datetime"

@(test)
parsing_well_formatted_dates_works_correctly :: proc(t: ^testing.T) {
	test_data := map[string]datetime.Date {
		"2014-4-11"  = datetime.Date{2014, 4, 11},
		"2024-11-11" = datetime.Date{2024, 11, 11},
	}
	defer delete(test_data)

	for k, v in test_data {
		dt, err := parse_date(k)
		testing.expect_value(t, err, datetime.Error.None)
		testing.expect_value(t, dt.date, v)
	}
}

@(test)
parsing_invalid_dates_fails :: proc(t: ^testing.T) {
	test_data := map[string]Date_Error {
		"invalid"      = Date_Parse_Error.Invalid_Date_Format,
		"2012-1122"    = Date_Parse_Error.Invalid_Date_Format,
		"year-1-1"     = datetime.Error.Invalid_Year,
		"2011-13-1"    = datetime.Error.Invalid_Month,
		"2011-month-1" = datetime.Error.Invalid_Month,
		"2011-10-32"   = datetime.Error.Invalid_Day,
		"2011-10-day"  = datetime.Error.Invalid_Day,
	}
	defer delete(test_data)

	for k, v in test_data {
		_, err := parse_date(k)
		testing.expect_value(t, err, v)
	}
}
