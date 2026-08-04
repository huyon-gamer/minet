[CmdletBinding()]
param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'

if (-not $OutputFile) {
    $OutputFile = Join-Path $RepositoryRoot 'sbom\minet.cdx.json'
}

$projectFile = Join-Path $RepositoryRoot 'minet\minet.csproj'
$outputDirectory = Split-Path -Parent $OutputFile
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

Push-Location $RepositoryRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'Failed to restore local .NET tools.' }
}
finally {
    Pop-Location
}

$temporaryOutput = Join-Path $env:TEMP ("minet-sbom-" + [guid]::NewGuid())
New-Item -ItemType Directory -Force -Path $temporaryOutput | Out-Null
try {
    Push-Location $RepositoryRoot
    try {
        dotnet tool run dotnet-CycloneDX -- $projectFile --framework net10.0 --runtime win-x64 --output $temporaryOutput --filename minet.cdx.json --output-format Json --no-serial-number --set-name minet --set-version 1.0.0 --set-type Application
    }
    finally {
        Pop-Location
    }
    if ($LASTEXITCODE -ne 0) { throw 'CycloneDX generation failed.' }

    $sbom = Get-Content -Raw (Join-Path $temporaryOutput 'minet.cdx.json') | ConvertFrom-Json
    $sbom.metadata.PSObject.Properties.Remove('timestamp')
    $json = $sbom | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($OutputFile, (($json -replace "`r`n", "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))
}
finally {
    Remove-Item -Recurse -Force $temporaryOutput -ErrorAction SilentlyContinue
}