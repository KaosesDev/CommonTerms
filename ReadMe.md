# CommonTerms

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://example.com)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-.NET%208-blue.svg)](https://dotnet.microsoft.com/)

[Kaoses Development](http://kaosesdev.com/) - [patreon](https://patreon.com/KaosesDev)

Simple WPF tool to analyze common words and phrases across files in a directory.

## Summary

`CommonTerms` scans files in a user-selected directory and produces frequency counts for single words (unigrams) and two-word phrases (bigrams). The app supports exclusion (stop) words, configurable minimum word length, recursive scanning, and saving/loading of results and stop-word lists.

## Key features

- Directory selection with browse dialog and recursive toggle
- Configurable minimum token length (`MinWordLength`)
- Display Top N results (`DisplayTopN`) for unigrams and bigrams
- Add / remove / save / load stop words (default filename: `ExcludeWords.txt`)
- Save full results to files (`commonTerms.txt`, `commonPhrases.txt`)
- Uses `BuildCommon` and `FileListBuilder` (from `CommonTermsFunc`) for analysis
- MVVM with `CommunityToolkit.Mvvm` and file dialogs via `Ookii.Dialogs.Wpf`

## Downloads


## Requirements

- Windows 10/11
- [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

## Usage

1. Open the app and go to the Dashboard.
2. Choose or browse to a directory to analyze.
3. Toggle `Recursive` if you want subfolders included.
4. Set `Min Word Length` to exclude short tokens.
5. Click `Run Analysis` to populate `Top Unigrams` and `Top Bigrams`.
6. Use `Save` buttons to export results. Use `Add` / `Load` / `Save` stop-words to manage exclusions.

## File formats

- Stop words: plain text, one word per line (`ExcludeWords.txt`).
- Results: tab-separated `term<TAB>count` lines (e.g. `term\t42`) saved to `commonTerms.txt` / `commonPhrases.txt`.

## Development notes

- This project targets .NET 8 and uses C# 12 features.
- View models use CommunityToolkit source generators (`[ObservableProperty]`, `[RelayCommand]`).
- Main analysis is implemented by `BuildCommon` and file discovery by `FileListBuilder` (in `CommonTermsFunc`).
- UI references a WPF UI library for controls and styles; ensure referenced packages and local style libraries are available.

## Contribution

1. Fork the repo and create a topic branch: `git checkout -b feature/my-change`
2. Make changes and add tests where appropriate.
3. Commit with clear messages and push the branch.
4. Open a Pull Request describing the change and motivation.

Guidelines:
- Keep changes small and focused.
- Follow the project's coding style and run `dotnet build` before submitting.
- Add or update documentation for public changes.

## License

This project is licensed under the MIT License — see the `LICENSE` file for details.