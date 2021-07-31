## Set Roon Specific Tags - _set-tags_

Sets roon specific tags. Unless specifically indicated (run with `-h`), all
provided tags *overwrite* existing tags.

### Example Usage

Set year (release date) and title:

```sh
roon-tagger set-tags --title 'A new Title' --year 2020 filename.flac
```

Get full usage (list of supported tags):

```sh
roon-tagger set-tags -h
```
