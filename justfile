collections_args := "-collection:project=."

[private]
@default:
    echo ""
    echo "Project tasks for DotLocal DNS"
    echo ""
    just --list

[doc("Create a debug build of the game")]
[windows]
build:
    odin build cli {{ collections_args }} -debug -vet -strict-style -vet-tabs -warnings-as-errors -out:bin/roon-tagger.exe

[doc("Run all tests")]
test-all:
    odin test cli {{ collections_args }} --all-packages

[doc("Odin *build* template (with collections) that accepts more arguments (e.g. build test executable)")]
[positional-arguments]
build-args *args='':
    odin build cli {{ collections_args }} "$@"

[doc("Build in release mode")]
release outfile="roon-tagger.exe":
    odin build cli {{ collections_args }} -o:speed -out:bin/{{ outfile }}

[doc("Code metrics")]
metrics:
    tokei --exclude "ext/"

[doc("Code metrics (per file metrics)")]
file-metrics:
    tokei --exclude "ext/" -f

[doc("Generate roles.txt, if manually supplying input make it absolute path")]
[working-directory("resources")]
roles input="roles.json":
    dotnet fsi extract-roles.fsx {{ input }} roles.txt
