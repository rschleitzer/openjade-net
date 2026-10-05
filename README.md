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

The additions live in a project of their own,
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
