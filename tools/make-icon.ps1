# Draws the placeholder Thunderstore icon (256x256 PNG): a minimap with a tracked home marker on its edge.
# Usage: powershell -File tools/make-icon.ps1 [output path]
param([string]$OutPath = (Join-Path $PSScriptRoot "..\thunderstore\icon.png"))

Add-Type -AssemblyName System.Drawing

$size = 256
$bmp = New-Object System.Drawing.Bitmap $size, $size
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::FromArgb(255, 30, 37, 48))

function Brush($r, $gr, $b) { New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, $r, $gr, $b)) }
function Pen($r, $gr, $b, $w) { New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(255, $r, $gr, $b)), $w }
function Pt($x, $y) { New-Object System.Drawing.PointF $x, $y }

# Minimap: dark water with a patch of land, framed.
$g.FillRectangle((Brush 58 74 92), 34, 34, 188, 188)
$g.FillEllipse((Brush 86 112 62), 60, 70, 110, 120)
$g.FillEllipse((Brush 96 124 70), 90, 110, 70, 60)
$g.DrawRectangle((Pen 216 207 181 8), 34, 34, 188, 188)

# Player arrow in the centre.
$g.FillPolygon((Brush 245 200 40), [System.Drawing.PointF[]]@((Pt 118 142), (Pt 128 112), (Pt 138 142), (Pt 128 134)))

# Home marker on the right edge: house icon, outward arrow, badge.
$white = Brush 245 245 240
$g.FillPolygon($white, [System.Drawing.PointF[]]@((Pt 160 120), (Pt 182 100), (Pt 204 120)))
$g.FillRectangle($white, 166, 119, 32, 26)
$g.FillRectangle((Brush 58 74 92), 178, 130, 8, 15)
$g.FillPolygon($white, [System.Drawing.PointF[]]@((Pt 212 108), (Pt 238 124), (Pt 212 140)))
$g.FillEllipse((Brush 20 20 20), 190, 88, 26, 26)
$g.FillEllipse((Brush 255 191 51), 193, 91, 20, 20)

$g.Dispose()
New-Item -ItemType Directory -Force (Split-Path $OutPath) | Out-Null
$bmp.Save((Join-Path (Resolve-Path (Split-Path $OutPath)).Path (Split-Path $OutPath -Leaf)), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Output "Wrote $OutPath"
