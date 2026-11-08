# Serves the project folder on http://localhost:<Port>/ so the hub (Hub/) and the web Sudoku (Web/)
# share one address, and therefore one login. Open http://localhost:8780/Hub/
param([int]$Port = 8780, [string]$Root = (Split-Path $PSScriptRoot))
$types = @{ ".html" = "text/html; charset=utf-8"; ".js" = "text/javascript; charset=utf-8"; ".css" = "text/css; charset=utf-8";
            ".jpg" = "image/jpeg"; ".png" = "image/png"; ".svg" = "image/svg+xml"; ".json" = "application/json"; ".ico" = "image/x-icon" }
$listener = New-Object Net.HttpListener
$listener.Prefixes.Add("http://localhost:$Port/")
$listener.Start()
"Mind Studio: http://localhost:$Port/Hub/"
while ($listener.IsListening) {
    $ctx = $listener.GetContext()
    $path = [Uri]::UnescapeDataString($ctx.Request.Url.AbsolutePath).TrimStart("/")
    if ($path -eq "") { $ctx.Response.Redirect("/Hub/"); $ctx.Response.Close(); continue }
    $file = [IO.Path]::GetFullPath((Join-Path $Root $path))
    if ((Test-Path $file -PathType Container)) { $file = Join-Path $file "index.html" }
    if ($file.StartsWith([IO.Path]::GetFullPath($Root)) -and (Test-Path $file -PathType Leaf)) {
        $bytes = [IO.File]::ReadAllBytes($file)
        $ext = [IO.Path]::GetExtension($file).ToLower()
        $ctx.Response.ContentType = if ($types[$ext]) { $types[$ext] } else { "application/octet-stream" }
        $ctx.Response.Headers.Add("Cache-Control", "no-store")
        $ctx.Response.OutputStream.Write($bytes, 0, $bytes.Length)
    } else { $ctx.Response.StatusCode = 404 }
    $ctx.Response.Close()
}
