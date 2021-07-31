## Configure Cli Behavior - _configure_

Configure lets you control certain tool behaviors:

* [Logging](#logging)
* [Editor](#editor)

### Logging

Logging in roon-tagger is always written to a file and never interferes with the
console output. By default, the log file is `roon-tagger.log` in the current
working directory and the log level is `None`. While the default config will not
produce any logs (because of the `None` level) if you specify higher verbosity
level as command arguments it will log to the log file. You can also specify
absolute path as the logging file but this is less recommended.

### Editor

Editor can be used in various places in the app for quickly editing specific tag
for many files (A good example is editing the titles of classical music to
quickly format the tracks title to match _work: movement_ format). By default there's
no editor configured and whenever you need to edit a file it will create a
temporary file in the current directory, let you edit it outside of the terminal
and then press `Enter` to read the updated file. However if you want to edit the
file in the foreground (with either a terminal app or a GUI window) you need to
configure an editor.

There are certain limitations when configuring the editor:

* As stated above, the editor command must run in the foreground. That means
  that it must not return until the file is saved and the editor is exited (so
  `notepad` is not a good editor as it returns immediately without waiting for
  you to finish editing).
* The editor command should accept the edited file as the last parameter (the
  filename will be automatically appended as the last argument).

There are also certain guidelines that should be followed to avoid issues:

* Do not rely on environment variables (e.g. $PATH) or shell expansions (e.g.
  `~`) in your command or parameters. They might not behave the same on all
  platforms.
* Because of the above, it's advisable to use full path of the command (try
  `which <CMD>` in unix or `Get-Command <CMD>` in powershell to get full path).

### Example Usage

Configure log file with level and alternate log file (in working directory):

```sh
roon-tagger configure --log-file output.log --log-level debug
```

Configure editor command on unix (without arguments - file name will be appended
at runtime):

```sh
roon-tagger configure --editor /usr/bin/nvim
```

Configure editor command on Windows/Powershell with required arguments (file
name will be appended as last argument at runtime). Note that we supply two
parameters to `code` (`-w` for editing in foreground and `-n` for opening in new
window):

```powershell
roon-tagger configure --editor-with-args 'C:\Path\To\Code.cmd' '-w,-n'
```

Get full usage:

```bash
roon-tagger configure -h
```
