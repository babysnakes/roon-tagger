## Release Notes

TODO: Fill release notes ...

### Installation Instructions

#### Archives

There are two kinds of archives:

* `roon-tagger-xxx-noarch.*` - These are small and contains only the application. They do require
  that you have _.Net 6.x_ runtime installed.
* `roon-tagger-xxx-<ARCH>.*` - These contains everything you need to run _roon-tagger_. If your
  architecture is not suuported (e.g. you're using a 32 bit or ARM processor), please file an issue.

Installation is currently manual. You need to extract the archive to somewhere in your computer. In
_Windows_ you should add the directory you extracted to to the `PATH`. On _Linux_ / _MacOS you can
symlink the `roon-tagger` executable to somewhere in your `PATH`.

#### Scoop File

[Scoop][scoop] support is currently experimental *and* manual. You can install *Roon Tagger* with
scoop using the direct manifest URL:

* Copy the URL of the `roon-tagger.json` file in the current release assets.
* Install roon-tagger using the address copied above:

      scoop install <URL>
* Uninstall with:

      scoop uninstall roon-tagger

> **Warning**
> only 64bit systems are supported. for other architecture please file an issue.

[scoop]: https://scoop.sh