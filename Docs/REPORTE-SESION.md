# Reporte de la sesión — 18 de octubre de 2026

**Resumen:** las seis fases (A a F) quedaron terminadas, cada una con su commit, a las 9:17 AM, antes del límite de las 11:45. La escena **c1_s1 "Apartamento 404"** se puede jugar desde la intro hasta la decisión `dec_c1_s1`. Hay **27 pruebas EditMode** y **1 prueba PlayMode**, todas en verde.

> **Importante:** todo se verificó con pruebas automáticas y con capturas de la escena. **Nadie lo ha jugado todavía con teclado y mouse.** Tu primera partida es la prueba de verdad (pasos en la sección 4).

---

## 1. Qué quedó hecho

| Fase | Commit | Contenido |
|---|---|---|
| — | `070cce9` | Tus documentos (guion, CLAUDE.md, skills) quedaron guardados en git. Corregí en las skills tres diferencias con el guion (`alexTrust` empieza en 60, contradicciones por caso, varias refutaciones por mentira). |
| A | `10af5de` | Paquetes (se agregó Test Framework y se quitaron dos experimentales), carpetas `_Project/`, asmdefs (juego, editor y pruebas), capas `Interactable` e `Inspection`, URP como pipeline por defecto, recursos de TextMeshPro. |
| B | `577c7b9` | ScriptableObjects de casos, personajes, ALEX, evidencias, mentiras, hipótesis, decisiones, finales, escenas y flags. Un script de Editor genera **los 54 assets del caso 1** con los IDs y textos del guion. |
| C | `3080d34` | `GameState` y `GameStateService`, `SaveService` (JSON), canales de eventos, `EvidenceService`, `AlexService` (Aceptar/Cuestionar y confianza), `DecisionService`, `HypothesisService`, `AccusationService`, `EndingEvaluator`. Las pruebas recorren **los 3 finales**, incluido el **final verdadero con Carlos detenido**. |
| D | `a323504` | Jugador en primera persona (`CharacterController`, Input System), interacción por raycast, resaltado ámbar/blanco y modo inspección: el objeto se gira con el mouse y el detalle oculto se revela al girarlo. También entró el `DialogueService` con la IA sin conexión. |
| E | `22903ea` | UI completa con el `UITheme` noir: HUD, inspección, expediente con análisis, diálogo (frases de respaldo del guion), panel de ALEX con Aceptar/Cuestionar y glitch, tablero de hipótesis y decisión. |
| F | `4e5a378` | Escena `Assets/_Project/Scenes/Apartamento404.unity` en greybox: sala, balcón y pasillo, las 7 evidencias, Laura, el jugador, la UI y la iluminación. Está en Build Settings como escena 0. |

**Qué se puede hacer en c1_s1:**
- Inspeccionar y guardar las 7 evidencias, y analizarlas en el expediente.
- Ver cómo ALEX culpa a Carlos al analizar la huella, y cómo da ERROR 404 al analizar la cámara del pasillo.
- Cuestionar a ALEX con la ventana o la cerradura: cuenta como contradicción y baja su confianza.
- Hablar con Laura: preguntarle, presentarle evidencias o testimonios, y descubrir su mentira de la hora con el teléfono.
- Después, **"Hablarle con calma"**: entrega el cuaderno, y al cerrar la conversación se abre `dec_c1_cuaderno`.
- Responder la hipótesis `hyp_c1_s1` y tomar `dec_c1_s1`. La partida se guarda.

**IA:** el juego usa solo `OfflineAiClient`. `IAiClient`, `HttpAiClient`, `AiSettings` y el validador de respuestas quedaron listos para conectar el proxy más adelante. No hay ninguna clave de API en el proyecto.

## 2. Qué falló o quedó pendiente

**Problemas que encontré y resolví**
- `AssetDatabase.ImportPackage` no termina en modo batch. Los recursos de TMP se extrajeron a mano del `.unitypackage`, y el resultado es el mismo.
- Crear una escena nueva descargaba los datos ya cargados y producía un error de referencia nula. Se corrigió el orden.
- La prueba PlayMode no corre con `-nographics`, porque el modo inspección usa una RenderTexture. Se corre en batch con gráficos.

**Pendiente**
- **Escenas c1_s2 (Café), c1_s3 (Portería y sótano) y c1_s4 (Comisaría).** Sus datos ya existen, pero no se han construido. Por eso, después de `dec_c1_s1` aparece "Fin de la demo".
- **Pantallas de acusación y de finales.** La lógica (`AccusationService`, `EndingEvaluator`) está hecha y probada; falta la UI.
- La opción "Preguntar por la llave maestra" con Hernán y Andrés (cuando existe `flag_c1_h_llave`). Corresponde a c1_s3.
- Escena `Bootstrap` persistente, menú principal y botón "Continuar". Hoy cada escena crea sus servicios y el estado pasa por el guardado.
- **ProBuilder y Cinemachine no se instalaron** (motivo en A3 y A4 de `DECISIONES.md`). El greybox es de primitivas.
- Fuentes Special Elite, Inter e IBM Plex Mono. Se usa LiberationSans; el `UITheme` ya tiene los campos para cambiarlas.
- Detalles visuales y de sensación:
  - iluminación sin hornear;
  - resaltado por tinte en vez de contorno;
  - fondo oscurecido en vez de desenfocado;
  - faltan los sonidos y la sacudida de pantalla de la skill de UI.
- Servidor proxy para la IA (`Server/`).
- Queda un aviso de compilación (API obsoleta) en `ProjectSetup.cs`. No afecta el juego.

## 3. Decisiones que tomé

Todas están en **`Docs/DECISIONES.md`**, con su motivo y su número. Las que más afectan cómo se juega:
- **B9:** en `dec_c1_s1` A (detener a Carlos), el jugador elige después entre ir al café o a la portería.
- **B10 y B11:** escribí dos reacciones que el guion no traía. Las frases "Presionar" del guion solo se dicen cuando ya se descubrió una mentira del personaje.
- **B12:** el celular de Andrés solo se entrega si su mentira se refuta con una evidencia sólida, no con el testimonio de Hernán.
- **C3 y C4:** Aceptar a ALEX suma +5 una sola vez. Cuestionarlo con una evidencia equivocada no castiga.
- **E5:** el tablero no dice si la respuesta es correcta; así el jugador no prueba hasta acertar.
- **E6:** la decisión del cuaderno se abre al cerrar la conversación con Laura.
- **E10:** la jerarquía de cada pantalla, que la skill de UI pedía aprobar antes de construirla. Revísala.

## 4. Qué debes hacer tú en Unity para probarlo

1. Abre el proyecto en Unity Hub (versión **6000.6.6f1**) y espera a que termine de importar.
2. Abre `Assets/_Project/Scenes/Apartamento404.unity`.
3. Pulsa **Play** y haz clic dentro de la ventana *Game*.
4. Controles:

   | Tecla | Acción |
   |---|---|
   | WASD | Moverse |
   | Mouse | Mirar |
   | E | Interactuar |
   | Tab | Expediente |
   | Q | Panel de ALEX |
   | H | Hipótesis |
   | Esc | Cerrar panel |
   | Clic izquierdo sostenido | Girar el objeto en el modo inspección |

5. Recorrido sugerido:
   1. Lee la intro y pulsa **Empezar**.
   2. Apunta a cada objeto con brillo ámbar y pulsa **E**. Gíralo y pulsa **Guardar en expediente**. Hay 7: ventana, cerradura de la puerta, huella y colilla en el balcón, teléfono frente al sofá, escritorio y cámara del pasillo (arriba, sobre la puerta).
   3. Pulsa **Tab** y **Analiza** cada evidencia. Al analizar la huella y la cámara, aparece un aviso de ALEX.
   4. Pulsa **Q**. En la observación sobre el balcón, pulsa **Cuestionar** y elige **Ventana rota del balcón**: debe salir "Contradicción detectada".
   5. Sal al pasillo y habla con **Laura**. Pulsa **Presentar evidencia → Teléfono bajo el sofá**. Luego aparece **Hablarle con calma**: púlsalo. Al **Despedirte**, se abre la decisión del cuaderno.
   6. Pulsa **H** o usa el ascensor del pasillo. Responde las dos preguntas y pulsa **Tomar la decisión**.
6. Para correr las pruebas: **Window → General → Test Runner**. En **EditMode → Run All** deben pasar 27; en **PlayMode → Run All**, 1.
7. **En el Inspector no hay que configurar nada**: el builder dejó todo enlazado. Si quieres:
   - En el objeto `GameContext`, *Start New Game* empieza una partida nueva en cada Play (está activo).
   - `Assets/_Project/Data/AiSettings` → *Use Offline* debe quedar activo.
8. Si borras o cambias algo y quieres regenerarlo, usa el menú **Caso404**:

   | Opción | Qué hace |
   |---|---|
   | Generar datos caso 1 | Vuelve a crear los assets del caso 1 |
   | Crear assets compartidos | Vuelve a crear el `UITheme`, `AiSettings` y los canales de eventos |
   | Construir escena c1_s1 | Reemplaza la escena (se pierden los cambios hechos a mano) |
   | Capturar vistas de c1_s1 | Guarda capturas en `Logs/preview_*.png` |

9. Si algo se ve raro, revisa la consola y copia el primer error rojo para la próxima sesión.

## 5. Qué pedirme en la siguiente sesión

1. **"Revisemos tu prueba de c1_s1"**: me cuentas qué se sintió mal (velocidad, sensibilidad del mouse, textos que no caben) y lo ajusto.
2. **"Construye c1_s3 Portería y sótano"**: es la escena con las pruebas clave contra Andrés (llavero, DVR y grabación original, overol con detalle oculto, portátil), más Hernán, Andrés y la opción de la llave maestra.
3. **"Construye c1_s2 Café La Esquina"**: es corta (3 evidencias y Carlos).
4. **"Construye la Comisaría con la acusación y las pantallas de los 3 finales"**: así el caso 1 queda completo de principio a fin.
5. **"Agrega la escena Bootstrap, el menú principal y Continuar"**.
6. Opcional: aprobar la instalación de ProBuilder y las fuentes, y luego el proxy de IA en `Server/`.
