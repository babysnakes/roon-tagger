namespace RoonTagger.Metadata

open FsToolkit.ErrorHandling
open RoonTagger.Metadata.Utils

/// Accepts a list of valid roles for validating. To skip validation provide None
type RoleValidator =
    | Roles of string list option

    /// Validates the provided role against `Roles` (if not None)
    member rv.validate(role: string) : Result<unit, MetadataErrors> =
        match rv with
        | Roles None -> Ok()
        | Roles (Some roles) ->
            if roles |> List.contains role then
                Ok()
            else
                UnsupportedRole role |> Error

    /// Validates the provided roles against `Roles` (if not None)
    member rv.validateMany(roles: string list) =
        roles |> List.traverseResultA rv.validate
