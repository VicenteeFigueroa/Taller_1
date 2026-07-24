# Liveness Health Check Endpoint

Este documento describe la configuración del endpoint de estado (Health Check) implementado en la aplicación, diseñado para su integración con sistemas de monitoreo o balanceadores de carga (como Kubernetes, Docker Swarm, o AWS ALB).

## Implementación (`Program.cs`)

Se habilitó el middleware nativo de .NET `AddHealthChecks()` y se configuró un enrutamiento en `/health` con un `ResponseWriter` personalizado. 
La lógica captura el momento exacto en el que levanta la aplicación (`DateTimeOffset.UtcNow` al inicio de `Program.cs`) y calcula el tiempo transcurrido para reportarlo como `uptime`.

## Payload de Respuesta

Se probó el endpoint haciendo llamadas sucesivas para verificar que el servidor levanta correctamente y el tiempo de actividad aumenta.

**Ejemplo de respuesta (Status 200 OK):**
```json
{
  "status": "Healthy",
  "uptime": "00:00:03.2236346",
  "timestamp": "2026-07-24T00:57:43.0601925+00:00"
}
```

### Justificación de los campos
- **`status`**: Indica si la aplicación está respondiendo. Actualmente siempre devuelve `Healthy` ya que sirve como una prueba *liveness* (si el proceso no responde, el request dará timeout y el orquestador sabrá que la aplicación murió).
- **`uptime`**: Expone el tiempo de actividad continuo del proceso en formato `d.hh:mm:ss.fffffff`. Extremadamente útil para detectar *CrashLoops* (reinicios constantes de la aplicación) desde el sistema de monitoreo.
- **`timestamp`**: Sirve para confirmar que la respuesta no es un caché pegado y que el servidor está generando respuestas dinámicas en tiempo real.
