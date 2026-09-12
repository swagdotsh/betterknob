$ErrorActionPreference = 'Stop'
try {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    if (-not (Test-Path -LiteralPath $compiler)) {
        $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
    }
    if (-not (Test-Path -LiteralPath $compiler)) { throw 'The Windows .NET Framework C# compiler was not found.' }
    $source = Join-Path $PSScriptRoot 'BetterKnob.cs'
    $buildDir = Join-Path $PSScriptRoot 'work'
    New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
    $stagedExe = Join-Path $buildDir 'betterknob.exe'
    $finalExe = Join-Path $PSScriptRoot 'betterknob.exe'
    & $compiler /nologo /target:winexe /optimize+ "/out:$stagedExe" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll $source
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed (exit $LASTEXITCODE)." }
    $testProcess = Start-Process -FilePath $stagedExe -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
    if ($testProcess.ExitCode -ne 0) { throw 'Self-tests failed. The previous executable has been preserved.' }
    try { Copy-Item -LiteralPath $stagedExe -Destination $finalExe -Force }
    catch { throw "Cannot replace betterknob.exe. Exit betterknob using its tray menu, then build again. $($_.Exception.Message)" }
    Write-Host "Build and self-tests passed: $finalExe" -ForegroundColor Green
    exit 0
} catch {
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}