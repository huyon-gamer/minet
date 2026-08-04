[CmdletBinding()]
param(
    [string]$RepositoryRoot,
    [string]$OutputFile
)

$ErrorActionPreference = 'Stop'

if (-not $RepositoryRoot) {
    $RepositoryRoot = Split-Path -Parent $PSScriptRoot
}

if (-not $OutputFile) {
    $OutputFile = Join-Path (Join-Path $RepositoryRoot 'sbom') 'minet.cdx.json'
}

$projectFile = Join-Path (Join-Path $RepositoryRoot 'minet') 'minet.csproj'
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

    $sbom = Get-Content -Raw -Encoding UTF8 (Join-Path $temporaryOutput 'minet.cdx.json') | ConvertFrom-Json
    $sbom.metadata.PSObject.Properties.Remove('timestamp')
    foreach ($component in @($sbom.components)) {
        if ($component.dependsOn -and $component.dependsOn.Count -eq 0) {
            $component.PSObject.Properties.Remove('dependsOn')
        }
    }
    foreach ($dependency in @($sbom.dependencies)) {
        if ($dependency.dependsOn -and $dependency.dependsOn.Count -eq 0) {
            $dependency.PSObject.Properties.Remove('dependsOn')
        }
        if ($dependency.dependsOn) {
            $dependency.dependsOn = @($dependency.dependsOn | Sort-Object)
        }
    }
    if ($sbom.dependencies) {
        $sbom.dependencies = @($sbom.dependencies | Sort-Object -Property ref)
    }
    $json = $sbom | ConvertTo-Json -Depth 100
    [System.IO.File]::WriteAllText($OutputFile, (($json -replace "`r`n", "`n") + "`n"), [System.Text.UTF8Encoding]::new($false))
}
finally {
    Remove-Item -Recurse -Force $temporaryOutput -ErrorAction SilentlyContinue
}