Add-Type -AssemblyName System.Drawing
$w = 1120
$h = 840
$bmp = New-Object System.Drawing.Bitmap $w, $h
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::White)
$pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::Black), 2
$font = [System.Drawing.Font]::new("Segoe UI", 10.5)
$fontBold = [System.Drawing.Font]::new("Segoe UI", 10.5, [System.Drawing.FontStyle]::Bold)
$brush = [System.Drawing.Brushes]::Black
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = [System.Drawing.StringAlignment]::Center
$fmt.LineAlignment = [System.Drawing.StringAlignment]::Center

function Draw-Box {
    param([int]$x, [int]$y, [int]$width, [int]$height, [string]$text, [bool]$bold = $false)
    $r = New-Object System.Drawing.Rectangle $x, $y, $width, $height
    $g.DrawRectangle($pen, $r)
    $f = if ($bold) { $fontBold } else { $font }
    $rf = New-Object System.Drawing.RectangleF([float]$x, [float]$y, [float]$width, [float]$height)
    $g.DrawString($text, $f, $brush, $rf, $fmt)
    return @{
        X     = $x; Y = $y; W = $width; H = $height
        CX    = [int]($x + $width / 2)
        TopY  = $y
        BotY  = [int]($y + $height)
        Left  = $x
        Right = [int]($x + $width)
    }
}

function Draw-PolylinePts {
    param([System.Drawing.PointF[]]$pts)
    for ($i = 0; $i -lt $pts.Length - 1; $i++) {
        $g.DrawLine($pen, $pts[$i], $pts[$i + 1])
    }
}

function Draw-Arrowhead {
    param([double]$x1, [double]$y1, [double]$x2, [double]$y2)
    $ang = [Math]::Atan2($y2 - $y1, $x2 - $x1)
    $sz = 9.0
    $a1 = $ang + 2.55
    $a2 = $ang - 2.55
    $ax1 = $x2 + $sz * [Math]::Cos($a1)
    $ay1 = $y2 + $sz * [Math]::Sin($a1)
    $ax2 = $x2 + $sz * [Math]::Cos($a2)
    $ay2 = $y2 + $sz * [Math]::Sin($a2)
    $g.DrawLine($pen, $x2, $y2, $ax1, $ay1)
    $g.DrawLine($pen, $x2, $y2, $ax2, $ay2)
}

function Pt([float]$x, [float]$y) { return [System.Drawing.PointF]::new($x, $y) }

function T([int[]]$codes) { return -join ($codes | ForEach-Object { [char]$_ }) }

# --- Блоки (подписи через Unicode, файл скрипта остается ASCII-совместимым) ---
$login = Draw-Box 360 22 400 44 (T @(0x041E,0x043A,0x043D,0x043E,0x0020,0x0430,0x0432,0x0442,0x043E,0x0440,0x0438,0x0437,0x0430,0x0446,0x0438,0x0438,0x0020,0x0028,0x004C,0x006F,0x0067,0x0069,0x006E,0x0046,0x006F,0x0072,0x006D,0x0029)) $true
$main  = Draw-Box 360 92 400 44 (T @(0x0413,0x043B,0x0430,0x0432,0x043D,0x043E,0x0435,0x0020,0x043E,0x043A,0x043D,0x043E,0x0020,0x0028,0x004D,0x0061,0x0069,0x006E,0x0046,0x006F,0x0072,0x006D,0x0029)) $true

$bx = 48
$bw = 208
$bh = 46
$gap = 20
$yMod = 188
$m1 = Draw-Box $bx $yMod $bw $bh (T @(0x041C,0x043E,0x0434,0x0443,0x043B,0x044C,0x0020,0x00AB,0x0422,0x0435,0x0445,0x043D,0x0438,0x043A,0x0430,0x00BB))
$m2 = Draw-Box ($bx + ($bw + $gap)) $yMod $bw $bh (T @(0x041C,0x043E,0x0434,0x0443,0x043B,0x044C,0x0020,0x00AB,0x0417,0x0430,0x044F,0x0432,0x043A,0x0438,0x0020,0x043D,0x0430,0x0020,0x0440,0x0435,0x043C,0x043E,0x043D,0x0442,0x00BB))
$m3 = Draw-Box ($bx + 2 * ($bw + $gap)) $yMod $bw $bh (T @(0x041C,0x043E,0x0434,0x0443,0x043B,0x044C,0x0020,0x00AB,0x041F,0x043B,0x0430,0x043D,0x043E,0x0432,0x043E,0x0435,0x0020,0x0422,0x041E,0x00BB))
$m4 = Draw-Box ($bx + 3 * ($bw + $gap)) $yMod $bw $bh (T @(0x041C,0x043E,0x0434,0x0443,0x043B,0x044C,0x0020,0x00AB,0x0421,0x043A,0x043B,0x0430,0x0434,0x0020,0x0437,0x0430,0x043F,0x0447,0x0430,0x0441,0x0442,0x0435,0x0439,0x00BB))

$repW = 2 * $bw + $gap
$repX = $bx
$repY = $yMod + $bh + 36
$rep = Draw-Box $repX $repY $repW $bh (T @(0x041C,0x043E,0x0434,0x0443,0x043B,0x044C,0x0020,0x00AB,0x041E,0x0442,0x0447,0x0435,0x0442,0x044B,0x00BB))

$bakW = 160
$bakX = $m4.Right + 14
$bakY = $repY
$bak = Draw-Box $bakX $bakY $bakW $bh (T @(0x041C,0x043E,0x0434,0x0443,0x043B,0x044C,0x0020,0x00AB,0x0420,0x0435,0x0437,0x0435,0x0440,0x0432,0x043D,0x044B,0x0435,0x0020,0x043A,0x043E,0x043F,0x0438,0x0438,0x00BB))

$dbY = $h - 108
$db = Draw-Box 56 $dbY ($w - 112) 50 (T @(0x0411,0x0430,0x0437,0x0430,0x0020,0x0434,0x0430,0x043D,0x043D,0x044B,0x0445,0x0020,0x0053,0x0051,0x004C,0x0020,0x0053,0x0065,0x0072,0x0076,0x0065,0x0072)) $true

$dbYTop = [float]$db.TopY
$ptDbRep = Pt 255 $dbYTop
$ptDbM3  = Pt 385 $dbYTop
$ptDbBak = Pt ([float]$bak.CX) $dbYTop
$ptDbM4  = Pt 915 $dbYTop

# Login -> Main
$p = @(Pt $login.CX $login.BotY; Pt $main.CX $main.TopY)
Draw-PolylinePts $p
Draw-Arrowhead $login.CX $login.BotY $main.CX $main.TopY

$mb = [float]$main.BotY
Draw-PolylinePts @(Pt ($main.CX - 118) $mb; Pt $m1.CX $m1.TopY); Draw-Arrowhead ($main.CX - 118) $mb $m1.CX $m1.TopY
Draw-PolylinePts @(Pt ($main.CX - 38) $mb; Pt $m2.CX $m2.TopY); Draw-Arrowhead ($main.CX - 38) $mb $m2.CX $m2.TopY
Draw-PolylinePts @(Pt ($main.CX + 38) $mb; Pt $m3.CX $m3.TopY); Draw-Arrowhead ($main.CX + 38) $mb $m3.CX $m3.TopY
Draw-PolylinePts @(Pt ($main.CX + 118) $mb; Pt $m4.CX $m4.TopY); Draw-Arrowhead ($main.CX + 118) $mb $m4.CX $m4.TopY

# Main -> Бэкапы: вправо над модулями, вниз по правому полю
$laneR = [float]($w - 32)
$mainExitX = [float]($main.Right - 6)
$mainExitY = [float]($mb + 1)
$yAboveMods = [float]($yMod - 12)
$bakEntryX = [float]$bak.CX
Draw-PolylinePts @(
    Pt $mainExitX $mainExitY
    Pt $laneR $mainExitY
    Pt $laneR $yAboveMods
    Pt $bakEntryX $yAboveMods
    Pt $bakEntryX $bak.TopY
)
Draw-Arrowhead $bakEntryX $yAboveMods $bakEntryX $bak.TopY

$yConn = [float]($repY - 14)
Draw-PolylinePts @(
    Pt $m1.CX $m1.BotY
    Pt $m1.CX $yConn
    Pt ($rep.Left + 92) $yConn
    Pt ($rep.Left + 92) $rep.TopY
)
Draw-Arrowhead ($rep.Left + 92) $yConn ($rep.Left + 92) $rep.TopY

Draw-PolylinePts @(
    Pt $m2.CX $m2.BotY
    Pt $m2.CX $yConn
    Pt ($rep.Right - 92) $yConn
    Pt ($rep.Right - 92) $rep.TopY
)
Draw-Arrowhead ($rep.Right - 92) $yConn ($rep.Right - 92) $rep.TopY

$yLane = [float]($repY + $bh + 26)

Draw-PolylinePts @(
    Pt $m3.CX $m3.BotY
    Pt $m3.CX $yLane
    Pt $ptDbM3.X $yLane
    $ptDbM3
)
Draw-Arrowhead $ptDbM3.X ($yLane + 40) $ptDbM3.X $dbYTop

Draw-PolylinePts @(
    Pt $m4.CX $m4.BotY
    Pt $m4.CX $yLane
    Pt $ptDbM4.X $yLane
    $ptDbM4
)
Draw-Arrowhead $ptDbM4.X ($yLane + 40) $ptDbM4.X $dbYTop

Draw-PolylinePts @(
    Pt $rep.CX $rep.BotY
    Pt $rep.CX $yLane
    Pt $ptDbRep.X $yLane
    $ptDbRep
)
Draw-Arrowhead $ptDbRep.X ($yLane + 40) $ptDbRep.X $dbYTop

Draw-PolylinePts @(
    Pt $bak.CX $bak.BotY
    Pt $bak.CX $yLane
    $ptDbBak
)
Draw-Arrowhead $bak.CX ($yLane + 40) $bak.CX $dbYTop

$path = 'c:\Users\nsaha\source\repos\BGSK1\risunok_2_1_1_bgsk1.png'
$bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bmp.Dispose()
Write-Output ('Saved ' + $path)
