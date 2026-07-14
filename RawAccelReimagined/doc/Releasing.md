# Publishing Raw Accel Reimagined

The in-app updater reads the latest stable release from this repository and requires these two assets:

- `Raw-Accel-Reimagined-Windows-x64.zip`
- `Raw-Accel-Reimagined-Windows-x64.zip.sha256`

Both assets are produced automatically by the release workflow. The ZIP intentionally excludes personal settings and hardware identifiers.

## Release procedure

1. Update `AssemblyVersion` and `AssemblyFileVersion` in both application projects using the same `MAJOR.MINOR.PATCH` version.
2. Update `release-notes.json` with the matching version and localized English/Portuguese notes, then update `RELEASE_NOTES.md` for the GitHub Release page.
3. Build and test the application and updater locally.
4. Commit and push all release changes to `main`.
5. Create and push a matching annotated tag, for example `v1.9.0`.
6. The `Build and publish release` GitHub Actions workflow builds the projects, creates the Windows ZIP and SHA-256 file, and publishes the GitHub Release using `RELEASE_NOTES.md`.
7. Confirm that the release is not marked as a draft or prerelease and that both required assets are present.
8. Test **Check for Updates** from the previous released version before announcing the release.

Local package validation is also available:

```powershell
.\scripts\build-release.ps1 -Version 1.8.0
```

Generated packages are written to `artifacts/`, which is excluded from Git.

Local Release builds place the application and updater in `RawAccelReimagined/`. The legacy `rawaccel.exe` at the repository root is intentionally excluded from modern Release packages.

## Safety guarantees

The updater downloads only the exact asset names listed above from the repository's latest stable GitHub Release. It verifies SHA-256 before closing the application, extracts into a temporary staging directory, rejects unsafe ZIP paths, backs up files before replacing them, and rolls back if installation fails.

The following local data is never overwritten:

- `settings.json`
- `.config`
- `.reimagined.config`
- `backups/`
- `.git/`
