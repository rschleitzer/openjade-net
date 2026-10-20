# OpenJade and OpenSP for .NET

A port of James Clark's [OpenJade](https://openjade.sourceforge.net/) and
[OpenSP](https://openjade.sourceforge.net/doc/index.htm) from C++ to C#.

OpenSP is the SGML parser, OpenJade the DSSSL style engine built on it: it
reads an SGML or XML document, applies a DSSSL stylesheet
(ISO/IEC 10179), and writes the result through one of its backends. This
port keeps the names, the algorithms and the structure of the original —
one C# file for each pair of `.h` and `.cxx` — so that the C++ sources
remain the reference for how it behaves. They are in `upstream/`
(OpenJade 1.3.2, OpenSP 1.5.2).

## Packages

| Package | What it is |
|---|---|
| [OpenSP](https://www.nuget.org/packages/OpenSP) | the SGML/XML parser |
| [OpenJade](https://www.nuget.org/packages/OpenJade) | the DSSSL engine with the backends of the original: RTF, HTML, TeX, MIF, and the SGML/XML transformation backend |

Both target .NET 10.

## Command line

The repository also builds the two programs of the original:

```
dotnet run --project src/Jade -- -t rtf -d stylesheet.dsl document.xml
dotnet run --project src/Nsgmls -- document.sgm
```

`-t` selects the backend: `rtf`, `html`, `tex`, `mif`, `sgml`, `xml`, or
`fot` for the flow object tree.

## What is added to the original

One addition lives in this port itself, because the parser has to take it:
**a part of the full page model** of ISO/IEC 10179 (12.6.4, 12.6.5). The
original reads `define-page-model` and `define-column-set-model` and skips
them, and has no `page-sequence` or `column-set-sequence`. Here they are
taken in the features `page` and `multi-column`, restricted: a page model has
one region filled top to bottom, a column-set model one column subset with
its columns and `balance?`; `page-sequence` takes `initial-page-models` and
`repeat-page-models`, `column-set-sequence` takes `column-set-model` and the
display characteristics. Everything else of the page model is an error where
it is written, and a characteristic outside the subset is allowed with its
initial value only. It is all in `src/OpenJade/Style/PageModel.cs`; the
backends get the models through `FOTBuilder`. The original's backends do not
set them.

A second addition, since 1.5.0: **`line-dash`** (ISO/IEC 10179), a list of
lengths, which the original installs as a characteristic it ignores, reaches
the backends. `FOTBuilder.setLineDash` gets the offset into the pattern, the
first length of the list, and the others, alternately the lengths of dashes and
gaps. The initial value is the original's, `(0pt)`: a solid line. It is in
`src/OpenJade/Style/InheritedC.cs` (`LineDashC`); the original's backends do
not set it.

Messages of the style engine go to the messenger with their location, as in
the original; an error in a stylesheet fails the run.

The other additions live in a project of their own,
[dazzle-net](https://github.com/rschleitzer/dazzle-net), which builds on
these packages:

- **The `directory` flow object class.** The transformation backend of the
  original writes files (`entity`); `directory` creates a directory and
  makes the entities inside it relative to it, so that a stylesheet can lay
  out a whole tree of generated files — which is what makes the engine
  usable as a code generator.
- **A PDF backend**, still rudimentary: paragraphs, headings, lists and
  tables, enough for a book set with the DocBook stylesheets.

## License

The license of the original, see `LICENSE`.
Copyright © 1994–1998 James Clark, © 1999, 2002 the OpenJade Project,
© 2025 Ralf Schleitzer for the port.
