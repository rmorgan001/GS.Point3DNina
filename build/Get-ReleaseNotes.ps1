# Writes the CHANGELOG.md section for a release tag (v1.2.3.4 -> "## 1.2.3.4") to a file, to be used as the release notes.
# Writes nothing, and says so, when the changelog has no entry for the tag, so the caller can fall back to generated notes.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Tag, [Parameter(Mandatory)][string]$Path, [string]$Changelog = (Join-Path (Split-Path $PSScriptRoot) 'CHANGELOG.md'))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
Remove-Item -LiteralPath $Path -ErrorAction SilentlyContinue
$version = $Tag.TrimStart('v')
$lines = Get-Content -LiteralPath $Changelog
$start = [array]::IndexOf($lines, "## $version")
if ($start -lt 0) { Write-Host "No '## $version' entry in $Changelog."; return }
$body = New-Object System.Collections.Generic.List[string]
foreach ($line in $lines[($start + 1)..($lines.Count - 1)]) {
    if ($line -match '^## ') { break }
    $body.Add($line)
}
$text = ($body -join "`n").Trim()
if (!$text) { Write-Host "The '## $version' entry in $Changelog is empty."; return }
Set-Content -LiteralPath $Path -Value $text -Encoding UTF8
Write-Host "Release notes for $Tag written to $Path."
