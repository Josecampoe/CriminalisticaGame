---
paths:
  - "Assets/_Project/Scripts/UI/**"
  - "Assets/_Project/UI/**"
---

# Reglas para la interfaz

- Antes de crear o cambiar una pantalla, carga la skill `diseno-ui` y sigue su paleta y tipografía
- Colores y tamaños salen del `UITheme` (ScriptableObject), nunca escritos a mano en cada componente
- Todo texto con TextMeshPro; nada de `UnityEngine.UI.Text`
- Los scripts de UI no cambian el `GameState` directamente: llaman a un servicio o emiten un evento
