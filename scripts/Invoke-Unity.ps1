param(
    [ValidateSet('Initialize', 'Validate', 'CheckCombat', 'BuildWindowsSmoke', 'BuildWebGL')]
    [string]$Task = 'Validate',
    [string]$UnityPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$gamePath = Join-Path $projectRoot 'game'
$versionLine = Get-Content -LiteralPath (Join-Path $gamePath 'ProjectSettings\ProjectVersion.txt') |
    Where-Object { $_ -like 'm_EditorVersion: *' }
$editorVersion = $versionLine.Substring('m_EditorVersion: '.Length).Trim()
if (-not $UnityPath) {
    $UnityPath = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity $editorVersion was not found. Supply -UnityPath with the installed Unity.exe path."
}

$logDirectory = Join-Path $projectRoot 'work'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null
$logPath = Join-Path $logDirectory "unity-$($Task.ToLowerInvariant()).log"
$unityArguments = @(
    '-batchmode', '-nographics',
    '-projectPath', ('"{0}"' -f $gamePath),
    '-executeMethod', $(if ($Task -eq 'CheckCombat') { 'Riftrail.Editor.BattleChecks.Run' } else { "Riftrail.Editor.ProjectSetup.$Task" }),
    '-quit', '-logFile', ('"{0}"' -f $logPath)
)
if ($Task -eq 'BuildWebGL') { $unityArguments += @('-buildTarget', 'WebGL') }
$unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $unityArguments `
    -WindowStyle Hidden -PassThru
$unityProcess.WaitForExit()
if ($unityProcess.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $logPath) { Get-Content -LiteralPath $logPath -Tail 40 }
    throw "Unity $Task failed with exit code $($unityProcess.ExitCode). See $logPath"
}
Write-Output "Unity $Task passed. Log: $logPath"
