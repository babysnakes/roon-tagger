## Add/Remove Personnel Credits - _credits_

The credits command adds/removes personnel credits in a custom tag. It allows
shortcut for adding personnel (insert each person once with a comma separated
list of credits - e.g. `--add 'Some Name' 'Vocals,Guitar'` will add two credits
for *Some Name*: *Guitar* and *Vocals*). It also validates the input against a
list of supported roles (the list is available in [Roon's wiki][wiki]).

> While role validation can be disabled, keep in mind that unsupported
> roles are unlikely to be displayed in Roon.

### Example Usage

Add credits for *John Coltrane* playing *Soprano Saxophone* and *Tenor
Saxophone* (windows powershell example):

```powershell
roon-tagger credits --add 'John Coltrane' 'Soprano Saxophone,Tenor Saxophone' (gi *.flac)
```

Add multiple credits in a single command (unix multiline example):

```bash
roon-tagger credits 
    --add "Jean-Marie Machado" "Piano,Arranger" \
    --add "Didier Ithursarry" Accordion \
    --add "François Thuillier" Tuba \
    --add "Stéphane Guillaume" Flute \
    --add "Jean-charles Richard" "Soprano Saxophone,Baritone Saxophone" \
    --add "Cecile Grenier" Viola \
    *.flac
```

Deleting credits is a little more involved, there's no shortcut. Let's delete
the *Soprano Saxophone* credit from a previous example. First let's display
credits in the matching format for deletion: 

```sh
roon-tagger view --raw-credits somefile.flac

# Which produces similar output to this:

 ┌─Info: somefile.flac───────────────────────────────┐
 │                                                   │
 │ Title           Some Title                        │
 │ Track Number    2                                 │
 │ Disc Number     1                                 │
 │ Import Date     2012-01-02                        │
 │ Release Date    2012-12-12                        │
 │ Year            1979                              │
 │                                                   │
 │ Credits:                                          │
 │                 John Coltrane - Soprano Saxophone │
 │                 John Coltrane - Tenor Saxophone   │
 └───────────────────────────────────────────────────┘
```

Now, let's copy the *Soprano Saxophone* credit (without surrounding spaces) and
use it as a value for `--dell`:

```sh
roon-tagger credits --del 'John Coltrane - Soprano Saxophone' somefile.flac
```

[wiki]: https://help.roonlabs.com/portal/en/kb/articles/credit-roles
