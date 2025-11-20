$mods = Get-ChildItem -Directory

foreach ($mod in $mods) {
    $buildScript = Join-Path $mod.FullName "build.ps1"
    if (Test-Path $buildScript) {
        Write-Host "Building $($mod.Name)..."
        Push-Location $mod.FullName
        ./build.ps1
        Pop-Location
    }
}
