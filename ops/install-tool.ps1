param([switch]$Local)
$ErrorActionPreference = 'Stop'
$src = Join-Path $PSScriptRoot '..\src'
$packageDir = Join-Path $src 'nupkg'
$project = Join-Path $src 'RalfAI\RalfAI.csproj'
$version = ([xml](Get-Content $project)).Project.PropertyGroup.Version

dotnet pack $project -c Release -o $packageDir
if ($Local) {
  Push-Location $src
  try {
    if (Test-Path '.config\dotnet-tools.json') {
      dotnet tool update RalfAI --add-source $packageDir --version $version
    } else {
      dotnet new tool-manifest
      dotnet tool install RalfAI --add-source $packageDir --version $version
    }
  } finally { Pop-Location }
} else {
  $installed = dotnet tool list --global | Select-String '^ralfai\s'
  if ($installed) { dotnet tool update --global RalfAI --add-source $packageDir --version $version }
  else { dotnet tool install --global RalfAI --add-source $packageDir --version $version }
}
Write-Host "RalfAI $version installed. Verify with: ralf --version"
