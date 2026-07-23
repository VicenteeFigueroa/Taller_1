# Evidencia de Compresión (Brotli + Gzip)

Este documento valida la correcta configuración del middleware de compresión (`AddResponseCompression`) en el servidor ASP.NET Core, como parte de los entregables del taller.

## Resultados del Script de Validación (`check-compression.ps1`)

Se realizaron peticiones al endpoint raíz (`/`) negociando distintos algoritmos de compresión mediante el header `Accept-Encoding`. Se midieron los bytes crudos transferidos por la red:

```text
Verificando compresión en: http://localhost:5064/

[Identity] Tamaño sin comprimir: 5137 bytes

[Brotli] Content-Encoding recibido: br
[Brotli] Tamaño comprimido: 1448 bytes
[Brotli] Ahorro: 3689 bytes (71.81%)

[Gzip] Content-Encoding recibido: gzip
[Gzip] Tamaño comprimido: 1730 bytes
[Gzip] Ahorro: 3407 bytes (66.32%)

Prueba completada exitosamente.
```

## Conclusión
La compresión funciona correctamente:
1. Las respuestas respetan la negociación de contenido y devuelven el header `Content-Encoding` adecuado (`br` o `gzip`).
2. **Brotli** demostró ser el más eficiente, reduciendo el payload de la página HTML estática en un **~72%**, lo cual acelerará sustancialmente el tiempo de entrega (Delivery Time) hacia los clientes. 
3. **Gzip** sirve como un excelente fallback para clientes antiguos, logrando un ahorro del **~66%**.
