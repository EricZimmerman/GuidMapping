# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A small `netstandard2.0` NuGet library that maps GUIDs to human-readable names. Public API is the static `GuidMapping.GuidMapping` class: `GetDescriptionFromGuid(string|Guid)` (returns `"Unmapped GUID: {guid}"` on a miss) and `TryGetDescription(string|Guid, out string)`. String inputs accept anything `Guid.TryParse` does (braces, any case, `N` format, surrounding whitespace).

## Commands

The SDK is pinned in `global.json` (.NET 10). The library targets `netstandard2.0` (default C# 7.3 — avoid newer syntax there); tests target `net10.0` and use NUnit 5.

```bash
dotnet build
dotnet test                                        # CI runs: dotnet test --configuration Release
dotnet test --filter "FullyQualifiedName~DataFileTests"   # just the data-file validation
dotnet pack --configuration Release
```

## How it works

- Data lives in `GuidMapping/Resources/GuidToName.txt`, one `guid|description` per line (~14k lines). Most changes to this repo are additions to this file.
- It is a plain `EmbeddedResource` with `LogicalName` `GuidMapping.GuidToName.txt`, read once by the static initializer into a `Dictionary<Guid, string>` (first entry for a GUID wins).
- `TryParseLine` (internal, exposed to tests via `InternalsVisibleTo`) is the single parser for both the loader and the tests.
- GUID families (one GUID per account or install: OneDrive's `04271989-*` and Creative Cloud's `0e270daa-1be6-48f2-ac49-*` sync roots) are mapped by prefix in `GuidMapping/Resources/GuidPrefixToName.txt` (`prefix|description`; the start of a lowercase `D` format GUID with its hyphens, at least 8 characters; parsed by `TryParsePrefixLine`). A prefix is only used when the exact GUID is not in `GuidToName.txt`, and the longest matching prefix wins.

## Editing the data file

`DataFileTests` (and `PrefixFileTests` for the prefix file) enforce the format, so run the tests after any data change:
- UTF-8 with BOM, CRLF line endings only.
- Canonical lines: lowercase hyphenated GUID, no braces, no padding around either field, non-empty description, no control characters.
- No duplicate GUIDs.

## Versioning and release

- MinVer sets the version from the git tag (bare tags like `2026.10.0`); there is no `<Version>` in the csproj. Untagged commits build as `x.y.z-alpha.0.N`.
- To release: push to `master`, then `git tag <ver> && git push origin <ver> && gh release create <ver> --verify-tag --generate-notes`.
- `.github/workflows/publish.yml` packs, validates and tests on PRs and pushes to `master`, and on a published GitHub Release pushes to NuGet.org using trusted publishing (`NuGet/login` with the `NUGET_USER` repo variable; no API key secret).
