# Request Tracing (X-Request-Id)

Este documento detalla la implementación de trazabilidad de peticiones mediante el header `X-Request-Id` y la correlación de logs usando Serilog.

## Implementación
Se creó el middleware `RequestTracingMiddleware` que intercepta todas las peticiones al inicio del pipeline (`Program.cs`). 
Su función es:
1. Buscar si el cliente ya envió un `X-Request-Id`. Si no, usa el `TraceIdentifier` nativo de ASP.NET Core o genera un nuevo UUID.
2. Adjuntar este ID como header `X-Request-Id` en la respuesta (útil para que el cliente lo guarde si necesita reportar un error de soporte).
3. Enriquecer el contexto de Serilog (`LogContext.PushProperty`) para que **todos** los logs generados durante la vida de esa petición incluyan este ID.

Se configuró el `outputTemplate` en `appsettings.json` para que los logs por consola expongan explícitamente el campo:
`{Timestamp:yyyy-MM-dd HH:mm:ss,fff} [{Level:u8}] [ReqId={RequestId}] ...`

## Validación y Ejemplos
Al hacer una petición al servidor (ej. un link inexistente `/missing-link` que detona un redirect fallido y logs de error en EF Core), podemos observar que el ID de la petición se propaga por todos los eventos de la aplicación de manera uniforme:

```text
2026-07-23 20:37:32,654 [DEBUG] [ReqId=0HNN93EVU0NVU:00000001] Shortly.Application.Services.LinkService ... - Retrieving link with shortUrl: missing-link
2026-07-23 20:37:32,801 [ERROR] [ReqId=0HNN93EVU0NVU:00000001] Microsoft.EntityFrameworkCore.Database.Command ... - Failed executing DbCommand
2026-07-23 20:37:32,815 [ERROR] [ReqId=0HNN93EVU0NVU:00000001] Microsoft.EntityFrameworkCore.Query ... - An exception occurred while iterating over the results of a query
```

Este RequestID exacto (`0HNN93EVU0NVU:00000001`) será retornado al cliente en el header `X-Request-Id` HTTP de la respuesta.

## ¿Cómo Usarlo para Diagnóstico?
Si un usuario reporta que una URL no redirige o que la plataforma está lenta:
1. Pídele que abra las herramientas de desarrollador en su navegador (Network Tab) y te comparta el header `X-Request-Id` de la petición fallida.
2. Filtra los logs en tu servidor buscando exclusivamente ese ID (ej. usando grep, o en herramientas de logging centralizado como Kibana, Datadog o Seq: `RequestId = '0HNN93EVU0NVU:00000001'`).
3. Esto te entregará el hilo completo de ejecución exacto de *ese* usuario aislado del ruido del resto del tráfico.
