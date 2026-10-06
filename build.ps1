$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) {
    $compilerPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'Install .NET Framework 4 or later to build PixelPet.' }
$appPath = Join-Path $projectRoot 'PixelPet.exe'
$sourcePath = Join-Path $projectRoot 'Pet.cs'
& $compilerPath /nologo /target:winexe "/out:$appPath" /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $sourcePath
if ($LASTEXITCODE -ne 0) { throw 'PixelPet compilation failed. Close the running application before rebuilding.' }
Write-Host 'Built PixelPet.exe. Double-click it to launch.'

