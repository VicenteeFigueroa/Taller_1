Write-Host "Iniciando servidor para validar Rate Limiting..."
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue

$process = Start-Process dotnet -ArgumentList "run" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 7 # Esperar a que levante

$uri = "http://localhost:5064/Login"
$payload = "Email=admin%40shortly.disc.cl&Password=admin123"

Write-Host "`n--- ENVIANDO 10 PETICIONES RÁPIDAS (Límite es 5 por minuto) ---"
for ($i = 1; $i -le 10; $i++) {
    try {
        $res = Invoke-WebRequest -Uri $uri -Method Post -Body $payload -ContentType "application/x-www-form-urlencoded" -UseBasicParsing -ErrorAction Stop
        Write-Host "Petición $i -> Status Code: $($res.StatusCode)"
    } catch {
        if ($_.Exception.Response) {
            $status = $_.Exception.Response.StatusCode
            Write-Host "Petición $i -> Status Code: $($status.value__)"
            if ($status.value__ -eq 429) {
                Write-Host ">>> RATE LIMIT ALCANZADO (429 Too Many Requests)" -ForegroundColor Yellow
                $retryAfter = $_.Exception.Response.Headers["Retry-After"]
                if ($retryAfter) {
                    Write-Host ">>> Header Retry-After: $retryAfter" -ForegroundColor Yellow
                }
            }
        } else {
            Write-Host "Petición $i -> Error: $_"
        }
    }
}

# Detener el servidor
Stop-Process -Id $process.Id -Force
Write-Host "`nPrueba finalizada exitosamente."
