## Roon Tagger

Sometime in the future it will be a CLI utility for tagging music files with
(mostly) roon specific metadata.

### Development Setup

Requires _.Net 6.0.x_ (check `global.json`).

Run:

```powershell
# Initialize tools and dependencies:
dotnet tool restore
dotnet paket restore

# Build, Test, etc
dotnet build
dotnet test
```

We also use [Fake][] for running some build/test tasks. For help run:

```powershell
dotnet fake build
```

This will list the main targets to run. These targets are also running in CI.

### Refreshing Roles List

This app contains a complete list of supported roles in _Roon_'s credit tags (as
published in their wiki). In order to update this roles list open
`<REPOSITORY_ROOT>/src/Metadata.Lib/ExtractRoles.fsx` and follow the
instructions. Note that this is not guaranteed to work, it's just how it worked
for me.

[Fake]: https://fake.build/