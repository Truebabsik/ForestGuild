<#
.SYNOPSIS
    Собирает лист моделей: режет кадр студии на плитки и подписывает их.

.DESCRIPTION
    Вторая половина съёмки листа. Первую делает PropStudio.Shoot() (Assets/Scripts/PropStudio.cs):
    он выставляет модели в сетку, снимает кадр и пишет два списка — прямоугольники плиток в кадре
    и габариты моделей в метрах. Этот скрипт режет кадр по прямоугольникам, раскладывает плитки
    в сетку и подписывает имя с габаритом; результат — docs/images/models_sheet.jpg.

    Запускать через блок, а не файлом: политика выполнения в этом проекте запрещает .ps1.

        $sb = [scriptblock]::Create([IO.File]::ReadAllText(
            ".dsh/skills/unity-model-optimize/scripts/compose_sheet.ps1", [Text.Encoding]::UTF8))
        & $sb -Src Temp/studio_sheet.png -Rects Temp/studio_rects.txt `
              -Sizes Temp/studio_models.txt -Out docs/images/models_sheet.jpg

    ЛОВУШКА, на которую я наступил дважды: PowerShell не различает регистр в именах переменных,
    поэтому ни одна локальная переменная не должна называться как параметр ($Src, $Rects, $Sizes,
    $Out, $Cols, $CellW, $CellH, $ImgH). Именно так пропадал путь к списку прямоугольников:
    локальная $rects затирала параметр $Rects.
#>
param(
    [Parameter(Mandatory = $true)][string]$Src,
    [Parameter(Mandatory = $true)][string]$Rects,
    [Parameter(Mandatory = $true)][string]$Sizes,
    [Parameter(Mandatory = $true)][string]$Out,
    [int]$Cols = 7,
    [int]$CellW = 300,
    [int]$CellH = 265,
    [int]$ImgH = 220,
    [int]$Quality = 88
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$frame = [System.Drawing.Image]::FromFile((Resolve-Path $Src).Path)
$tileLines = @(Get-Content $Rects | Where-Object { $_ -match '\|' })
if ($tileLines.Count -eq 0) { throw "нет ни одной плитки в $Rects" }

$sizeMap = @{}
foreach ($line in (Get-Content $Sizes)) {
    $parts = $line.Split('|')
    if ($parts.Count -ge 4) { $sizeMap[$parts[0]] = ($parts[1] + ' x ' + $parts[2] + ' x ' + $parts[3]) }
}

$rows = [int][math]::Ceiling($tileLines.Count / $Cols)
$outW = $Cols * $CellW
$outH = $rows * $CellH

$bmp = New-Object System.Drawing.Bitmap($outW, $outH)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(18, 18, 20))
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$font = New-Object System.Drawing.Font("Segoe UI", 13)
$fontSmall = New-Object System.Drawing.Font("Segoe UI", 11)
$brush = [System.Drawing.Brushes]::WhiteSmoke
$brushDim = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(170, 170, 175))
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::FromArgb(60, 60, 65))

$index = 0
$drawn = 0
$clipped = 0
foreach ($line in $tileLines) {
    $parts = $line.Split('|')
    $name = $parts[0]
    $x = [int]$parts[1]; $y = [int]$parts[2]; $w = [int]$parts[3]; $h = [int]$parts[4]

    # a rect that leaves the frame is a bug in the projection, not something to paper over
    if ($x -lt 0 -or $y -lt 0 -or ($x + $w) -gt $frame.Width -or ($y + $h) -gt $frame.Height) {
        Write-Warning ("{0}: прямоугольник {1},{2} {3}x{4} выходит за кадр {5}x{6}" -f $name, $x, $y, $w, $h, $frame.Width, $frame.Height)
        $clipped++
        $index++
        continue
    }
    if ($w -lt 4 -or $h -lt 4) { $index++; continue }

    $col = $index % $Cols
    $row = [int][math]::Floor($index / $Cols)
    $cx = $col * $CellW
    $cy = $row * $CellH

    # вписать плитку в ячейку, сохранив пропорции
    $scale = [math]::Min(($CellW - 16) / $w, $ImgH / $h)
    $dw = [int]($w * $scale)
    $dh = [int]($h * $scale)
    $dx = $cx + [int](($CellW - $dw) / 2)
    $dy = $cy + [int](($ImgH - $dh) / 2) + 4
    $g.DrawImage($frame, (New-Object System.Drawing.Rectangle($dx, $dy, $dw, $dh)),
        (New-Object System.Drawing.Rectangle($x, $y, $w, $h)), [System.Drawing.GraphicsUnit]::Pixel)

    $label = $name -replace '^Studio_', ''
    $g.DrawString($label, $font, $brush, ($cx + 8), ($cy + $ImgH + 6))
    if ($sizeMap.ContainsKey($name)) {
        $g.DrawString($sizeMap[$name] + ' m', $fontSmall, $brushDim, ($cx + 8), ($cy + $ImgH + 28))
    }
    $g.DrawRectangle($pen, $cx, $cy, ($CellW - 1), ($CellH - 1))
    $index++
    $drawn++
}

$codec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
    Where-Object { $_.MimeType -eq 'image/jpeg' }
$encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
$encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter(
    [System.Drawing.Imaging.Encoder]::Quality, [long]$Quality)

$outPath = [IO.Path]::GetFullPath($Out)
$bmp.Save($outPath, $codec, $encoderParams)
$g.Dispose(); $bmp.Dispose(); $frame.Dispose()

Write-Output ("{0}: плиток {1} из {2}, лист {3}x{4}, {5} КиБ, вне кадра {6}" -f `
    (Split-Path $outPath -Leaf), $drawn, $tileLines.Count, $outW, $outH, [int]((Get-Item $outPath).Length / 1KB), $clipped)
