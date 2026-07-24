# Evidencia de Configuración CORS Restrictiva

Este documento valida la correcta configuración de la política CORS (Cross-Origin Resource Sharing) en el servidor ASP.NET Core, cumpliendo el requerimiento de permitir solo orígenes confiables.

## Configuración Implementada (`Program.cs`)
Se aplicó la política con el mínimo alcance necesario usando `UseCors` con las siguientes reglas explícitas:
- **Orígenes permitidos:** Únicamente `https://trusted.shortly.com`
- **Métodos permitidos:** Solo `GET` y `POST`
- **Headers permitidos:** Solo `Content-Type` y `Authorization`

## Flujo Preflight (Petición `OPTIONS`)

Cuando un navegador intenta realizar una petición de origen cruzado compleja (ej. un `POST` con `Content-Type: application/json`), primero envía una petición preliminar de tipo `OPTIONS` (Preflight request) para preguntarle al servidor si la operación está permitida.

### 1. Test con Origen Permitido (`https://trusted.shortly.com`)
Se simuló la petición `OPTIONS` indicando el origen confiable. El servidor respondió entregando explícitamente los headers `Access-Control-Allow-*`:

```text
--- TEST ORIGEN PERMITIDO ---
Origen: https://trusted.shortly.com
Status: NoContent
Access-Control-Allow-Origin: https://trusted.shortly.com
Access-Control-Allow-Methods: GET,POST
Access-Control-Allow-Headers: Content-Type,Authorization
```
**Efecto:** El navegador ve estos headers, confirma que coinciden con su origen e intención, y procede a enviar la petición `POST` real.

### 2. Test con Origen Denegado (`https://evil.hacker.com`)
Se simuló la misma petición `OPTIONS` pero desde un origen no autorizado. El middleware procesa la petición pero omite deliberadamente inyectar los headers CORS:

```text
--- TEST ORIGEN DENEGADO ---
Origen: https://evil.hacker.com
Status: NoContent
Access-Control-Allow-Origin: (No presente, CORS bloqueado por el navegador)
```
**Efecto:** El navegador recibe un estatus `204` pero al no encontrar el header `Access-Control-Allow-Origin`, asume que el servidor rechaza el origen y aborta inmediatamente la conexión, protegiendo al usuario.
