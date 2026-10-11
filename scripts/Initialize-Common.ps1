[CmdletBinding()]
param([string]$CommonRoot = (Join-Path $PSScriptRoot '../../Common'), [switch]$Pinned)
$ErrorActionPreference = 'Stop'
$CommonRoot = [IO.Path]::GetFullPath($CommonRoot)
New-Item -ItemType Directory -Force -Path $CommonRoot | Out-Null
$baselines = Get-Content (Join-Path $PSScriptRoot '../common-baselines.json') -Raw | ConvertFrom-Json
foreach ($property in $baselines.PSObject.Properties) {
    $name = $property.Name
    $target = Join-Path $CommonRoot $name
    if (Test-Path $target) {
        if (!(Test-Path (Join-Path $target '.git'))) { throw "$target exists but is not a Git checkout." }
        Write-Host "Preserving existing checkout: $name"
        $head = & git -C $target rev-parse HEAD
        if ($LASTEXITCODE -ne 0) { throw "Cannot read $name HEAD." }
        if ($head -ne $property.Value) { Write-Warning "$name differs from the reviewed API baseline. Validate locally before use." }
        continue
    }
    & git clone --branch main "https://github.com/fitzroywright/$name.git" $target
    if ($LASTEXITCODE -ne 0) { throw "Clone failed for $name." }
    if ($Pinned) {
        & git -C $target checkout --detach $property.Value
        if ($LASTEXITCODE -ne 0) { throw "Baseline checkout failed for $name." }
    }
}
Write-Host "CommonRoot: $CommonRoot"
