## External Dependencies for roon-tagger

This directory includes external dependencies to the project, either copied or as submodule. Could
be imported to the project using (provided you have defined the `project` collection):

```odin
import "project:ext/<name>"
```

### Dependencies

- [pflags][] - Because of the short/long arguments support. Hopefully this will be merged into Odin.

[pflags]: https://github.com/prnvbn/pflags
