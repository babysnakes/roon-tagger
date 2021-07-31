## Roon Tagger Cli Documentation

While Roon has excellent metadata management via database, it is sometimes
desireable to manage metadata via tags. A few use cases for managing data
manually are:

* If an album does not exist in the database.
* If an existing database metadata is missing or incorrect.
* For setting metadata to override default application values like _import date_.

### Supported File Formats

Currently we only support _Flac_ format.

### SubCommands

The following sub-commands are available:

* [`set-tags`](commands/set-tags.md)
* [`edit-titles`](commands/edit-titles.md)
* [`credits`](commands/credits.md)
* [`configure`](commands/configure.md)
* [`view`](commands/view.md)
* [`extract-works`](commands/extract-works.md)

### Menu Navigation

Some of the commands presents you with sort of terminal mode menu navigation.
Use _Up/Down_ arrows to navigate these menus and follow the on screen
instructions.

### Usage

For usage on each sub-command run:

```sh
roon-tagger [ CMD ] -h
```
