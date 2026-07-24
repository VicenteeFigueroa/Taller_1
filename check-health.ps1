Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue
Write-Host "Iniciando servidor para validar endpoint de Health Check..."
$process = Start-Process dotnet -ArgumentList "run" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 7 # Esperar a que levante

$uri = "http://localhost:5064/health"

Write-Host "`n--- PRIMERA PETICIÓN ---"
try {
    $res = Invoke-WebRequest -Uri $uri -UseBasicParsing
    Write-Host "Status Code: $($res.StatusCode)"
    Write-Host "Payload:"
    Write-Host $res.Content
} catch {
    Write-Host "Error: $_"
}

Write-Host "`nEsperando 3 segundos..."
Start-Sleep -Seconds 3

Write-Host "`n--- SEGUNDA PETICIÓN (Para verificar Uptime) ---"
try {
    $res = Invoke-WebRequest -Uri $uri -UseBasicParsing
    Write-Host "Status Code: $($res.StatusCode)"
    Write-Host "Payload:"
    Write-Host $res.Content
} catch {
    Write-Host "Error: $_"
}

# Detener el servidor
Stop-Process -Id $process.Id -Force
Write-Host "`nPrueba finalizada exitosamente."
