## Roon Tagger

Sometime in the future it will be a CLI utility for tagging music files with
(mostly) roon specific metadata.

### Development Setup

Requires _.Net 5.0.x_.

Run:

```powershell
dotnet tool restore
dotnet paket restore
dotnet build
# Optionally run the build tool locally. Without arguments it will show you
# usage summary
.\build.ps1 # or ./build.sh for unix shells ...
.\build.ps1 <options>
```

### Refreshing Roles List

This app contains a complete list of supported roles in _Roon_'s credit tags (as
published in their wiki). In order to update this roles list open
`<REPOSITORY_ROOT>/src/Metadata.Lib/ExtractRoles.fsx` and follow the
instructions. Note that this is not guaranteed to work, it's just how it worked
for me.
