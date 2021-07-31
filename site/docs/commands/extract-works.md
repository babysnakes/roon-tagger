## Extract Work / Movement Information from Title - _extract-works_

Sometimes a musical piece is constructed of _work_ and _movements_ instead of
songs. This happens mostly in classical music but not exclusively. Roon uses
custom tags to display _works_ with _movements_.

Usually such musical pieces have a title in the form of `work: movement`. The
movement might include movement numbering in various formats. This command
parses the titles of all provided tracks and extracts works with movements. It
uses a few rules to identify works:

* The title must be in a format of `work: movement`.
* There should be more then one sequential track with the same work name to be
  considered as work.
* The movement part of each track within single work is parsed to identify
  numbering (see [movement parsing](#movement-parsing)). This is used to
  identify whether a number (or roman letters) prefix is part of the movement
  name or numberings.

Once it identifies all the works you can either accept all extracted works or
handle each work individually - this will allow you to manually change the
_work_ name and edit all the _movements_ within a work.

Although it strips the numbering from the movement name you can choose to add
simple roman numbering as prefix (run with `-h` for details).

### Limitations

* This tool expects files that are correctly tagged - each track should have at
  least _title_, _track number_ and _disc number_ (if applicable).
* Always provide all tracks in an album. If the track numbers are not sequential
  it will throw an error. Note that there's no need to sort the provided tracks,
  it sorts the tracks before processing.

### Movement Parsing

The movement parsing tries to identify whether the first parts of the movement
is part of the movement name or movement numbering. If it identifies that all
the movements in a work has the same numbering format it will remove the
numbering from the title name.

It supports the following
formats:

* Roman numbers with or without suffix:
  * `II A Title`
  * `II: A Title`
  * `II. A Title`
* Numbers with various combinations of prefix/suffix. Here are examples:
  * `2 A Title`
  * `2. A Title`
  * `2: A Title`
  * `No 2 A Title`
  * `No 2. A Title`
  * `No. 2: A Title`

### Example Usage

Get full usage:

```bash
roon-tagger view -h
```
