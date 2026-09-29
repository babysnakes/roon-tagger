/*
This is a fake Date library to save dates to tags.

Currently Odin does not support parsing of date strings without hour. My requirements are
pretty basic so there's no need to look for a more robust implementation.

To be replaced once Odin does have a parser for date strings.
*/
package dates

import "core:strconv"
import "core:strings"
import "core:time"
import "core:time/datetime"

Date_Tag :: struct {
	date: datetime.Date,
}

Date_Error :: union #no_nil {
	datetime.Error,
	Date_Parse_Error,
}

Date_Parse_Error :: enum {
	Invalid_Date_Format,
}

parse_date :: proc(str_date: string) -> (d: Date_Tag, err: Date_Error) {
	if str_date == "today" {
		date, err := datetime.components_to_date(time.date(time.now()))
		return Date_Tag{date}, err
	}

	parts := strings.split(str_date, "-")
	defer delete(parts)
	if len(parts) != 3 do return d, .Invalid_Date_Format

	year, y_ok := strconv.parse_i64(parts[0])
	if !y_ok do return d, .Invalid_Year
	month, m_ok := strconv.parse_i64(parts[1])
	if !m_ok do return d, .Invalid_Month
	day, d_ok := strconv.parse_i64(parts[2])
	if !d_ok do return d, .Invalid_Day
	//
	date := datetime.components_to_date(year, month, day) or_return
	return Date_Tag{date}, .None
}
