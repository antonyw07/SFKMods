param(
    [string]$Version = "0.1.0"
)

$modName = Split-Path -Leaf (Get-Location)
$buildDir = "$modName/bin/Release"
$outputDll = "$buildDir/$modName.dll"
$zipName = "$modName-$Version.zip"

Write-Host "=== Building $modName ==="

# 1. Build the solution
dotnet build "$modName.sln" -c Release

if (!(Test-Path $outputDll)) {
    throw "DLL not found: $outputDll"
}

# 2. Generate manifest.json
$template = Get-Content "manifest.template.json" -Raw
$manifest = $template.Replace('$MODNAME$', $modName)
$manifest = $manifest.Replace('$VERSION$', $Version)
$manifest | Out-File -Encoding UTF8 "manifest.json"

# 3. Required files check
$required = @("icon.png", "README.md", "manifest.json")
foreach ($f in $required) {
    if (!(Test-Path $f)) { throw "$f missing!" }
}

# 4. Create zip package
$zipContent = @(
    "icon.png",
    "README.md",
    "manifest.json",
    $outputDll
)

if (Test-Path $zipName) { Remove-Item $zipName }

Compress-Archive -Path $zipContent -DestinationPath $zipName

Write-Host "=== Package built: $zipName ==="
