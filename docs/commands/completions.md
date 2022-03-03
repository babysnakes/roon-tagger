## Generate Shell Tab-Completion Script - _completions_

Completions generates tab-completion script for supported shells.

Supported shells:

* PowerShell

### Configure Tab-Completion In _PowerShell_

The easiest way to configure tab completion for _roon-tagger_ is adding the
following to you powershell initialization:

```powershell
roon-tagger completions powershell | Out-String | Invoke-Expression
```
