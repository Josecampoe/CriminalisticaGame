---
paths:
  - "Assets/_Project/**/*.cs"
---

# Reglas para scripts C#

- Un `MonoBehaviour` o `ScriptableObject` por archivo; el nombre del archivo es igual al de la clase
- Namespace `Caso404.<Sistema>` (ej. `Caso404.Evidence`)
- Campos privados con `[SerializeField] private` y `_camelCase`; propiedades públicas de solo lectura cuando haga falta exponer
- `PascalCase` para clases, métodos y propiedades; `camelCase` para variables locales
- Cachea componentes en `Awake`; suscríbete a eventos en `OnEnable` y desuscríbete en `OnDisable`
- Nada de lógica pesada ni `GetComponent` dentro de `Update`
- Comentarios `///` en métodos públicos, en español
- Sin `Debug.Log` sueltos en código final: usa un logger con condición o quítalos
