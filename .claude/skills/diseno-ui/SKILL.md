---
name: diseno-ui
description: Identidad visual e interfaces de El Caso 404 (estilo noir de investigación). Usar al diseñar o construir cualquier pantalla, HUD, panel, menú, cuadro de diálogo, tablero de evidencias, pantalla de final o al elegir colores, fuentes, iconos, animaciones o sonidos de interfaz.
---

# Diseño de interfaz — El Caso 404

## Dirección visual
Noir moderno con un toque digital: la investigación es física (expediente, fotos, tablero de corcho, hilo rojo) y Alex es la capa digital que se cuela en ella (texto monoespaciado, leve parpadeo, color cian). La tensión visual entre "papel" y "pantalla" cuenta la historia: cuando el jugador sospecha de Alex, su capa empieza a verse "corrupta" (glitch sutil, colores desaturados, número 404 apareciendo).

## Paleta (tokens del `UITheme`)
| Token | Hex | Uso |
|---|---|---|
| `bg_dark` | #14161B | Fondos de panel, overlay |
| `paper` | #E9E2D0 | Fichas de evidencia, expediente |
| `ink` | #1E1E1E | Texto sobre papel |
| `text_light` | #EDEDED | Texto sobre fondo oscuro |
| `accent_red` | #C0392B | Hilo del tablero, contradicciones, acusar |
| `alex_cyan` | #4FD1C5 | Todo lo que dice o toca Alex |
| `warning_amber` | #E0A526 | Pistas nuevas, sin analizar |
| `muted` | #6B7280 | Texto secundario, deshabilitado |

Contraste mínimo 4.5:1 para texto. No dependas solo del color: añade icono o etiqueta (ej. contradicción = rojo + icono de rayo + texto).

## Tipografía (TextMeshPro, fuentes libres de Google Fonts)
- Títulos y expediente: **Special Elite** (máquina de escribir)
- Texto de interfaz y diálogos: **Inter** o **IBM Plex Sans**
- Alex y datos técnicos: **IBM Plex Mono**
- Escala: 48 / 32 / 24 / 18 / 14 px de referencia a 1920×1080. Texto de diálogo nunca por debajo de 18.

## Pantallas del prototipo
1. **Menú principal** — título con efecto de máquina de escribir, Nueva partida / Continuar / Opciones / Salir
2. **HUD de exploración (primera persona)** — mínimo:
   - retícula pequeña en el centro, que crece o cambia de color al apuntar a algo investigable;
   - prompt bajo la retícula ("E — Inspeccionar", "E — Hablar con Laura");
   - nombre de la escena al entrar (se desvanece en 3 s);
   - atajos a Expediente (Tab) y a ALEX (Q);
   - contador de evidencias sin analizar.
2b. **Modo inspección** — fondo desenfocado u oscurecido, objeto 3D al centro que se rota con el mouse, nombre del objeto arriba y botones "Guardar en expediente" y "Soltar". Algunos objetos tienen detalles que solo se ven al girarlos (por ejemplo, el vidrio en la manga del overol).
3. **Expediente / inventario** — fichas de papel en cuadrícula; cada ficha: foto, nombre, estado (nueva, analizada), botón Analizar
4. **Análisis de evidencia** — vista de la ficha ampliada + resultado que aparece como sello o informe de laboratorio
5. **Diálogo** — retrato del personaje, nombre, texto con efecto de escritura, campo para escribir la pregunta + 3–4 botones rápidos (Preguntar por el crimen, por otra persona, por una evidencia, Presionar). Indicador de "pensando" mientras responde la IA
6. **Panel de Alex** — estilo terminal en cian, deslizable desde el lateral; sus sugerencias son tarjetas que el jugador puede "Aceptar" o "Cuestionar"
7. **Tablero de hipótesis** — corcho con fichas y personajes; el jugador une evidencias con hilo rojo y responde ¿Quién? ¿Cómo? ¿Qué lo prueba?; opciones bloqueadas salen grises con candado y el porqué
8. **Decisión** — pantalla dramática, oscurecida, 2–3 opciones grandes, confirmación antes de decisiones irreversibles
9. **Final** — ilustración, título del final, resumen de lo que el jugador descubrió y lo que se le escapó

## Construcción en Unity (uGUI)
- Un `Canvas` por capa: HUD, Paneles, Diálogo, Overlay (transiciones). `Canvas Scaler` en *Scale With Screen Size*, referencia 1920×1080, match 0.5.
- Layouts con `Horizontal/Vertical/Grid Layout Group` + `Content Size Fitter`; anclajes correctos para 16:9 y 16:10.
- Cada pantalla es un prefab con un script `XxxView` que solo muestra datos y expone eventos (`OnAnalyzeClicked`). Un `XxxPresenter` o el servicio correspondiente conecta vista y lógica.
- Abrir y cerrar paneles con un `UIManager` que gestiona una pila (Escape cierra el panel de arriba).
- Animaciones cortas (150–300 ms) con `CanvasGroup.alpha` y escala; usa curvas *ease-out*. Nada de animaciones que retrasen la respuesta del jugador.
- Navegación con teclado/mando mediante `EventSystem` y `Selectable.navigation`; foco visible.

## Particularidades del 3D
- La UI va en Canvas *Screen Space – Overlay*. Usa *World Space* solo para cosas diegéticas, como la pantalla de un computador dentro de la escena o el tablero de corcho de la Comisaría.
- Resalta el objeto apuntado con un contorno suave (shader de outline o capa de render de URP) en `warning_amber` si la evidencia es nueva y en blanco si ya se revisó.
- Al abrir cualquier panel: pausa el movimiento, libera el cursor y aplica un desenfoque o viñeta al fondo (volumen de post-procesado). Al cerrarlo, vuelve el cursor bloqueado al centro.
- En los diálogos, la cámara se acerca al rostro del personaje (Cinemachine o interpolación de la cámara). El retrato en la UI es opcional si el modelo se ve bien.
- Ambientación noir en la escena: luces cálidas puntuales, sombras marcadas, niebla ligera y una paleta de colores que coincida con la de la UI.

## Sensación (feedback)
- Recoger evidencia: sonido de cámara/papel + ficha que vuela al icono del expediente
- Contradicción detectada: hilo rojo que se tensa, leve sacudida de pantalla, sonido grave
- Alex miente y el jugador lo descubre: glitch de 0.2 s sobre el panel cian
- Siempre confirma una acción del jugador en menos de 100 ms, aunque la IA tarde en responder

## Al diseñar
- Si no existe el `UITheme`, créalo primero con los tokens de arriba.
- Antes de construir una pantalla nueva, describe en 5–8 líneas su jerarquía (qué ve primero el jugador, qué acción es la principal) y espera mi visto bueno.
- Si te pido maquetas para presentar, puedes generarlas como imagen o HTML fuera de Unity antes de implementarlas.
