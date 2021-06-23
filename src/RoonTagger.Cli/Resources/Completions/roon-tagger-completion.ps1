
using namespace System.Management.Automation
using namespace System.Management.Automation.Language

Register-ArgumentCompleter -Native -CommandName 'roon-tagger' -ScriptBlock {
    param($wordToComplete, $commandAst, $cursorPosition)

    $commandElements = $commandAst.CommandElements
    $command = @(
        'roon-tagger'
        for ($i = 1; $i -lt $commandElements.Count; $i++) {
            $element = $commandElements[$i]
            if ($element -isnot [StringConstantExpressionAst] -or
                $element.StringConstantType -ne [StringConstantType]::BareWord -or
                $element.Value.StartsWith('-')) {
                break
        }
        $element.Value
    }) -join ';'

    $completions = @(switch ($command) {
        'roon-tagger' {
            [CompletionResult]::new('--version', 'version', [CompletionResultType]::ParameterName, 'Print version and exit')
            [CompletionResult]::new('--verbose', 'verbose', [CompletionResultType]::ParameterName, 'Enable verbose output')
            [CompletionResult]::new('--help', 'help', [CompletionResultType]::ParameterName, 'Prints help information')
            [CompletionResult]::new('set-tags', 'set-tags', [CompletionResultType]::ParameterValue, 'Set roon specific tags')
            [CompletionResult]::new('edit-titles', 'edit-titles', [CompletionResultType]::ParameterValue, 'Edit the tiles of all provided tracks')
            [CompletionResult]::new('credits', 'credits', [CompletionResultType]::ParameterValue, 'Add/Delete track credits')
            [CompletionResult]::new('configure', 'configure', [CompletionResultType]::ParameterValue, 'Configure roon-tagger')
            [CompletionResult]::new('view', 'view', [CompletionResultType]::ParameterValue, 'View file tags')
            [CompletionResult]::new('extract-works', 'extract-works', [CompletionResultType]::ParameterValue, 'Identify work/movements in the provided tracks')
            break
        }
        'roon-tagger;edit-titles' {
            [CompletionResult]::new('--verbose', 'verbose', [CompletionResultType]::ParameterName, 'Enable verbose output')
            [CompletionResult]::new('--help', 'help', [CompletionResultType]::ParameterName, 'Prints help information')
            break
        }
        'roon-tagger;set-tags' {
            [CompletionResult]::new('--title', 'title', [CompletionResultType]::ParameterName, 'The title to set')
            [CompletionResult]::new('--import-date', 'import-date', [CompletionResultType]::ParameterName, 'The date the track was imported to roon')
            [CompletionResult]::new('--release-date', 'release-date', [CompletionResultType]::ParameterName, 'The release date of the track/album')
            [CompletionResult]::new('--year', 'year', [CompletionResultType]::ParameterName, 'Album release date')
            [CompletionResult]::new('--verbose', 'verbose', [CompletionResultType]::ParameterName, 'Enable verbose output')
            [CompletionResult]::new('--help', 'help', [CompletionResultType]::ParameterName, 'Prints help information')
            break
        }
        'roon-tagger;credits' {
            [CompletionResult]::new('--add', 'add', [CompletionResultType]::ParameterName, 'Add the provided credit')
            [CompletionResult]::new('--del', 'del', [CompletionResultType]::ParameterName, 'Delete an existing credit')
            [CompletionResult]::new('--skip-verification', 'skip-verification', [CompletionResultType]::ParameterName, 'Skip role verification (against role list)')
            [CompletionResult]::new('--year', 'year', [CompletionResultType]::ParameterName, 'Album release date')
            [CompletionResult]::new('--verbose', 'verbose', [CompletionResultType]::ParameterName, 'Enable verbose output')
            [CompletionResult]::new('--help', 'help', [CompletionResultType]::ParameterName, 'Prints help information')
            break
        }
        'roon-tagger;configure' {
            [CompletionResult]::new('--log-file', 'log-file', [CompletionResultType]::ParameterName, 'Default log file')
            [CompletionResult]::new('--log-level', 'log-level', [CompletionResultType]::ParameterName, 'Default log level')
            [CompletionResult]::new('--editor', 'editor', [CompletionResultType]::ParameterName, 'Editor to use for editing tags file (without arguments)')
            [CompletionResult]::new('--editor-with-args', 'editor-with-args', [CompletionResultType]::ParameterName, 'Editor to use for editing tags file (with arguments)')
            [CompletionResult]::new('--reset-editor', 'reset-editor', [CompletionResultType]::ParameterName, 'Reset the editor configuration')
            [CompletionResult]::new('--show', 'show', [CompletionResultType]::ParameterName, 'Show current configuration')
            [CompletionResult]::new('--verbose', 'verbose', [CompletionResultType]::ParameterName, 'Enable verbose output')
            [CompletionResult]::new('--help', 'help', [CompletionResultType]::ParameterName, 'Prints help information')
            break
        }
        'roon-tagger;configure' {
            [CompletionResult]::new('--raw-credits', 'raw-credits', [CompletionResultType]::ParameterName, 'View raw credits')
            [CompletionResult]::new('--verbose', 'verbose', [CompletionResultType]::ParameterName, 'Enable verbose output')
            [CompletionResult]::new('--help', 'help', [CompletionResultType]::ParameterName, 'Prints help information')
            break
        }
        'roon-tagger;extract-works' {
            [CompletionResult]::new('--add-roman-numerals', 'add-roman-numerals', [CompletionResultType]::ParameterName, 'Adds Roman numerals to movement names')
            [CompletionResult]::new('--verbose', 'verbose', [CompletionResultType]::ParameterName, 'Enable verbose output')
            [CompletionResult]::new('--help', 'help', [CompletionResultType]::ParameterName, 'Prints help information')
            break
        }
    })

    $completions.Where{ $_.CompletionText -like "$wordToComplete*" } |
        Sort-Object -Property ListItemText
}
