module RoonTagger.Metadata.WorkMovement

open FsToolkit.ErrorHandling
open TrackHelpers
open RoonTagger.Metadata.Utils

let log = Serilog.Log.Logger

/// Splits the track's title to work and optional movement.
let splitTitle2WorkMovement (track: AudioTrack) =
    let title = Track.safeGetTagStringValue track TitleTag |> List.head

    title.Split(":", 2)
    |> List.ofArray
    |> function
    | title :: movement :: [] -> (title.Trim(), Some(movement.Trim()))
    | other ->
        let title = (other |> List.head).Trim()
        (title, None)

/// Extract work name from track's title. Return None if can not split to work/movement
let workFromTitle =
    splitTitle2WorkMovement
    >> fun (work, movement) ->
        if Option.isSome movement then
            Some work
        else
            None

/// Parsing movement part of the title. Should be able to parse the following as
/// indexed:
/// - I. a title
/// - II: another title
/// - No. 1: a title
/// - NO 2. another title
/// - 2. title
///
/// Sometimes movement have weird names starting with digit or roman.  It should
/// try to parse sensibly the following as titles without index:
/// - title with no index
/// - 2 digit as first char of movement (not index)
module MovementParser =

    open FParsec

    type MovementElements =
        | COLON
        | DOT
        | ROMAN
        | NO
        | IDX
        | REST

    let colon = pchar ':' >>% COLON
    let dot = pchar '.' >>% DOT
    let suffix = dot <|> colon
    let movementName = restOfLine false

    let roman = regex "(?i)[ivx]+" >>% ROMAN
    let no = pstringCI "no" .>> spaces >>% NO
    let noWithDot = no .>>. opt dot .>> spaces
    let idx = numberLiteral NumberLiteralOptions.None "number" >>% IDX

    // prefixes
    let romanPrefix = roman .>>. opt suffix .>> spaces1
    let numPrefix = opt noWithDot .>>. idx .>>. suffix .>> spaces1

    let title =
        opt (attempt romanPrefix |>> ignore <|> attempt (numPrefix |>> ignore))
        >>. movementName

    // For comparing the layout of the movements
    let romanPrefixString = romanPrefix |>> fun x -> x.ToString()
    let numPrefixString = numPrefix |>> fun x -> x.ToString()

    let titleLayout =
        opt (attempt romanPrefixString <|> attempt numPrefixString)
        .>>. (movementName >>% REST)

    /// Get the layout of the movement for comparison
    let parseLayout movement =
        run titleLayout movement
        |> function
        | Success (r, _, _) ->
            log.Verbose("Parsing layout of '{Movement}' returned: {R}", movement, r)
            r
        | Failure (msg, _, _) ->
            failwith $"[BUG]: Error parsing movement! this should not have happened. Error was: {msg}"

    /// Extract the movement name (remove prefixes)
    let parseMovement movement =
        run title movement
        |> function
        | Success (s, _, _) ->
            log.Verbose("Parsing title of '{Movement}' returned: {S}", movement, s)
            s
        | Failure (msg, _, _) ->
            failwith $"[BUG]: Error parsing movement! this should not have happened. Error was: {msg}"

/// An already parsed work (not applied). Only use `Create` to initialize
type Work =
    | Work of string * ConsecutiveTracks

    /// Tries to create a Work from the provided work name and tracks. Validates the input.
    static member Create (workName: string) (ConsecutiveTracks tracks as cTracks) =

        let processTrack (track: AudioTrack) =
            let w, mvmt = track |> splitTitle2WorkMovement

            if w <> workName || mvmt.IsNone then
                failwith (
                    $"[BUG] Work.Create was called with invalid data: path: {track.Path}, "
                    + $"provided work: {workName}, extracted work: {w}, movement: {mvmt}"
                )

            mvmt |> Option.get

        let movements = tracks |> List.map processTrack
        let count = movements |> List.length
        let layout = movements |> List.head |> MovementParser.parseLayout

        let constantLayout =
            movements
            |> List.map MovementParser.parseLayout
            |> List.forall ((=) layout)

        let workSetterFn =
            if constantLayout then
                fun idx movement ->
                    let newMvmt = MovementParser.parseMovement movement
                    setWorkMovement tracks.[idx] workName newMvmt (idx + 1) count
            else
                fun idx mvmt -> setWorkMovement tracks.[idx] workName mvmt (idx + 1) count

        movements
        |> List.mapi workSetterFn
        |> List.sequenceResultA
        |> Result.map (fun _ -> Work(workName, cTracks))

/// Searches the provided tracks for possible works.
let extractWorks (ConsecutiveTracks tracks) =
    tracks
    |> List.groupByConsecutively workFromTitle
    |> List.map
        (fun pair ->
            log.Debug("Found possible work: %A{Pair}", pair)
            pair)
    |> List.filter (fun (title, tracks) -> title.IsSome && tracks |> List.length > 1)
    |> List.map
        (fun pair ->
            log.Verbose("Filtered work: %A{Pair}", pair)
            pair)
    |> List.traverseResultM
        (fun (titleOpt, tracks) ->
            let title = titleOpt |> Option.get
            ConsecutiveTracks.Create tracks |> Result.bind (Work.Create title))

/// Applies (saves) the work data
let applyWork (Work (name, (ConsecutiveTracks tracks))) =
    log.Information("Saving metadata of work: '{Name}'", name)
    tracks |> List.traverseResultM Track.applyTags |> Result.map ignore
