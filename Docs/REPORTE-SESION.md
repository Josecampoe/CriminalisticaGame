# Reporte de sesiones — El Caso 404

## Sesión 4 (19 de octubre de 2026, hasta las 9:30 AM)

**En resumen:** quedaron hechos la **tarea 1 (personajes)** y la **tarea 4 (cartas de contexto)**. Hay una **versión básica de la tarea 3 (ciudad)**, y se generaron el **build de Windows** y la **subida a GitHub** en ramas por integrante. **No alcanzó el tiempo** para la **tarea 2 (espacios más grandes)**.

### Qué se hizo
| Tarea | Estado | Detalle |
|---|---|---|
| Subida a GitHub | Hecha | `tools/split_by_owner.py` reparte el trabajo en una rama por integrante, con commits pequeños firmados por cada uno y la fecha original. Verifica que fusionar las 3 ramas da exactamente el mismo proyecto. Ramas: `jose/base-juego`, `majo/docs-ui-datos`, `daniel/core-pruebas`; las de esta sesión son `jose/sesion4`, `majo/sesion4` y `daniel/sesion4`. |
| 1. Personajes | Hecha | Los 8 personajes usan modelos humanoides de Quaternius en vez de cápsulas, con ropa según el guion, peinado, barba, gorra o placa, y escala según edad. Tienen animaciones de reposo, hablar, sentado y sentado hablando, creadas por código porque el paquete no trae ninguna. Al hablar, el personaje mira al jugador. Se conservan los colliders, `CharacterNpc` y el resaltado. Los sentados tienen su propia silla. |
| 4. Cartas de contexto | Hecha | Antes de c1_s1 (**Nueva partida**) y antes de c2_s1 (**Comenzar «Protocolo 404»**): fondo oscuro, hoja de expediente, sello CONFIDENCIAL, máquina de escribir (un clic muestra todo) y el botón "Comenzar investigación". Usan los textos exactos que me diste. La Comisaria Lozano quedó agregada al guion (§1 y §6). |
| Build de Windows | Hecho | `Builds/ElCaso404/ElCaso404.exe` (118 MB). Se regenera con **Caso404 → Build de Windows**. |
| Capturas | Parcial | En `Docs/capturas/`: la carta del caso 1 (escribiéndose y completa), los personajes de pie, sentados y hablando, la ciudad desde el balcón, el ventanal nocturno, Carlos en el café y el interrogatorio. Faltan la carta del caso 2 y una captura de cada escena. |
| 2. Espacios más grandes | **No se hizo** | El apartamento sigue del tamaño anterior. |
| 3. Ciudad alrededor | **Básica** | `CityKit` pone alrededor de 6 escenas un suelo de calle (ya no hay piso infinito), un anillo de edificios de Kenney (detallados cerca y mallas simples lejos), montañas lejanas, cielo nublado o nocturno con ventanas que brillan y niebla en el borde. La calle está a la altura de cada piso: 12 m abajo en el apartamento y 36 m en el piso ejecutivo. Hay colliders invisibles en el balcón y en el ventanal del piso ejecutivo, que ahora es de vidrio transparente. **Falta:** andenes, faroles y carros (el kit de Kenney **solo trae edificios**), iluminación horneada, una vista nocturna más clara en el piso ejecutivo y la Comisaría de noche en el caso 2. |

Pruebas: **40 EditMode y 6 PlayMode en verde** antes y después de los cambios. Las PlayMode ahora también pasan por las dos cartas.

### Qué debes revisar tú
1. **Cómo se ven los personajes.** Los cuerpos base del paquete gratuito son muy musculosos y la ropa queda ajustada, como un enterizo, porque está pintada sobre la piel. Si no te convence, la alternativa es un paquete con ropa modelada.
2. **Las poses en el juego.** En Play los personajes deben estar en reposo; los sentados, en su silla; y al hablarles, gesticular y mirarte. Revisa sobre todo la Comisaría y el café.
3. **La ciudad**: asómate al balcón del apartamento y al ventanal del piso ejecutivo; revisa que no haya bordes visibles ni caídas.
4. **Las dos cartas**: que el texto se lea bien en tu pantalla y que el clic complete el texto.
5. **El vestuario que elegí** sin que lo pidieras: Laura con uniforme de enfermera, Sebastián con buzo con capota y Elena con chaqueta y jeans (`DECISIONES.md`, N2).
6. **Los PR en GitHub** (enlaces abajo). Te recomiendo fusionar primero las ramas de la subida inicial y después las de la sesión 4: las de la sesión 4 continúan a las anteriores y no tienen conflictos.

### Cómo probar
- **En el Editor:** **Caso404 → Jugar desde el menú** (Ctrl+Shift+P) → **Nueva partida**. Debe aparecer la carta.
- **En el ejecutable:** abre `Builds/ElCaso404/ElCaso404.exe`.

### Para la próxima sesión
- **"Hagamos la tarea 2 y terminemos la 3"**: apartamento ampliado con pasillo 401–405 y ascensor, repartir las evidencias en el espacio, y completar la ciudad con andenes, faroles, carros, luz horneada y la Comisaría de noche en el caso 2.
- Capturas de cada escena y de la carta del caso 2 (necesita jugar el caso 1 hasta el final o una prueba que llegue ahí).

---

## Resumen (sesión 2, 18 de octubre de 2026)

**El caso 1 se juega completo**, desde el menú principal hasta sus 3 finales y "Tres meses después...". **El caso 2 también se juega completo**: sus 4 escenas, la Comisaría compartida, los 4 finales y el final secreto con la decisión sobre ALEX.

Todo se verificó en modo batch con **37 pruebas EditMode** y **6 pruebas PlayMode**, todas en verde. Las PlayMode recorren el juego desde el menú, como lo haría un jugador.

> **Importante:** igual que en la sesión 1, **nadie lo ha jugado todavía con teclado y mouse.** Las pruebas usan las mismas entradas que el jugador (inspeccionar, hablar, acusar, elegir botones), pero no mueven el mouse ni caminan. Tu primera partida completa es la prueba de verdad (sección 4).

---

## 1. Qué quedó hecho en esta sesión

| Fase | Commit | Contenido |
|---|---|---|
| G | `a476041` | Escena **c1_s2 Café La Esquina** (3 evidencias y Carlos, que no aparece si fue detenido). Kit común `SceneKit` para construir escenas. Botón "Terminar la escena" para las escenas sin decisión de cierre. Tema "Preguntar por la llave maestra". Código de acusación, final, fundido y menú. |
| H | `bc766a4` | Escena **c1_s3 Portería y sótano**: portería, sótano con casilleros, administración (el DVR desbloquea la grabación original), mantenimiento (llavero) y parqueadero (portátil). El overol aparece, o en su lugar las cenizas si Carlos fue detenido. Hernán y Andrés. |
| I | `90c3f1a` | **Comisaría**: tablero con todas las hipótesis del caso, pantalla de ALEX, sala de interrogatorio con los 4 personajes y "Expediente para la Fiscalía" (acusación: culpable + 3 pruebas). Andrés entrega su celular aquí; su análisis profundo activa `flag_c1_pista_nx`. |
| J | `9d4e350` | **Menú principal** (Nueva partida, Continuar, Salir), fundidos entre escenas, **pantallas de final** con el texto del guion y un resumen de lo que descubriste y lo que se te escapó, y **"Tres meses después..."**. |
| K | `42562f0` | **Pruebas PlayMode del caso 1 completo**: final verdadero, verdadero con Carlos detenido, incompleto y engaño. |
| L | `863387c`, `6f69458`, `54b1502` | **Caso 2 completo:** <ul><li>Datos: 15 evidencias, 11 mentiras, 4 personajes con Elena como aliada, 3 decisiones y 4 finales.</li><li>Escenas: **c2_s1 Sala de servidores B**, **c2_s2 Piso ejecutivo** y **c2_s3 Caseta y parqueadero**, con la Comisaría compartida.</li><li>Mecánicas: contraseña LUCERO, laboratorio externo para las placas, la copia espejo que entrega Inés, el testimonio de Inés como prueba sólida y el final secreto con `dec_c2_alex`.</li><li>Paso del caso 1 al 2 conservando flags, finales y `alexTrust`.</li><li>10 pruebas EditMode y 2 PlayMode nuevas.</li></ul> |

**Extras después de las fases (hasta las 10:20 AM):**
- Regla de Elena con la USB.
- Línea de objetivo en el HUD y menú de pausa con Esc.
- **Opciones** en el menú (sensibilidad del mouse, invertir Y, pantalla completa).
- Revisión visual de la UI con capturas (`UiScreenshots`), que corrigió el expediente, la inspección de objetos planos y el HUD encimado.
- Sonidos generados por código y sacudida de cámara en las contradicciones.
- Glitch "404" al abrir `directiva_404`.
- **Proxy de IA en `Server/`** (Node, sin dependencias, clave por variable de entorno, con modo de prueba); el `HttpAiClient` ya lo usa y se probó contra el modo de prueba.
- Prueba de integridad de las 8 escenas.
- `Docs/ARQUITECTURA.md`, para la sustentación.
- Atajos **Caso404 → Jugar desde el menú** (Ctrl+Shift+P) y **Borrar partida guardada**.

Estado final de las pruebas: **40 EditMode y 6 PlayMode en verde**, más 2 pruebas explícitas (capturas y proxy) que también pasaron.

Las decisiones de esta sesión están en `Docs/DECISIONES.md`, secciones S1–S5, G1–G4, H1–H3, I1–I6, J1–J4, K1–K2 y L1–L19.

## 2. Qué falló o quedó pendiente

**Problemas encontrados y resueltos durante la sesión**
- Un nombre de método chocaba con el namespace `Caso404.Player`. Se renombró el método.
- Una comprobación de la prueba contaba mal las evidencias de c1_s3: la grabación original no tiene objeto en la escena.
- En la primera versión, el celular de Andrés se podía obtener en el sótano. Ahora solo se entrega en la Comisaría, como dice el guion.

**Pendiente**
- **Prueba humana con teclado y mouse** de todo el recorrido (ver sección 4).
- **Escena Bootstrap persistente.** Hoy el estado pasa entre escenas por el archivo de guardado. Funciona, pero cada escena crea sus propios servicios.
- Elena en el final del engaño: resuelto en M1.
- **IA real**: el proxy de `Server/` existe, pero solo se probó en modo de prueba (no había clave). Para usarlo, define `ANTHROPIC_API_KEY`, arranca `node Server/proxy.mjs` y desmarca *Use Offline* en `AiSettings`.
- **Arte**: el greybox es de primitivas porque ProBuilder no está instalado. Faltan las fuentes Special Elite, Inter e IBM Plex, la iluminación horneada, el contorno de resaltado, los sonidos y la sacudida de pantalla.
- Algunas frases las escribí yo porque el guion no las trae (B10, G4, L7). Revísalas para la sustentación.

## 3. Decisiones más importantes (detalle en `Docs/DECISIONES.md`)
- **S3:** las escenas cargan el estado desde el guardado. Para jugar desde el principio, entra por **MainMenu → Nueva partida**.
- **S5:** el café y la portería se terminan desde el tablero con "Terminar la escena". La Comisaría solo se abre cuando ambas están completas.
- **I2:** el celular de Andrés solo se consigue en la Comisaría, presentándole una prueba sólida (llavero, overol o grabación original).
- **L2 y L3:** el portátil de Tomás solo cuenta abierto con LUCERO, y la cámara del parqueadero solo identifica el carro de Valeria tras el laboratorio externo. Agregué dos flags técnicos que el guion no nombra.
- **L12:** la Comisaría es una sola escena para los dos casos. El juego elige `c1_s4` o `c2_s4` según el caso en curso.

## 4. Cómo probar el juego completo en Unity

1. Abre el proyecto con Unity **6000.6.6f1** y espera a que importe.
2. Opcional: menú **Caso404 → Construir todo (datos y escenas)** para regenerar todo. Las escenas ya están en el repositorio.
3. Abre `Assets/_Project/Scenes/MainMenu.unity` y pulsa **Play**. Elige **Nueva partida**.
4. Controles:

   | Tecla | Acción |
   |---|---|
   | WASD | Moverse |
   | Mouse | Mirar |
   | E | Interactuar |
   | Tab | Expediente |
   | Q | ALEX |
   | H | Hipótesis |
   | Esc | Cerrar panel |
   | Clic izquierdo sostenido | Girar el objeto en el modo inspección |

5. **Recorrido del final verdadero del caso 1:**
   1. **c1_s1 Apartamento:**
      1. Inspecciona y guarda las 7 evidencias y analízalas con **Tab**.
      2. Con **Q**, cuestiona la observación de ALEX sobre el balcón usando la **Cerradura**.
      3. Habla con **Laura**: preséntale el **Teléfono** y luego pulsa **Hablarle con calma**.
      4. Al despedirte, en la decisión del cuaderno elige **guardarlo fuera del sistema**.
      5. Pulsa **H**, responde **Con una llave** y **las tres personas**, y en la decisión elige **"Revisar primero la portería y el sótano"**.
   2. **c1_s3 Portería y sótano:**
      1. Recoge todo y analiza el **DVR**: aparece la grabación original.
      2. Gira el **overol** hasta ver el vidrio en la manga.
      3. Con **Q**, cuestiona el ERROR 404 usando la **Grabación original**, y la observación sobre el overol usando el **Overol**.
      4. A Hernán preséntale el **Recibo de la tienda**.
      5. Pulsa **H**, responde **Andrés** y **Para simular un robo**, y luego **Terminar la escena**. Ve al café.
   3. **c1_s2 Café:** recoge y analiza las 3 evidencias, pulsa **H** y responde **No, estaba en el café**. Luego **Terminar la escena** y ve a la Comisaría.
   4. **c1_s4 Comisaría:**
      1. A **Andrés** preséntale el **Registro del llavero**: te da su celular.
      2. En el expediente (**Tab**), analiza el celular y haz el **Análisis profundo**.
      3. En el **Expediente para la Fiscalía** acusa a **Andrés** con el **llavero**, el **overol** y el **celular**.
6. Después del final, pulsa **Comenzar «Protocolo 404»** para jugar el caso 2. Pistas para su final secreto:
   - La contraseña del portátil de Tomás es **LUCERO**: está en el boceto y en el post-it, y Elena también la sugiere.
   - Las placas se leen con el **Análisis de laboratorio externo** de la cámara del parqueadero.
   - A **Inés** preséntale el **libro de rondas** y elige **protegerla**: te da la copia espejo.
   - Acusa a **Valeria**.
7. Para correr las pruebas: **Window → General → Test Runner**. En **EditMode → Run All** deben pasar 37; en **PlayMode → Run All**, 6 (tardan un par de minutos porque cargan las escenas).
8. En el Inspector no hay que configurar nada. Si quieres probar una escena suelta sin pasar por el menú, abre esa escena y dale Play: usará tu última partida guardada.
9. Si algo falla, copia el primer error rojo de la consola para la próxima sesión.

## 5. Qué pedirme en la siguiente sesión
1. **"Ajustemos lo que encontré jugando"**: velocidad, sensibilidad del mouse, textos que no caben, objetos difíciles de alcanzar.
2. **"Escena Bootstrap persistente y opciones"** (volumen, sensibilidad, pantalla completa).
3. **"Regla de Elena con la USB"** y repasar los textos que escribí por mi cuenta.
4. **"Servidor proxy de IA en `Server/`"**, para que los personajes respondan preguntas libres (con la clave en una variable de entorno).
5. **"Arte"**: instalar ProBuilder, las fuentes del `UITheme`, modelos low-poly (Kenney o Quaternius) e iluminación horneada.

---

## Sesión 1 (resumen)
Fases A–F: preparación del proyecto, ScriptableObjects y generador de datos del caso 1, núcleo (`GameState`, servicios, guardado, `EndingEvaluator`), jugador en primera persona con inspección, UI básica y la escena c1_s1 hasta `dec_c1_s1`. Commits `10af5de` a `836946b`.

---

## Cierre de la sesión 2 (12:31 PM)

El trabajo se detuvo a las 10:20 AM porque se alcanzó el límite de uso. Se retomó a las 12:31 PM, después de la hora límite (11:45), solo para cerrar este reporte. No hubo cambios de código después de las 10:20: el último commit de código es `02c618d` (glitch 404 y atajos de Editor).

Estado final:
- Fases A a L completas. El caso 1 y el caso 2 se juegan de principio a fin, incluido el final secreto.
- 40 pruebas EditMode y 6 PlayMode en verde. Las pruebas explícitas de capturas y del proxy también pasaron.
- Lo pendiente y los pasos para probar están en las secciones 2 y 4 de este reporte.
