---
name: revisar
description: Revisión de calidad del proyecto antes de un commit o una entrega al profesor.
disable-model-invocation: true
---

Revisa los cambios recientes (`git diff` y archivos nuevos) y el estado del proyecto:

1. **Compila**: consola de Unity sin errores ni advertencias nuevas.
2. **Convenciones**: namespaces, `[SerializeField] private`, eventos con desuscripción en `OnDisable`, nada de `Find` en tiempo de juego.
3. **Regla de oro**: ninguna decisión de historia o final depende de lo que responda la IA.
4. **Seguridad**: ninguna clave de API, token o URL privada en `Assets/`, ScriptableObjects ni archivos versionados. Revisa también `.gitignore`.
5. **Diseño justo**: cada `allowedLie` tiene evidencia que la refute; cada final es alcanzable (ejecuta las pruebas EditMode).
6. **UI**: colores y fuentes salen del `UITheme`; textos en español; tamaño mínimo de diálogo respetado.
7. **Modo sin conexión**: el juego avanza con `OfflineAiClient`.

Entrega una lista corta: ✅ lo que está bien, ⚠️ lo que conviene mejorar, ❌ lo que bloquea. Propón el mensaje de commit. No cambies nada sin preguntarme.
