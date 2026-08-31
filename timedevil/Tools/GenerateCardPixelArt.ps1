param(
    [string]$OutputDir = "Assets/Resources/my_asset/CardArt",
    [int]$BaseSize = 64,
    [int]$OutputSize = 512
)

Set-StrictMode -Version Latest
Add-Type -AssemblyName System.Drawing

function New-HexColor {
    param([string]$Hex, [int]$Alpha = 255)

    $base = [System.Drawing.ColorTranslator]::FromHtml($Hex)
    return [System.Drawing.Color]::FromArgb($Alpha, $base.R, $base.G, $base.B)
}

function Use-Brush {
    param([string]$Hex, [scriptblock]$Body, [int]$Alpha = 255)

    $brush = [System.Drawing.SolidBrush]::new((New-HexColor $Hex $Alpha))
    try { & $Body $brush } finally { $brush.Dispose() }
}

function Use-Pen {
    param([string]$Hex, [float]$Width, [scriptblock]$Body, [int]$Alpha = 255)

    $pen = [System.Drawing.Pen]::new((New-HexColor $Hex $Alpha), $Width)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Square
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Square
    try { & $Body $pen } finally { $pen.Dispose() }
}

function Fill-Rect {
    param($G, [int]$X, [int]$Y, [int]$W, [int]$H, [string]$Hex, [int]$Alpha = 255)
    Use-Brush $Hex { param($b) $G.FillRectangle($b, $X, $Y, $W, $H) } $Alpha
}

function Fill-Ellipse {
    param($G, [int]$X, [int]$Y, [int]$W, [int]$H, [string]$Hex, [int]$Alpha = 255)
    Use-Brush $Hex { param($b) $G.FillEllipse($b, $X, $Y, $W, $H) } $Alpha
}

function Fill-Poly {
    param($G, [System.Drawing.Point[]]$Points, [string]$Hex, [int]$Alpha = 255)
    Use-Brush $Hex { param($b) $G.FillPolygon($b, $Points) } $Alpha
}

function Draw-Line {
    param($G, [int]$X1, [int]$Y1, [int]$X2, [int]$Y2, [string]$Hex, [float]$Width = 2, [int]$Alpha = 255)
    Use-Pen $Hex $Width { param($p) $G.DrawLine($p, $X1, $Y1, $X2, $Y2) } $Alpha
}

function P {
    param([int]$X, [int]$Y)
    return [System.Drawing.Point]::new($X, $Y)
}

function Draw-Spark {
    param($G, [int]$X, [int]$Y, [int]$Size, [string]$Color, [string]$Core = "#fff8b8")

    $outer = [System.Drawing.Point[]]@(
        (P $X ($Y - $Size)),
        (P ($X + $Size) $Y),
        (P $X ($Y + $Size)),
        (P ($X - $Size) $Y)
    )
    Fill-Poly $G $outer "#55203a" 180
    $innerSize = [Math]::Max(1, $Size - 1)
    $inner = [System.Drawing.Point[]]@(
        (P $X ($Y - $innerSize)),
        (P ($X + $innerSize) $Y),
        (P $X ($Y + $innerSize)),
        (P ($X - $innerSize) $Y)
    )
    Fill-Poly $G $inner $Color
    if ($Size -gt 2) { Fill-Rect $G ($X - 1) ($Y - 1) 2 2 $Core }
}

function Draw-Diamond {
    param($G, [int]$X, [int]$Y, [int]$W, [int]$H, [string]$Fill, [string]$Outline = "#201628")

    $o = [System.Drawing.Point[]]@(
        (P $X ($Y - [int]($H / 2))),
        (P ($X + [int]($W / 2)) $Y),
        (P $X ($Y + [int]($H / 2))),
        (P ($X - [int]($W / 2)) $Y)
    )
    Fill-Poly $G $o $Outline

    $i = [System.Drawing.Point[]]@(
        (P $X ($Y - [int](($H - 6) / 2))),
        (P ($X + [int](($W - 6) / 2)) $Y),
        (P $X ($Y + [int](($H - 6) / 2))),
        (P ($X - [int](($W - 6) / 2)) $Y)
    )
    Fill-Poly $G $i $Fill
}

function Draw-Flame {
    param($G, [string]$Outer = "#d82028", [string]$Mid = "#ff7a16", [string]$Core = "#fff238")

    $shadow = [System.Drawing.Point[]]@((P 34 6), (P 45 22), (P 41 39), (P 50 54), (P 31 61), (P 14 53), (P 20 36), (P 18 20))
    Fill-Poly $G $shadow "#501019"
    $red = [System.Drawing.Point[]]@((P 34 8), (P 43 24), (P 39 40), (P 48 53), (P 31 59), (P 16 52), (P 22 36), (P 20 22))
    Fill-Poly $G $red $Outer
    $orange = [System.Drawing.Point[]]@((P 33 17), (P 39 29), (P 34 42), (P 41 51), (P 30 55), (P 22 48), (P 27 36), (P 25 27))
    Fill-Poly $G $orange $Mid
    $yellow = [System.Drawing.Point[]]@((P 32 27), (P 35 36), (P 31 45), (P 35 52), (P 28 52), (P 25 45), (P 29 38), (P 28 31))
    Fill-Poly $G $yellow $Core
    Draw-Spark $G 15 28 3 "#ff6b1a"
    Draw-Spark $G 51 31 2 "#ffb22a"
    Draw-Spark $G 19 14 2 "#ff6b1a"
}

function Draw-Eye {
    param($G, [string]$Iris = "#64b6ff", [string]$Accent = "#925dff")

    $outer = [System.Drawing.Point[]]@((P 9 33), (P 20 22), (P 32 19), (P 45 22), (P 55 33), (P 44 43), (P 31 46), (P 18 43))
    Fill-Poly $G $outer "#271823"
    $white = [System.Drawing.Point[]]@((P 12 33), (P 21 25), (P 32 23), (P 43 25), (P 52 33), (P 42 40), (P 31 43), (P 20 40))
    Fill-Poly $G $white "#e9e0dc"
    Fill-Ellipse $G 23 24 18 18 $Iris
    Fill-Ellipse $G 28 29 8 8 "#1d1620"
    Fill-Rect $G 30 25 3 3 "#ffffff"
    Draw-Line $G 12 22 4 17 $Accent 3 210
    Draw-Line $G 51 22 59 17 $Accent 3 210
    Draw-Line $G 8 45 3 49 $Accent 3 170
    Draw-Line $G 56 45 61 49 $Accent 3 170
}

function Draw-Heart {
    param($G, [string]$Fill = "#e84a65", [string]$Outline = "#31141e")

    Fill-Ellipse $G 15 16 18 18 $Outline
    Fill-Ellipse $G 31 16 18 18 $Outline
    $o = [System.Drawing.Point[]]@((P 13 25), (P 51 25), (P 32 56))
    Fill-Poly $G $o $Outline
    Fill-Ellipse $G 18 18 14 14 $Fill
    Fill-Ellipse $G 32 18 14 14 $Fill
    $i = [System.Drawing.Point[]]@((P 17 27), (P 47 27), (P 32 52))
    Fill-Poly $G $i $Fill
}

function Draw-CrackedHeart {
    param($G)

    Draw-Heart $G "#d9435f"
    Draw-Line $G 33 17 28 29 "#fff0c4" 2
    Draw-Line $G 28 29 35 35 "#fff0c4" 2
    Draw-Line $G 35 35 30 51 "#fff0c4" 2
    Draw-Line $G 33 17 28 29 "#4b1424" 1
    Draw-Line $G 28 29 35 35 "#4b1424" 1
    Draw-Line $G 35 35 30 51 "#4b1424" 1
}

function Draw-Shield {
    param($G, [string]$Fill = "#59c9a5", [string]$Accent = "#fff2a6")

    $o = [System.Drawing.Point[]]@((P 32 7), (P 51 15), (P 47 38), (P 32 57), (P 17 38), (P 13 15))
    Fill-Poly $G $o "#1f2430"
    $i = [System.Drawing.Point[]]@((P 32 11), (P 47 17), (P 43 36), (P 32 52), (P 21 36), (P 17 17))
    Fill-Poly $G $i $Fill
    Draw-Line $G 32 14 32 49 $Accent 2 210
}

function Draw-Arrow {
    param($G, [string]$Fill = "#ffcf4a", [string]$Direction = "up")

    if ($Direction -eq "left") {
        $o = [System.Drawing.Point[]]@((P 8 32), (P 26 15), (P 26 25), (P 54 25), (P 54 39), (P 26 39), (P 26 49))
        Fill-Poly $G $o "#261926"
        $i = [System.Drawing.Point[]]@((P 13 32), (P 28 19), (P 28 28), (P 50 28), (P 50 36), (P 28 36), (P 28 45))
        Fill-Poly $G $i $Fill
    } elseif ($Direction -eq "down") {
        $o = [System.Drawing.Point[]]@((P 32 56), (P 15 38), (P 25 38), (P 25 10), (P 39 10), (P 39 38), (P 49 38))
        Fill-Poly $G $o "#261926"
        $i = [System.Drawing.Point[]]@((P 32 51), (P 19 36), (P 28 36), (P 28 14), (P 36 14), (P 36 36), (P 45 36))
        Fill-Poly $G $i $Fill
    } else {
        $o = [System.Drawing.Point[]]@((P 32 8), (P 49 26), (P 39 26), (P 39 54), (P 25 54), (P 25 26), (P 15 26))
        Fill-Poly $G $o "#261926"
        $i = [System.Drawing.Point[]]@((P 32 13), (P 45 28), (P 36 28), (P 36 50), (P 28 50), (P 28 28), (P 19 28))
        Fill-Poly $G $i $Fill
    }
}

function Draw-Spiral {
    param($G, [string]$Fill = "#a56bff", [string]$Accent = "#ffc4ff")

    Draw-Line $G 49 32 49 21 "#22162e" 5
    Draw-Line $G 49 21 36 13 "#22162e" 5
    Draw-Line $G 36 13 20 19 "#22162e" 5
    Draw-Line $G 20 19 15 34 "#22162e" 5
    Draw-Line $G 15 34 24 48 "#22162e" 5
    Draw-Line $G 24 48 40 48 "#22162e" 5
    Draw-Line $G 40 48 46 36 "#22162e" 5
    Draw-Line $G 46 36 35 27 "#22162e" 5
    Draw-Line $G 35 27 27 33 "#22162e" 5
    Draw-Line $G 49 32 49 21 $Fill 3
    Draw-Line $G 49 21 36 13 $Fill 3
    Draw-Line $G 36 13 20 19 $Fill 3
    Draw-Line $G 20 19 15 34 $Fill 3
    Draw-Line $G 15 34 24 48 $Fill 3
    Draw-Line $G 24 48 40 48 $Fill 3
    Draw-Line $G 40 48 46 36 $Fill 3
    Draw-Line $G 46 36 35 27 $Fill 3
    Draw-Line $G 35 27 27 33 $Fill 3
    Fill-Rect $G 28 31 5 5 $Accent
}

function Draw-CloudRain {
    param($G, [string]$Cloud = "#6b748f", [string]$Rain = "#4aa6ff")

    Fill-Ellipse $G 11 24 20 17 "#202433"
    Fill-Ellipse $G 25 17 25 24 "#202433"
    Fill-Ellipse $G 39 25 14 15 "#202433"
    Fill-Rect $G 16 33 34 10 "#202433"
    Fill-Ellipse $G 14 25 16 13 $Cloud
    Fill-Ellipse $G 27 20 20 19 $Cloud
    Fill-Ellipse $G 40 28 10 10 $Cloud
    Fill-Rect $G 18 33 29 7 $Cloud
    Draw-Line $G 22 45 18 53 $Rain 3
    Draw-Line $G 33 43 29 55 $Rain 3
    Draw-Line $G 44 45 40 53 $Rain 3
}

function Draw-Mask {
    param($G, [string]$Fill = "#d7d1cd", [string]$Accent = "#744bff")

    Fill-Ellipse $G 14 12 36 42 "#261926"
    Fill-Ellipse $G 17 14 30 36 $Fill
    Fill-Rect $G 22 29 7 4 "#211721"
    Fill-Rect $G 36 29 7 4 "#211721"
    Draw-Line $G 25 44 39 40 $Accent 3
}

function Draw-Chain {
    param($G, [string]$Fill = "#b7c2d9", [string]$Accent = "#ff6cc7")

    Draw-Line $G 21 25 43 39 "#201725" 12
    Draw-Line $G 21 25 43 39 $Fill 8
    Draw-Line $G 17 32 32 17 "#201725" 12
    Draw-Line $G 17 32 32 17 $Fill 8
    Fill-Rect $G 29 28 6 6 $Accent
}

function Draw-Question {
    param($G)

    Fill-Rect $G 24 11 18 6 "#24172c"
    Fill-Rect $G 42 17 6 12 "#24172c"
    Fill-Rect $G 36 29 6 6 "#24172c"
    Fill-Rect $G 30 35 6 7 "#24172c"
    Fill-Rect $G 30 50 7 7 "#24172c"
    Fill-Rect $G 26 13 14 4 "#f2e07a"
    Fill-Rect $G 40 18 4 9 "#f2e07a"
    Fill-Rect $G 34 29 6 4 "#f2e07a"
    Fill-Rect $G 31 35 4 6 "#f2e07a"
    Fill-Rect $G 31 51 5 5 "#f2e07a"
    Draw-Spark $G 18 20 2 "#a56bff"
    Draw-Spark $G 47 46 2 "#6bc7ff"
}

function Draw-CardDrop {
    param($G, [string]$Fill = "#d9c7a4", [string]$Accent = "#4fd1ff")

    $o = [System.Drawing.Point[]]@((P 22 11), (P 46 18), (P 39 52), (P 15 45))
    Fill-Poly $G $o "#261926"
    $i = [System.Drawing.Point[]]@((P 24 15), (P 42 20), (P 36 48), (P 18 43))
    Fill-Poly $G $i $Fill
    Draw-Diamond $G 31 31 11 13 $Accent
    Draw-Line $G 43 14 52 9 "#ff6b7a" 3
    Draw-Line $G 44 51 54 56 "#ff6b7a" 3
}

function Draw-Laurel {
    param($G)

    Draw-Diamond $G 32 23 18 22 "#ffd74f"
    Draw-Line $G 17 39 28 53 "#30c878" 4
    Draw-Line $G 47 39 36 53 "#30c878" 4
    for ($i = 0; $i -lt 4; $i++) {
        Fill-Rect $G (15 + $i * 3) (34 + $i * 4) 7 4 "#73e38b"
        Fill-Rect $G (42 - $i * 3) (34 + $i * 4) 7 4 "#73e38b"
    }
    Draw-Spark $G 32 22 4 "#fff1a6"
}

function Render-Icon {
    param($G, [string]$Id)

    switch ($Id) {
        "unrest" { Draw-Eye $G "#8f77ff" "#ff675f"; Draw-Line $G 20 11 15 5 "#ff675f" 2; Draw-Line $G 44 11 49 5 "#ff675f" 2 }
        "nervous" { Draw-Line $G 14 11 28 30 "#251527" 7; Draw-Line $G 28 30 18 30 "#251527" 7; Draw-Line $G 18 30 36 55 "#251527" 7; Draw-Line $G 14 11 28 30 "#ffd44a" 4; Draw-Line $G 28 30 18 30 "#ffd44a" 4; Draw-Line $G 18 30 36 55 "#ffd44a" 4; Draw-Spark $G 47 22 3 "#ff657a"; Draw-Spark $G 48 45 2 "#7fc8ff" }
        "scary" { Draw-Eye $G "#c768ff" "#ff334e"; Draw-Line $G 15 52 21 42 "#f0efe4" 4; Draw-Line $G 31 55 32 43 "#f0efe4" 4; Draw-Line $G 48 52 43 42 "#f0efe4" 4 }
        "guilt" { Draw-CrackedHeart $G; Draw-Line $G 15 13 8 22 "#6f2740" 3; Draw-Line $G 48 13 56 22 "#6f2740" 3 }
        "jealousy" { Draw-Eye $G "#35d47a" "#7fe05c"; Draw-Spiral $G "#54d86a" "#eaff9d" }
        "obsession" { Draw-Spiral $G "#bd60ff" "#ffd2ff"; Draw-Chain $G "#c8b7ff" "#ff6ac8" }
        "cynicism" { Draw-Mask $G "#cbc1b7" "#7d6170"; Draw-Line $G 20 20 45 48 "#2d1a26" 3 }
        "break" { Draw-Chain $G "#a8a8a8" "#ff404c"; Draw-Line $G 20 44 44 20 "#ff404c" 4; Draw-Line $G 24 50 50 24 "#fff0a8" 2 }
        "calm" { Fill-Ellipse $G 14 20 36 28 "#1b3040"; Draw-Line $G 15 35 27 31 "#70dbff" 3; Draw-Line $G 27 31 39 35 "#70dbff" 3; Draw-Line $G 39 35 51 31 "#70dbff" 3; Draw-Line $G 18 43 30 39 "#b9f3ff" 2; Draw-Line $G 30 39 43 43 "#b9f3ff" 2; Draw-Spark $G 33 18 2 "#ffffff" }
        "confidence" { Draw-Shield $G "#4fd0ff" "#fff0a6"; Draw-Spark $G 32 27 7 "#ffd64d"; Draw-Spark $G 47 16 2 "#ffffff" }
        "trust" { Draw-Chain $G "#92e0ff" "#fff1a6"; Draw-Heart $G "#62d9a4" }
        "will" { Draw-Arrow $G "#ffda57" "up"; Draw-Line $G 22 53 42 13 "#fff6b0" 3; Draw-Spark $G 42 14 3 "#ffffff" }
        "victory" { Draw-Laurel $G }
        "infit" { Draw-Shield $G "#8df5d2" "#ffffff"; Draw-Diamond $G 32 31 22 28 "#87d7ff"; Draw-Spark $G 32 31 3 "#ffffff" }

        "brave" { Draw-Arrow $G "#ffd34a" "up"; Draw-Flame $G "#ff4b2e" "#ff931f" "#fff05a" }
        "fear" { Draw-Arrow $G "#7f8aa3" "left"; Draw-Eye $G "#a98aff" "#2b2740" }
        "hope" { Fill-Ellipse $G 13 36 38 18 "#253544"; Draw-Spark $G 32 24 10 "#ffd84e"; Draw-Line $G 15 45 49 45 "#ffb347" 3; Draw-Spark $G 47 18 2 "#b8f7ff" }
        "passion" { Draw-Heart $G "#ff4b4b"; Draw-Flame $G "#f02e2e" "#ff8521" "#fff04d" }
        "regret" { Draw-Arrow $G "#78a0d8" "left"; Draw-Line $G 20 18 45 48 "#4d3350" 4; Draw-Spark $G 17 47 2 "#9bc4ff" }
        "sympathy" { Draw-Heart $G "#72d6c3"; Fill-Ellipse $G 12 27 16 23 "#77bfff"; Fill-Ellipse $G 39 27 16 23 "#77bfff"; Draw-Line $G 20 51 32 57 "#b5fff0" 3; Draw-Line $G 44 51 32 57 "#b5fff0" 3 }
        "depression" { Draw-CloudRain $G "#667087" "#5aa0ff"; Draw-Arrow $G "#53627c" "down" }
        "hatred" { Draw-Heart $G "#cf2638"; Draw-Line $G 10 53 54 11 "#2a1118" 6; Draw-Line $G 13 50 51 14 "#ff3449" 3; Draw-Spark $G 49 48 3 "#ff992b" }
        "fascination" { Draw-Spiral $G "#d85cff" "#ffd4ff"; Draw-Eye $G "#ff7adf" "#a86cff" }

        "attachment" { Draw-Chain $G "#ffd1e8" "#ff6ab7"; Draw-Heart $G "#ff8fbd" }
        "contempt" { Draw-Mask $G "#b4b4b4" "#b33b55"; Draw-Line $G 18 42 46 36 "#4a2732" 3; Draw-Line $G 16 21 28 27 "#4a2732" 3 }
        "excitement" { Draw-Spark $G 32 31 14 "#ffcf3f"; Draw-Spark $G 17 18 4 "#ff6bd3"; Draw-Spark $G 50 20 3 "#6ff0ff"; Draw-Spark $G 48 48 4 "#ff8c3a"; Draw-Spark $G 15 46 3 "#ffffff" }
        "humiliation" { Draw-Mask $G "#d3b5b5" "#735070"; Draw-Line $G 22 48 42 48 "#5a293d" 3; Draw-Line $G 32 45 32 56 "#8a5bff" 3 }
        "longing" { Fill-Ellipse $G 13 12 35 35 "#f4d67a"; Fill-Ellipse $G 24 9 35 36 "#000000" 255; Draw-Spark $G 49 18 4 "#bfe9ff"; Draw-Line $G 18 48 46 48 "#7a5ba8" 3 }
        "question" { Draw-Question $G }
        "discard" { Draw-CardDrop $G "#c4af8b" "#ff6a6a"; Draw-Arrow $G "#ff6a6a" "down" }
        "reassurance" { Draw-Shield $G "#8fe3c1" "#fff7b2"; Draw-Heart $G "#ffc6a5"; Draw-Spark $G 48 16 2 "#ffffff" }

        "warm" { Fill-Ellipse $G 13 36 38 16 "#4a2c24"; Draw-Flame $G "#ff6328" "#ffab30" "#fff7a3"; Draw-Line $G 18 52 46 52 "#7b4328" 4 }
        "relief" { Draw-Shield $G "#74d4ff" "#fff9bc"; Draw-Line $G 16 36 28 48 "#fff9bc" 4; Draw-Line $G 28 48 50 20 "#fff9bc" 4 }
        "panic" { Draw-Eye $G "#ff667a" "#ffd34a"; Draw-Spark $G 32 10 4 "#ff334e"; Draw-Spark $G 12 34 3 "#ff334e"; Draw-Spark $G 53 34 3 "#ff334e"; Draw-Spark $G 32 56 3 "#ff334e" }
        "despair" { Fill-Ellipse $G 10 10 44 44 "#1a1420"; Fill-Ellipse $G 18 18 28 28 "#33213a"; Draw-Line $G 17 45 47 19 "#6e2e78" 4; Draw-Line $G 25 13 30 28 "#6e2e78" 3; Fill-Rect $G 29 45 7 7 "#0e0a12" }
        "celebration" { Draw-Spark $G 32 28 11 "#ffd64a"; Draw-Spark $G 14 18 3 "#ff6ec7"; Draw-Spark $G 50 17 3 "#7cefff"; Fill-Rect $G 18 48 6 3 "#ff6ec7"; Fill-Rect $G 40 48 7 3 "#7cefff"; Fill-Rect $G 30 50 5 4 "#8fff8f" }
        "content" { Fill-Rect $G 18 20 29 25 "#2a1c24"; Fill-Rect $G 21 23 23 18 "#e3b36c"; Fill-Rect $G 24 26 17 8 "#fff0a6"; Draw-Line $G 19 47 46 47 "#5b3826" 3; Draw-Line $G 25 35 39 35 "#6f4328" 2; Draw-Line $G 48 26 55 33 "#e3b36c" 4 }
        default { Draw-Diamond $G 32 32 34 42 "#b0d8ff"; Draw-Spark $G 32 32 4 "#ffffff" }
    }
}

function New-GuidN {
    return [Guid]::NewGuid().ToString("N")
}

function Ensure-FolderMeta {
    param([string]$Path)

    $metaPath = "$Path.meta"
    if (Test-Path -LiteralPath $metaPath) { return }

    $guid = New-GuidN
    $content = @"
fileFormatVersion: 2
guid: $guid
folderAsset: yes
DefaultImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    [System.IO.File]::WriteAllText($metaPath, $content, [System.Text.Encoding]::UTF8)
}

function Get-ExistingGuid {
    param([string]$MetaPath)

    if (!(Test-Path -LiteralPath $MetaPath)) { return $null }

    $match = [regex]::Match((Get-Content -LiteralPath $MetaPath -Raw), '(?m)^guid:\s*([0-9a-fA-F]+)')
    if ($match.Success) { return $match.Groups[1].Value }
    return $null
}

function Write-TextureMeta {
    param([string]$ImagePath)

    $metaPath = "$ImagePath.meta"
    $guid = Get-ExistingGuid $metaPath
    if ([string]::IsNullOrWhiteSpace($guid)) { $guid = New-GuidN }
    $spriteId = New-GuidN

    $content = @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 1
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 512
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Standalone
    maxTextureSize: 512
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: $spriteId
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    [System.IO.File]::WriteAllText($metaPath, $content, [System.Text.Encoding]::UTF8)
}

$ids = @(
    "attachment", "brave", "break", "calm", "celebration", "confidence",
    "contempt", "content", "cynicism", "depression", "despair", "discard",
    "excitement", "fascination", "fear", "guilt", "hatred", "hope",
    "humiliation", "infit", "jealousy", "longing", "nervous", "obsession",
    "panic", "passion", "question", "reassurance", "regret", "relief",
    "scary", "sympathy", "trust", "unrest", "victory", "warm", "will"
)

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
Ensure-FolderMeta $OutputDir

foreach ($id in $ids) {
    $base = [System.Drawing.Bitmap]::new($BaseSize, $BaseSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($base)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighSpeed

    try {
        Render-Icon $g $id
    } finally {
        $g.Dispose()
    }

    $scaled = [System.Drawing.Bitmap]::new($OutputSize, $OutputSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $sg = [System.Drawing.Graphics]::FromImage($scaled)
    $sg.Clear([System.Drawing.Color]::Transparent)
    $sg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::None
    $sg.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
    $sg.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half

    try {
        $sg.DrawImage($base, 0, 0, $OutputSize, $OutputSize)
    } finally {
        $sg.Dispose()
        $base.Dispose()
    }

    $path = Join-Path $OutputDir "$id.png"
    $scaled.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $scaled.Dispose()
    Write-TextureMeta $path
}

Write-Output "Generated $($ids.Count) card artwork sprites in $OutputDir"
