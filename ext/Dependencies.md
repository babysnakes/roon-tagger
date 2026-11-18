## External Dependencies for roon-tagger

This directory includes external dependencies to the project, either copied or as submodule. Could
be imported to the project using (provided you have defined the `project` collection):

```odin
import "project:ext/<name>"
```

### Dependencies

- [pflags][] - Because of the short/long arguments support. Hopefully this will be merged into Odin.
- [colors][] (odin-colors) - Copied project files into this project (commit: `74ca0482a6466873eeee5d907a694e0b5f16408e`)

[pflags]: https://github.com/prnvbn/pflags
[colors]: https://github.com/hrszpuk/odin-color
