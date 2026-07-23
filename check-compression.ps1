Add-Type -AssemblyName System.Net.Http
$uri = "http://localhost:5064/"

Write-Host "Verificando compresin en: $uri`n"

$handler = New-Object System.Net.Http.HttpClientHandler
$handler.AutomaticDecompression = [System.Net.DecompressionMethods]::None
$client = New-Object System.Net.Http.HttpClient($handler)

try {
    # 1. Sin compresin (Identity)
    $reqNone = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Get, $uri)
    $resNone = $client.SendAsync($reqNone).Result
    $bytesNone = $resNone.Content.ReadAsByteArrayAsync().Result.Length
    Write-Host "[Identity] Tamao sin comprimir: $bytesNone bytes"

    # 2. Brotli (br)
    $reqBr = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Get, $uri)
    $reqBr.Headers.AcceptEncoding.TryParseAdd("br") | Out-Null
    $resBr = $client.SendAsync($reqBr).Result
    $bytesBr = $resBr.Content.ReadAsByteArrayAsync().Result.Length
    $encBr = [string]::Join(",", $resBr.Content.Headers.ContentEncoding)
    $savedBr = $bytesNone - $bytesBr
    $pctBr = [math]::Round(($savedBr / $bytesNone) * 100, 2)
    Write-Host "`n[Brotli] Content-Encoding recibido: $encBr"
    Write-Host "[Brotli] Tamao comprimido: $bytesBr bytes"
    Write-Host "[Brotli] Ahorro: $savedBr bytes ($pctBr%)"

    # 3. Gzip
    $reqGzip = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Get, $uri)
    $reqGzip.Headers.AcceptEncoding.TryParseAdd("gzip") | Out-Null
    $resGzip = $client.SendAsync($reqGzip).Result
    $bytesGzip = $resGzip.Content.ReadAsByteArrayAsync().Result.Length
    $encGzip = [string]::Join(",", $resGzip.Content.Headers.ContentEncoding)
    $savedGzip = $bytesNone - $bytesGzip
    $pctGzip = [math]::Round(($savedGzip / $bytesNone) * 100, 2)
    Write-Host "`n[Gzip] Content-Encoding recibido: $encGzip"
    Write-Host "[Gzip] Tamao comprimido: $bytesGzip bytes"
    Write-Host "[Gzip] Ahorro: $savedGzip bytes ($pctGzip%)"

    Write-Host "`nPrueba completada exitosamente."
} catch {
    Write-Host "Error: $_"
}
