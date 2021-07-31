## Edit the Titles of the Provided Tracks in a Text Editor - _edit-titles_

This command let you edit the titles of the provided tracks (sorted by
disc/track number) in a text file. This is usable mainly when you need to edit
multiple titles which have identical parts (e.g. work: movement in classical
music). Editing all the titles in an advanced text editor let's you easily
copy/paste and compare sections between titles.

**Important:**

* When editing the titles *do not* add, remove or change the order of the lines!
* The provided audio tracks should be sequential (after sorting). Don't just
  pick the files you want to edit, it's better to provide all the files in the
  album.

### Editing Modes

You have the following modes for editing the titles file:

#### Saving the titles to a file for editing in an external tool

This option (default when editor command is not configured) will save the titles
to a text file in the current working directory and let you edit it in a text
editor of your choice outside the terminal. After you're done editing, press
`Enter` to let the tool know that you're done editing the file. It will read and
save the titles to the corresponding tracks.

#### Editing the titles in a configured command

This option will launch the configured editor and let you edit the titles while
waiting for you to finish editing. When you leave the editor it will read the
saved file and save the new titles.

### Example Usage

Edit the titles of all tracks in directory (windows powershell version):

```powershell
roon-tagger edit-titles (gi *.flac)
```

Get full usage:

```bash
roon-tagger edit-titles -h
```
