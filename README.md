# minet
 Makurosofuto Garnet bēsu no rōkarukībaryūsutoa. SQLite eizoku-ka rogu, AOF, jidō bakkuappu kinō o tōsai. 65 A local key-value store based on Microsoft Garnet. It features an SQLite-based persistence log, AOF, and automatic backup capabilities.

** I was worried about licensing issues, so I tried using GitHub Copilot, but I hit my credit limit. I'll update this again later... **
## License

minet is available under the [MIT License](LICENSE). Third-party dependency
notices are available in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
The Windows x64 release also includes a [CycloneDX SBOM](sbom/minet.cdx.json).

## Releases

Tags beginning with `v` create a self-contained Windows x64 GitHub Release.
Each release ZIP includes `LICENSE` and `THIRD-PARTY-NOTICES.md`; the matching
CycloneDX SBOM is attached to the GitHub Release.

## Dependency updates

When intentionally changing a NuGet dependency, update and review the lock
file, notices, and SBOM together:

```powershell
dotnet restore minet/minet.csproj
./scripts/Generate-ThirdPartyNotices.ps1
./scripts/Generate-Sbom.ps1
git diff -- minet/packages.lock.json THIRD-PARTY-NOTICES.md sbom/minet.cdx.json
```

`diskann-garnet` 4.0.0 lacks license metadata and bundled license files. Its
reviewed, version-specific upstream attribution is recorded in
`scripts/LicenseOverrides.json`; review or replace that record when changing
the package version.
