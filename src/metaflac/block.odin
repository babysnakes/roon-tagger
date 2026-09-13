package metaflac

Block :: union #no_nil {
	Stream_Info_Block,
	Padding_Block,
	Application_Block,
	Seek_Table_Block,
	Vorbis_Comment_Block,
	Cuesheet_Block,
	Picture_Block,
	Unknown_Block,
}

Stream_Info_Block :: struct {
	data: []u8,
}

Padding_Block :: struct {
	data: []u8,
}

Application_Block :: struct {
	data: []u8,
}

Seek_Table_Block :: struct {
	data: []u8,
}

Vorbis_Comment_Block :: struct {
	data: []u8,
}

Cuesheet_Block :: struct {
	data: []u8,
}

Picture_Block :: struct {
	data: []u8,
}

Unknown_Block :: struct {
	data: []u8,
}

parse_block :: proc(block_type: u8, data: []u8) -> Block {
	switch block_type {
	case 0:
		return parse_stream_info(data)
	case 1:
		return parse_padding(data)
	case 2:
		return parse_application(data)
	case 3:
		return parse_seek_table(data)
	case 4:
		return parse_vorbis_comment(data)
	case 5:
		return parse_cuesheet(data)
	case 6:
		return parse_picture(data)
	case:
		return parse_unknown(data)
	}
}

calculate_block_size :: proc(block: Block) -> (result: int) {
	switch b in block {
	case Stream_Info_Block:
		result = len(b.data)
	case Padding_Block:
		result = len(b.data)
	case Application_Block:
		result = len(b.data)
	case Seek_Table_Block:
		result = len(b.data)
	case Vorbis_Comment_Block:
		result = len(b.data)
	case Cuesheet_Block:
		result = len(b.data)
	case Picture_Block:
		result = len(b.data)
	case Unknown_Block:
		result = len(b.data)
	}

	return result
}

parse_stream_info :: proc(data: []u8) -> Stream_Info_Block {
	return Stream_Info_Block{data = data}
}

parse_padding :: proc(data: []u8) -> Padding_Block {
	return Padding_Block{data = data}
}

parse_application :: proc(data: []u8) -> Application_Block {
	return Application_Block{data = data}
}

parse_seek_table :: proc(data: []u8) -> Seek_Table_Block {
	return Seek_Table_Block{data = data}
}

parse_vorbis_comment :: proc(data: []u8) -> Vorbis_Comment_Block {
	return Vorbis_Comment_Block{data = data}
}

parse_cuesheet :: proc(data: []u8) -> Cuesheet_Block {
	return Cuesheet_Block{data = data}
}

parse_picture :: proc(data: []u8) -> Picture_Block {
	return Picture_Block{data = data}
}

parse_unknown :: proc(data: []u8) -> Unknown_Block {
	return Unknown_Block{data = data}
}
