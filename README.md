## Roon Tagger

Sometime in the future it will be a utility for tagging music files with (mostly) Roon specific
metadata. This is a rewrite in Odin, old pre-released binaries can be downloaded from the
[Releases][rel] page.

### Development Setup

- Have the latest _Odin_ version installed.
- Add the `project` collection (`-collection:project=.`) to your various _Odin_ invocations.
- Optionally have _DotNet_ (with _F#_ support) installed (see the section about _roles_ below).
- Optionally install [`just`][just] for running tasks (checkout `justfile`)

#### Importing Roon's supported roles

Use the included _F#_ script to import updated list of _Roon_ supported roles. This happens seldom
but if you need to run it, checkout the instructions in `resources/extract-roles.fsx`.

[rel]: https://github.com/babysnakes/roon-tagger/releases
[just]: https://just.systems/
