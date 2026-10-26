# Draws the Thunderstore icon (256x256 PNG) from tools/IconDraw.cs.
# Usage, from the repo root in PowerShell:  . .\tools\make-icon.ps1   (or paste these lines into a prompt)
param([string]$OutPath = (Join-Path $PSScriptRoot "..\thunderstore\icon.png"))

Add-Type -Path (Join-Path $PSScriptRoot "IconDraw.cs") -ReferencedAssemblies System.Drawing
New-Item -ItemType Directory -Force (Split-Path $OutPath) | Out-Null
[IconDraw]::Render([System.IO.Path]::GetFullPath($OutPath))
Write-Output "Wrote $OutPath"
