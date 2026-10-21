param(
  [string]$PackageDirectory = "artifacts/packages"
)

$ErrorActionPreference = 'Stop'
$required = @(
  'DevXStream.DADS.Models.1.0.0.nupkg',
  'DevXStream.DADS.Serialization.1.0.0.nupkg',
  'DevXStream.DADS.Validation.1.0.0.nupkg',
  'DevXStream.DADS.OpenApi.1.0.0.nupkg',
  'DevXStream.DADS.Docs.1.0.0.nupkg'
)

foreach ($packageName in $required) {
  $package = Join-Path $PackageDirectory $packageName
  if (-not (Test-Path -LiteralPath $package)) { throw "Missing release package: $packageName" }
  $archive = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $package))
  try {
    $names = @($archive.Entries | ForEach-Object FullName)
    if ($names -notcontains 'README.md') { throw "$packageName does not contain README.md" }
    $nuspec = $archive.Entries | Where-Object { $_.FullName -like '*.nuspec' } | Select-Object -First 1
    $reader = [System.IO.StreamReader]::new($nuspec.Open())
    try { $content = $reader.ReadToEnd() } finally { $reader.Dispose() }
    if ($content -notmatch '<license type="expression">MIT</license>') { throw "$packageName is missing MIT package metadata" }
    if ($content -notmatch '<repository type="git"') { throw "$packageName is missing repository metadata" }
    Write-Output "PASS $packageName metadata and README"
  } finally { $archive.Dispose() }
}
