param([switch]$Local)
$ErrorActionPreference = 'Stop'
$src = Join-Path $PSScriptRoot '..\src'
$packageDir = Join-Path $src 'nupkg'
$project = Join-Path $src 'RalfAI.Studio.Tool\RalfAI.Studio.Tool.csproj'
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version

dotnet pack $project -c Release -o $packageDir
if ($Local) {
  Push-Location $src
  try {
    if (-not (Test-Path '.config\dotnet-tools.json')) { dotnet new tool-manifest }
    $installed = dotnet tool list | Select-String '^ralfai.studio\s'
    if ($installed) { dotnet tool update RalfAI.Studio --add-source $packageDir --version $version }
    else { dotnet tool install RalfAI.Studio --add-source $packageDir --version $version }
  } finally { Pop-Location }
} else {
  $installed = dotnet tool list --global | Select-String '^ralfai.studio\s'
  if ($installed) { dotnet tool update --global RalfAI.Studio --add-source $packageDir --version $version }
  else { dotnet tool install --global RalfAI.Studio --add-source $packageDir --version $version }
}
Write-Host "RalfAI Studio $version installed. Run: ralf-studio [workspace]"
