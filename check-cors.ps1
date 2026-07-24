Add-Type -AssemblyName System.Net.Http

$uri = "http://localhost:5064/"

Write-Host "Verificando reglas CORS en: $uri`n"

$client = New-Object System.Net.Http.HttpClient

# 1. Origen Permitido (https://trusted.shortly.com)
$reqAllowed = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Options, $uri)
$reqAllowed.Headers.Add("Origin", "https://trusted.shortly.com")
$reqAllowed.Headers.Add("Access-Control-Request-Method", "POST")
$reqAllowed.Headers.Add("Access-Control-Request-Headers", "Content-Type")

$resAllowed = $client.SendAsync($reqAllowed).Result

Write-Host "--- TEST ORIGEN PERMITIDO ---"
Write-Host "Origen: https://trusted.shortly.com"
Write-Host "Status: $($resAllowed.StatusCode)"
if ($resAllowed.Headers.Contains("Access-Control-Allow-Origin")) {
    Write-Host "Access-Control-Allow-Origin: $($resAllowed.Headers.GetValues('Access-Control-Allow-Origin') -join ',')"
}
if ($resAllowed.Headers.Contains("Access-Control-Allow-Methods")) {
    Write-Host "Access-Control-Allow-Methods: $($resAllowed.Headers.GetValues('Access-Control-Allow-Methods') -join ',')"
}
if ($resAllowed.Headers.Contains("Access-Control-Allow-Headers")) {
    Write-Host "Access-Control-Allow-Headers: $($resAllowed.Headers.GetValues('Access-Control-Allow-Headers') -join ',')"
}

# 2. Origen Denegado (https://evil.hacker.com)
$reqDenied = New-Object System.Net.Http.HttpRequestMessage([System.Net.Http.HttpMethod]::Options, $uri)
$reqDenied.Headers.Add("Origin", "https://evil.hacker.com")
$reqDenied.Headers.Add("Access-Control-Request-Method", "POST")
$reqDenied.Headers.Add("Access-Control-Request-Headers", "Content-Type")

$resDenied = $client.SendAsync($reqDenied).Result

Write-Host "`n--- TEST ORIGEN DENEGADO ---"
Write-Host "Origen: https://evil.hacker.com"
Write-Host "Status: $($resDenied.StatusCode)"
if ($resDenied.Headers.Contains("Access-Control-Allow-Origin")) {
    Write-Host "Access-Control-Allow-Origin: $($resDenied.Headers.GetValues('Access-Control-Allow-Origin') -join ',')"
} else {
    Write-Host "Access-Control-Allow-Origin: (No presente, CORS bloqueado por el navegador)"
}
