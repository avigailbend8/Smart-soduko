# Builds SmartSudoku.html: the web version as one file, with the pictures embedded,
# so it can be sent to a friend and opened in any browser without internet.
$web = $PSScriptRoot
$page = [IO.File]::ReadAllText((Join-Path $web "index.html"))

$images = Get-ChildItem (Join-Path $web "img") -Filter *.jpg | ForEach-Object {
    '"{0}":"data:image/jpeg;base64,{1}"' -f $_.Name, [Convert]::ToBase64String([IO.File]::ReadAllBytes($_.FullName))
}
$embedded = "<script>window.EMBEDDED_IMAGES={" + ($images -join ",") + "};</script>"

$html = "<!doctype html><html lang=""he""><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1,viewport-fit=cover"">" +
    "<style>html{color-scheme:light}body{margin:0;font:14px system-ui,sans-serif}img{max-width:100%}[hidden]{display:none!important}</style></head><body>" +
    $embedded + $page + "</body></html>"

$out = Join-Path $web "SmartSudoku.html"
[IO.File]::WriteAllText($out, $html, (New-Object Text.UTF8Encoding($false)))
"Built $out ($([math]::Round((Get-Item $out).Length / 1KB)) KB)"

# The same file is the public website (GitHub Pages serves the docs folder)
$docs = Join-Path (Split-Path $web) "docs"
New-Item -ItemType Directory -Force $docs | Out-Null
[IO.File]::WriteAllText((Join-Path $docs "index.html"), $html, (New-Object Text.UTF8Encoding($false)))
"Copied to docs\index.html for GitHub Pages"
