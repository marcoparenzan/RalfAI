param([switch]$Local)
$ErrorActionPreference = 'Stop'
$src = Join-Path $PSScriptRoot '..\src'
$packageDir = Join-Path $src 'nupkg'
$project = Join-Path $src 'RalfAI.Studio.Tool\RalfAI.Studio.Tool.csproj'
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version

# Native commands do not stop the script on failure by themselves: check the exit code.
function Invoke-Dotnet {
  & dotnet @args
  if ($LASTEXITCODE -ne 0) { throw "dotnet $($args -join ' ') failed with exit code $LASTEXITCODE" }
}

# Windows cannot replace a global tool while one of its processes is running; say which ones.
function Assert-NotRunning([string]$name) {
  $running = @(Get-Process -Name $name -ErrorAction SilentlyContinue)
  if ($running.Count -eq 0) { return }
  $lines = foreach ($p in $running) {
    $started = try { $p.StartTime.ToString('yyyy-MM-dd HH:mm') } catch { 'unknown start' }
    "  PID $($p.Id) (started $started)"
  }
  throw "Cannot update the global tool while '$name' is running. Close these and run the script again:`n$($lines -join "`n")"
}

if (-not $Local) { Assert-NotRunning 'ralf-studio' }

Invoke-Dotnet pack $project -c Release -o $packageDir

if ($Local) {
  Push-Location $src
  try {
    if (-not (Test-Path '.config\dotnet-tools.json')) { Invoke-Dotnet new tool-manifest }
    $installed = dotnet tool list | Select-String '^ralfai.studio\s'
    if ($installed) { Invoke-Dotnet tool update RalfAI.Studio --add-source $packageDir --version $version }
    else { Invoke-Dotnet tool install RalfAI.Studio --add-source $packageDir --version $version }
  } finally { Pop-Location }
} else {
  $installed = dotnet tool list --global | Select-String '^ralfai.studio\s+(\S+)'
  if ($installed -and $installed.Matches[0].Groups[1].Value -eq $version) {
    Write-Host "RalfAI Studio $version is already the installed global tool - nothing to update. Run: ralf-studio [workspace]"
    return
  }
  if ($installed) { Invoke-Dotnet tool update --global RalfAI.Studio --add-source $packageDir --version $version }
  else { Invoke-Dotnet tool install --global RalfAI.Studio --add-source $packageDir --version $version }
}
Write-Host "RalfAI Studio $version installed. Run: ralf-studio [workspace]"
