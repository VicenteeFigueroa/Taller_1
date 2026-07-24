# Registro de Prompts de Inteligencia Artificial (IA)

Este archivo documenta los prompts utilizados durante el desarrollo de este taller con el asistente de IA, en cumplimiento con las instrucciones del proyecto.


"Necesito  correr `ab -n 1000 -c 10` en al menos 3 endpoints antes y después de los cambios, para hacer un benchmark,
registrando requests/seg, p50/p90/p99, transfer rate y fallos, con las mismas 
condiciones de test en ambas corridas. ¿Qué endpoints eligirias para hacerlo?"

"Genérame un archivo llamado SecurityHeadersMiddleware, 
que agregue estos headers de seguridad: 
Strict-Transport-Security, X-Content-Type-Options, X-Frame-Options, 
Referrer-Policy y Permissions-Policy. Necesito que cada header tenga un 
comentario explicando qué tipo de ataque mitiga"


"Estoy generando el shortUrl con `Ulid.NewUlid().ToString()[..12]`, pero el ULID 
codifica un timestamp en sus primeros bits, así que se puede inferir cuándo se 
creó un link a partir del código público. Necesito reemplazar eso por una 
representación irreversible, manteniendo unicidad y caracteres URL-safe. 
¿Cómo lo implemento y cómo valido que ya no queda el patrón de orden temporal?"

"Genérame un archivo llamado PerformanceMeasurementMiddleware que mida la duración de 
cada request y agregue el header `X-Response-Time: <ms>ms` a la respuesta, necesito 
que genere un log específico para las requests que superen los 500ms, incluyendo método, 
path, status code y tiempo transcurrido, para poder hacer filtros mas facil."

"Necesito configurar compresión de respuestas usando AddResponseCompression() 
con los proveedores Brotli y Gzip. Configúrame los MIME types de forma segura"

"Ayúdame a generar un script que verifique la compresión de respuestas: necesito 
comparar el tamaño de / sin header Accept-Encoding vs con Accept-Encoding: br, y 
luego lo mismo con gzip. Quiero que muestre el Content-Encoding recibido y el % de 
bytes ahorrados"

"Ayúdame a definir una política de CORS que permita solo orígenes de confianza y 
bloquee llamadas cross-origin no autorizadas. Necesito que la política especifique 
explícitamente los orígenes, métodos y headers permitidos."

"Genérame un middleware llamado RequestTracingMiddleware que cree o propague un 
X-Request-Id que lo agregue al contexto de logging de Serilog (LogContext) para que 
aparezca en todos los logs de esa request, y lo devuelva en el header de la respuesta. 
Necesito igual que funcione en flujos de redirección, para que el mismo ID quede relacionado 
en los logs del request original y en los logs de la respuesta final."

---
*Este documento será actualizado progresivamente conforme se desarrollen los demás puntos del taller.*
