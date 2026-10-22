param(
  [string]$PackageDirectory = "artifacts/packages"
)

$ErrorActionPreference = 'Stop'
$required = @(
  'DevXStream.DADS.Models',
  'DevXStream.DADS.Serialization',
  'DevXStream.DADS.Validation',
  'DevXStream.DADS.OpenApi',
  'DevXStream.DADS.Docs',
  'DevXStream.DADS.AspNetCore'
)

foreach ($packageId in $required) {
  $matches = @(Get-ChildItem -LiteralPath $PackageDirectory -Filter "$packageId.*.nupkg" -File)
  if ($matches.Count -eq 0) { throw "Missing release package for $packageId" }
  if ($matches.Count -gt 1) { throw "Expected one release package for $packageId, found $($matches.Count)" }
  $package = $matches[0].FullName
  $packageName = $matches[0].Name
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
