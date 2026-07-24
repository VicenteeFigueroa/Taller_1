Write-Host "Iniciando servidor temporalmente para capturar logs..."
Stop-Process -Name dotnet -Force -ErrorAction SilentlyContinue

$logFile = "tracing-logs.txt"
if (Test-Path $logFile) { Remove-Item $logFile }

# Iniciar servidor y redirigir stdout al archivo
$process = Start-Process dotnet -ArgumentList "run" -RedirectStandardOutput $logFile -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 10 # Esperar a que levante

$uri = "http://localhost:5064/missing-link"
Write-Host "`nHaciendo petición de prueba a: $uri"

# Hacemos la peticion ignorando el error 404 (el redirect endpoint devolverá 404 porque no existe en BD)
$reqId = $null
try {
    $res = Invoke-WebRequest -Uri $uri -UseBasicParsing
    $reqId = $res.Headers["X-Request-Id"]
} catch {
    if ($_.Exception.Response) {
        # Si da un 4xx o 5xx, el header igual debería estar ahí
        $headers = $_.Exception.Response.Headers
        if ($headers.Contains("X-Request-Id")) {
            $reqId = $headers.GetValues("X-Request-Id")[0]
        }
    }
}

Write-Host "Header HTTP recibido [X-Request-Id]: $reqId"

# Detener el servidor
Stop-Process -Id $process.Id -Force
Start-Sleep -Seconds 2

# Buscar el ReqId en el archivo de log
Write-Host "`nBuscando el ReqId en los logs de Serilog..."
$lines = Select-String -Path $logFile -Pattern "ReqId=$reqId"

if ($lines) {
    Write-Host "¡Éxito! El RequestId fue propagado a los logs del servidor:"
    foreach ($line in $lines) {
        Write-Host "-> $($line.Line)"
    }
} else {
    Write-Host "No se encontraron logs con ese ReqId."
}
