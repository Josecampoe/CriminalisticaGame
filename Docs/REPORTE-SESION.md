# Reporte de sesiones — El Caso 404

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

Las decisiones de esta sesión están en `Docs/DECISIONES.md`, secciones S1–S5, G1–G4, H1–H3, I1–I6, J1–J4, K1–K2 y L1–L19.

## 2. Qué falló o quedó pendiente

**Problemas encontrados y resueltos durante la sesión**
- Un nombre de método chocaba con el namespace `Caso404.Player`. Se renombró el método.
- Una comprobación de la prueba contaba mal las evidencias de c1_s3: la grabación original no tiene objeto en la escena.
- En la primera versión, el celular de Andrés se podía obtener en el sótano. Ahora solo se entrega en la Comisaría, como dice el guion.

**Pendiente**
- **Prueba humana con teclado y mouse** de todo el recorrido (ver sección 4).
- **Escena Bootstrap persistente.** Hoy el estado pasa entre escenas por el archivo de guardado. Funciona, pero cada escena crea sus propios servicios.
- **Elena en el final del engaño**: el guion dice que solo coopera si le muestras la USB. Hoy solo cambia su saludo (L19).
- **IA real**: el `HttpAiClient` está preparado, pero falta el servidor proxy en `Server/`. Todo funciona con las frases de respaldo del guion.
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
