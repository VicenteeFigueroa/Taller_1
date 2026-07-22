$ErrorActionPreference = 'Stop'

$ab = "C:\Users\vjavi\AppData\Local\Microsoft\WinGet\Packages\ApacheLounge.httpd_Microsoft.Winget.Source_8wekyb3d8bbwe\Apache24\bin\ab.exe"
$loginPayload = "Email=admin%40shortly.disc.cl&Password=admin123"
Set-Content -Path login.txt -Value $loginPayload -Encoding Ascii

$date = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
$os = (Get-CimInstance Win32_OperatingSystem).Caption
$cpu = (Get-CimInstance Win32_Processor).Name
$ram = [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)

Write-Host "Starting application in Release mode..."
$process = Start-Process dotnet -ArgumentList "run", "-c", "Release" -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 5

Write-Host "Warming up endpoints..."
Try {
    Invoke-WebRequest -Uri "http://localhost:5064/aspnet" -Method Get -MaximumRedirection 0 -ErrorAction SilentlyContinue | Out-Null
    Invoke-WebRequest -Uri "http://localhost:5064/Login" -Method Post -Body $loginPayload -ContentType "application/x-www-form-urlencoded" -ErrorAction SilentlyContinue | Out-Null
    Invoke-WebRequest -Uri "http://localhost:5064/" -Method Get -ErrorAction SilentlyContinue | Out-Null
} Catch {}

Write-Host "Running Benchmarks..."

$out1 = & $ab -n 1000 -c 10 "http://localhost:5064/aspnet"
$out2 = & $ab -n 1000 -c 10 -p login.txt -T "application/x-www-form-urlencoded" "http://localhost:5064/Login"
$out3 = & $ab -n 1000 -c 10 "http://localhost:5064/"

Write-Host "Stopping application..."
Stop-Process -Id $process.Id -Force

function Parse-AbOutput ($output) {
    $reqSec = ($output | Select-String "Requests per second:\s+([\d\.]+)")
    $reqSecVal = if ($reqSec) { $reqSec.Matches.Groups[1].Value } else { "N/A" }
    
    $p50 = ($output | Select-String "\s+50%\s+(\d+)")
    $p50Val = if ($p50) { $p50.Matches.Groups[1].Value } else { "N/A" }

    $p90 = ($output | Select-String "\s+90%\s+(\d+)")
    $p90Val = if ($p90) { $p90.Matches.Groups[1].Value } else { "N/A" }

    $p99 = ($output | Select-String "\s+99%\s+(\d+)")
    $p99Val = if ($p99) { $p99.Matches.Groups[1].Value } else { "N/A" }

    $failed = ($output | Select-String "Failed requests:\s+(\d+)")
    $failedVal = if ($failed) { $failed.Matches.Groups[1].Value } else { "0" }

    $non2xx = ($output | Select-String "Non-2xx responses:\s+(\d+)")
    $non2xxVal = if ($non2xx) { $non2xx.Matches.Groups[1].Value } else { "0" }

    $failStr = if ([int]$non2xxVal -gt 0) { "$failedVal ($non2xxVal Non-2xx)" } else { $failedVal }

    return @{ ReqSec = $reqSecVal; P50 = $p50Val; P90 = $p90Val; P99 = $p99Val; Failures = $failStr }
}

$stat1 = Parse-AbOutput $out1
$stat2 = Parse-AbOutput $out2
$stat3 = Parse-AbOutput $out3

$md = @"
# Benchmark Before Changes

## Entorno de Pruebas
- **Fecha y Hora:** $date
- **Sistema Operativo:** $os
- **Hardware:** CPU: $cpu | RAM: ${ram} GB
- **Comando base:** ``ab -n 1000 -c 10 [url]``
- **Modo del Servidor:** ASP.NET Core Kestrel en modo **Release** (con JIT warmup previo).

## Resumen Ejecutivo

| Endpoint | Método | Req/seg | p50 (ms) | p90 (ms) | p99 (ms) | Fallos / Non-2xx |
|----------|--------|---------|----------|----------|----------|------------------|
| `/{shortUrl}` | GET | $($stat1.ReqSec) | $($stat1.P50) | $($stat1.P90) | $($stat1.P99) | $($stat1.Failures) |
| `/Login` | POST | $($stat2.ReqSec) | $($stat2.P50) | $($stat2.P90) | $($stat2.P99) | $($stat2.Failures) |
| `/` | GET | $($stat3.ReqSec) | $($stat3.P50) | $($stat3.P90) | $($stat3.P99) | $($stat3.Failures) |

---

## Salida Detallada de Apache Bench (ab)

### 1. GET /{shortUrl} (Redirección a URL original)
Este endpoint será optimizado mediante **Response Caching (Item #2)** y **Conditional Redirects (Item #10)**.
*Nota: Retorna \`302 Found\`, por lo que \`ab\` lo marca como "Non-2xx responses". Esto es esperado.*

\`\`\`text
$($out1 -join "`n")
\`\`\`

### 2. POST /Login (Validación de Autenticación)
Este endpoint será protegido mediante **Rate Limiting (Item #5)**.
*Nota: Al enviar la petición mediante \`ab\` sin el token AntiForgery, retorna \`400 Bad Request\`, lo cual es captado como \`Non-2xx response\`. El rate limiting será aplicado antes de esta validación.*

\`\`\`text
$($out2 -join "`n")
\`\`\`

### 3. GET / (Página de inicio y creación de URLs)
Este endpoint se beneficiará de la **Compresión Brotli/Gzip (Item #6)** y **Security Headers (Item #3)**.

\`\`\`text
$($out3 -join "`n")
\`\`\`

"@

Set-Content -Path docs\benchmark-before.md -Value $md -Encoding UTF8
Write-Host "Benchmark completed. Report written to docs\benchmark-before.md"
