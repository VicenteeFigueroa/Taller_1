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

---
*Este documento será actualizado progresivamente conforme se desarrollen los demás puntos del taller.*
