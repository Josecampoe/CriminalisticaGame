# Reporte de sesiones — El Caso 404

## Sesión visual (29 de octubre de 2026)

**En resumen:** las 7 escenas tienen ahora un aspecto noir: post-procesado por ambiente, lámparas con fuente visible, texturas en pisos y paredes, reflejos, utilería, ciudad con semáforos, cables y avisos, lluvia de noche y sonido ambiente. La jugabilidad no cambió: las mismas evidencias, personajes y colliders. **Todas las pruebas en verde** antes y después de cada cambio (41 EditMode, una de ellas nueva, y 6 PlayMode).

> **Nota sobre Git:** el `git pull` de `main` chocó (main local y `origin/main` tienen historias distintas: ~50 conflictos). No resolví nada a mano: guardé el intento en `git stash` ("restos-del-pull-fallido-main") y partí `dev-visual` de `dev-sesion4`, que tiene todo lo de `origin/main` más la luz horneada y la ciudad que no habían llegado a `main`. Puedes borrar ese stash con `git stash drop` cuando quieras.

### Qué se mejoró en cada escena

Capturas en `Docs/capturas/antes/` y `Docs/capturas/despues/` con los mismos nombres (mismo punto de vista, altura de los ojos).

| Escena | Antes → después | Qué cambió |
|---|---|---|
| **Menú principal** | `ui_00_menu` | Detrás del título se ve Valdora de noche con lluvia y la silueta de una persiana (antes, fondo plano). |
| **c1_s1 Apartamento 404** | `c1s1_pasillo`, `c1s1_sala`, `c1s1_estudio`, `c1s1_balcon`, `c1s1_ciudad` | Día nublado frío con lámparas cálidas. Piso de tablas con brillo, yeso, baldosa del pasillo con manchas. Aviso verde iluminado de "ESCALERAS" en el pasillo. Escena del crimen revuelta: libros y cojines en el piso, cuadro caído con vidrio roto, cajón abierto, papeles, taza rota. **Marcadores amarillos numerados** junto a las evidencias. Huella con dibujo de suela. Persianas, lámpara de pie encendida, libros individuales, cojines, cobija, microondas, plantas, cuadros, rayos de luz por las ventanas. |
| **c1_s2 Café La Esquina** | `c1s2_entrada`, `c1s2_barra`, `c1s2_trastienda` | **De noche, como dice el guion.** Pared de ladrillo, baldosa, lámparas colgantes sobre el salón, butacos tapizados, letrero de neón "CAFÉ LA ESQUINA", lluvia tras las vitrinas, murmullo de café. |
| **c1_s3 Portería y sótano** | `c1s3_porteria`, `c1s3_sotano`, `c1s3_parqueadero` | Fluorescente verdoso. Concreto manchado, tuberías y tubería contra incendios en el techo, tubos que parpadean, casilleros con rejillas, estanterías con cajas, archivadores, carros con cabina y llantas, más tubos en el parqueadero. |
| **c1_s4 / c2_s4 Comisaría** | `c1s4_oficina`, `c1s4_ventana`, `c1s4_interrogatorio` | Persianas con franjas de luz, **lámparas de banquero verdes** en los escritorios, lámpara única sobre la mesa de interrogatorio, archivadores y dispensador de agua. En el caso 2 cambia sola a noche (perfil de post-procesado incluido). |
| **c2_s1 Sala de servidores B** | `c2s1_pasillo`, `c2s1_sala`, `c2s1_racks` | Noche fría: frentes de racks con LEDs verdes, ámbar y azules que laten, bandejas de cables, tubos fluorescentes que parpadean, zumbido de ventiladores. |
| **c2_s2 Piso ejecutivo** | `c2s2_oficinas`, `c2s2_ventanal`, `c2s2_pasillo` | Ventanal limpio (el vidrio ya no "lava" la imagen), lluvia afuera, calle mojada, avisos de neón en la ciudad, lámparas colgantes, archivadores, cuadros. |
| **c2_s3 Caseta y parqueadero** | `c2s3_caseta`, `c2s3_parqueadero`, `c2s3_calle` | Letrero "VIGILANCIA" y aplique de luz en la caseta. Parqueadero con franjas en las columnas, conos, topes, números de puesto, letrero "SÓTANO -1", cuadrícula de tubos, tuberías, manchas de aceite, carro con cabina y luces, lluvia y calle mojada. |
| **Personajes** | `personaje_*` | Ropa sin los músculos del cuerpo base y con trama de tela (Carlos ya no parece sin camisa), piel satinada, ropa mate, zapatos y pelo con brillo, luz de relleno en la cara. |
| **UI** | `ui_*` | Carta y ficha del expediente como papel envejecido con sombra y sello "EVIDENCIA"; panel de ALEX como pantalla con líneas de barrido. |

**En todas las escenas:**
- **Render:** HDR, tone mapping ACES, bloom suave, viñeta, grano de película, contraste alto y saturación baja, con sombras frías y luces cálidas (split toning). SMAA. SSAO a media resolución. Sombras suaves con 4 cascadas a 35 m.
- **Tres perfiles:** día nublado (apartamento), fluorescente verdoso (portería y sótano, Comisaría en el caso 1) y noche (café, caso 2 y la Comisaría en el caso 2, que cambia sola).
- **Luz:** cada luz tiene su lámpara o tubo visible; la más fuerte de cada escena proyecta sombra suave; reflection probes por cuarto; polvo flotando bajo las lámparas; algunos fluorescentes parpadean; luz de relleno suave en la cara de cada personaje para que siempre se vean.
- **Materiales:** texturas generadas por código (no había texturas en ThirdParty): tablas de madera y baldosas con brillo, ladrillo, yeso, concreto manchado y con grietas, madera con vetas, tela, metal cepillado y lockers con óxido. Vidrio casi transparente con reflejo.
- **Detalle:** rodapiés, enchufes, interruptores, cuadros, tazas, libros y papeles en las mesas, plantas, manchas en los pisos de concreto, tuberías en sótanos y parqueaderos.
- **Ciudad:** semáforos en las esquinas, cables entre postes, avisos luminosos (encendidos de noche), calle mojada de noche y lluvia detrás de los ventanales.
- **Vida:** sonido ambiente por escena (ciudad, café, zumbido de fluorescentes, servidores, lluvia), cámara que se acerca y desenfoca el fondo al hablar, y balanceo suave al caminar (se apaga en **Opciones → Balanceo de cámara al caminar**).
- **UI:** la carta y la ficha del expediente son hojas de papel envejecido con sombra y sello; el panel de ALEX es una pantalla con líneas de barrido y borde con brillo.

### Rendimiento (FPS)

Máquina de prueba: **portátil con Intel Core i5-13420H y gráficos integrados Intel UHD** (gama media o algo menos en gráficos), a **1920×1080 en pantalla completa**, sin VSync.

**En el ejecutable** (`ElCaso404.exe -benchmark`: recorre las 7 escenas y gira la vista 360° durante 6 s en el punto de inicio; archivo `Docs/capturas/despues/benchmark_ejecutable.csv`):

| Escena | Primer build de la sesión (sin optimizar) | Final | Percentil 95 final |
|---|---|---|---|
| Apartamento 404 | 61 FPS | **94 FPS** | 12.5 ms |
| Café La Esquina | 59 FPS | **82 FPS** | 14.8 ms |
| Portería y sótano | 72 FPS | **96 FPS** | 14.4 ms |
| Comisaría | 68 FPS | **81 FPS** | 13.5 ms |
| Sala de servidores B | 66 FPS | **89 FPS** | 12.8 ms |
| Piso ejecutivo | 70 FPS | **95 FPS** | 14.0 ms |
| Caseta y parqueadero | 48 FPS | **70 FPS** | 15.8 ms |

Todas las escenas quedan sobre 60 FPS de promedio, y también el 5 % de frames más lentos (el peor es la caseta: 15.8 ms ≈ 63 FPS). El build anterior a la sesión no tenía el medidor, así que la comparación "antes/después" exacta es la del Editor:

**En el Editor** (`VisualAudit`, mismo método antes y después: 40 frames a 1920×1080 por vista; promedio de las vistas de cada escena). El Editor tiene sobrecosto y el portátil se calienta tras muchas corridas, así que sirve para comparar, no como cifra real:

| Escena | Antes | Después |
|---|---|---|
| Apartamento 404 | 19.3 ms (52 FPS) | 22.8 ms (44 FPS) |
| Café La Esquina | 21.5 ms (46 FPS) | 20.2 ms (49 FPS) |
| Portería y sótano | 19.0 ms (53 FPS) | 24.0 ms (42 FPS) |
| Comisaría | 16.8 ms (60 FPS) | 22.8 ms (44 FPS) |
| Sala de servidores B | 18.7 ms (54 FPS) | 21.4 ms (47 FPS) |
| Piso ejecutivo | 21.9 ms (46 FPS) | 25.1 ms (40 FPS) |
| Caseta y parqueadero | 20.5 ms (49 FPS) | 24.0 ms (42 FPS) |

Es decir, el look nuevo cuesta hasta unos 5 ms más por frame que el greybox en el Editor (el café incluso bajó), y el ejecutable lo absorbe. Lo que más costaba eran **las luces puntuales** y **la ciudad dibujada detrás de las paredes**; se resolvió con occlusion culling, una sola sombra puntual por escena, sin sombra direccional de noche y menos luces reales en parqueaderos y café (medido con **Caso404 → `VisualAudit.ProfileEffects`**, que apaga un efecto a la vez).

**Build:** `Builds/ElCaso404/ElCaso404.exe`, 179 MB (antes 135 MB: texturas, audio y variantes de shaders del post-procesado). Tarda unos 5 min la primera vez y menos de 1 min después.


### Qué recursos conseguir para el siguiente salto de realismo
1. **Personajes con ropa modelada y animaciones reales** (Mixamo: descarga gratuita con cuenta de Adobe). Es lo que más se nota hoy: los cuerpos base tienen la ropa pintada y algunos parecen sin camisa con luz cálida (Carlos).
2. **Texturas PBR fotográficas** (Poly Haven o ambientCG, CC0): madera, concreto, baldosa, ladrillo y tela de 1K o 2K. Se cambian en los materiales sin tocar código.
3. **Un HDRI de ciudad de noche y otro nublado** (Poly Haven) para el cielo y los reflejos.
4. **Muebles low-poly** (Kenney Furniture Kit, Quaternius Ultimate Furniture): sofás, sillas, estantes y electrodomésticos en vez de cajas.
5. **Sonidos grabados** (freesound.org, CC0): ambiente de café, lluvia, zumbido eléctrico, pasos.
6. **Fuentes** de la skill diseno-ui (Special Elite, IBM Plex Mono) para la carta y ALEX.

### Qué debes probar tú
1. **Jugar el caso 1 completo** en el ejecutable (`Builds/ElCaso404/ElCaso404.exe`) y revisar que todas las evidencias se vean y se puedan recoger. Las pruebas automáticas lo verifican, pero nadie lo ha jugado con mouse y teclado después de estos cambios.
2. **¿Muy oscuro?** El noir pide sombras, pero tu pantalla puede ser más oscura que la mía. Si cuesta ver, sube la exposición en `Art/PostProcess/VP_*.asset` (**Color Adjustments → Post Exposure**, +0.3) o en `VisualPass.Profile` y corre **Caso404 → Arte → Aplicar paso visual**.
3. **Balanceo de cámara**: camina un rato; si marea, apágalo en **Opciones → Balanceo de cámara al caminar**.
4. **Al hablar con un personaje** la cámara se acerca y el fondo se desenfoca: revisa que el texto del diálogo se lea bien.
5. **El café ahora es de noche** (el guion lo dice); si preferías el día nublado que pediste, es un cambio de una línea en `VisualPass.For`.
6. **Sonido ambiente**: revisa el volumen frente a los sonidos de la interfaz.
7. **La caseta y el café** son las escenas más pesadas: si en tu portátil bajan de 60 FPS, dime y quito la lluvia o reduzco luces ahí.
8. **Los PR** (enlaces abajo): fusiónalos en cualquier orden; el script verifica que juntos dan exactamente `dev-visual`.

### Pull requests (sin fusionar)
- José: https://github.com/Josecampoe/CriminalisticaGame/pull/8
- Majo: https://github.com/Josecampoe/CriminalisticaGame/pull/9
- Daniel: https://github.com/Josecampoe/CriminalisticaGame/pull/10


---

## Sesión 4 (19 de octubre de 2026)

> **Actualización de las 11:30 AM:** quedaron hechas también la **tarea 2 (espacios más grandes)** y la **tarea 3 (ciudad completa)**, con iluminación horneada. Ver la tabla.

**En resumen:** las **4 tareas están hechas**: personajes, espacios más grandes, ciudad y cartas de contexto. También se generaron el **build de Windows** y la **subida a GitHub** en ramas por integrante.

### Qué se hizo
| Tarea | Estado | Detalle |
|---|---|---|
| Subida a GitHub | Hecha | `tools/split_by_owner.py` reparte el trabajo en una rama por integrante, con commits pequeños firmados por cada uno y la fecha original. Verifica que fusionar las 3 ramas da exactamente el mismo proyecto. Ramas: `jose/base-juego`, `majo/docs-ui-datos`, `daniel/core-pruebas`; las de esta sesión son `jose/sesion4`, `majo/sesion4` y `daniel/sesion4`. |
| 1. Personajes | Hecha | Los 8 personajes usan modelos humanoides de Quaternius en vez de cápsulas, con ropa según el guion, peinado, barba, gorra o placa, y escala según edad. Tienen animaciones de reposo, hablar, sentado y sentado hablando, creadas por código porque el paquete no trae ninguna. Al hablar, el personaje mira al jugador. Se conservan los colliders, `CharacterNpc` y el resaltado. Los sentados tienen su propia silla. |
| 4. Cartas de contexto | Hecha | Antes de c1_s1 (**Nueva partida**) y antes de c2_s1 (**Comenzar «Protocolo 404»**): fondo oscuro, hoja de expediente, sello CONFIDENCIAL, máquina de escribir (un clic muestra todo) y el botón "Comenzar investigación". Usan los textos exactos que me diste. La Comisaria Lozano quedó agregada al guion (§1 y §6). |
| Build de Windows | Hecho | `Builds/ElCaso404/ElCaso404.exe` (135 MB; la primera compilación tras hornear la luz tarda unos 30 min por las variantes de shaders). Se regenera con **Caso404 → Build de Windows**. |
| Capturas | Hecha | En `Docs/capturas/`: la carta del caso 1 (escribiéndose y completa), los personajes, cada escena (`c1s1_*` … `c2s3_*`), la ciudad desde el balcón y los ventanales de noche. La carta del caso 2 usa la misma vista que la del caso 1 (solo cambia el texto) y no tiene captura propia. |
| 2. Espacios más grandes | Hecha | **Apartamento 404:** pasillo del edificio con puertas 401–405, ascensor y ventana; sala-comedor, cocina abierta, estudio con el escritorio de Elena, pasillo interno, baño, habitación y balcón de 8 m. Las 7 evidencias quedaron repartidas por pasillo, entrada, sala, balcón y estudio. **Café:** salón más grande, trastienda con el monitor de la cámara y baño. **Sala de servidores:** 4 filas de racks y pasillo con ventanal. La portería y la Comisaría ganaron ventanas a la calle. El objetivo del HUD ahora pide recorrer el lugar al llegar. |
| 3. Ciudad alrededor | Hecha | Valdora por cuadras alrededor de 7 escenas: andenes, calles con líneas, edificios de Kenney (detallados cerca y mallas simples lejos, en un prefab compartido), faroles, árboles, carros estacionados, montañas, cielo nublado (caso 1) o nocturno con ventanas encendidas (caso 2) y niebla en el borde. Ya no hay piso infinito. Se ve el edificio donde ocurre cada escena, con la calle a la altura correcta (12 m bajo el balcón, 36 m bajo el piso 12). Hay colliders invisibles en el balcón y los ventanales. La Comisaría queda de día en el caso 1 y de noche en el caso 2. **Iluminación horneada** (rebotes y oclusión ambiental) con **Caso404 → Hornear iluminación**. |

Pruebas: **40 EditMode y 6 PlayMode en verde** antes y después de los cambios. Las PlayMode ahora también pasan por las dos cartas.

### Qué debes revisar tú
1. **Cómo se ven los personajes.** Los cuerpos base del paquete gratuito son muy musculosos y la ropa queda ajustada, como un enterizo, porque está pintada sobre la piel. Si no te convence, la alternativa es un paquete con ropa modelada.
2. **Las poses en el juego.** En Play los personajes deben estar en reposo; los sentados, en su silla; y al hablarles, gesticular y mirarte. Revisa sobre todo la Comisaría y el café.
3. **La ciudad y los espacios**: recorre el apartamento completo y asómate al balcón, a las vitrinas del café y al ventanal del piso 12. Revisa que no haya bordes visibles, caídas ni objetos difíciles de alcanzar. La sala de servidores es oscura a propósito ("frío" en el guion); dime si quieres más luz.
4. **Las dos cartas**: que el texto se lea bien en tu pantalla y que el clic complete el texto.
5. **El vestuario que elegí** sin que lo pidieras: Laura con uniforme de enfermera, Sebastián con buzo con capota y Elena con chaqueta y jeans (`DECISIONES.md`, N2).
6. **Los PR en GitHub** (enlaces abajo). Te recomiendo fusionar primero las ramas de la subida inicial y después las de la sesión 4: las de la sesión 4 continúan a las anteriores y no tienen conflictos.

### Cómo probar
- **En el Editor:** **Caso404 → Jugar desde el menú** (Ctrl+Shift+P) → **Nueva partida**. Debe aparecer la carta.
- **En el ejecutable:** abre `Builds/ElCaso404/ElCaso404.exe`.

### Para la próxima sesión
- **"Ajustemos lo que encontré jugando"**: distancias, luz, objetos difíciles de alcanzar, textos.
- Si quieres personajes con ropa modelada en 3D, habría que conseguir un paquete con ropa (el gratuito solo trae cuerpos base).
- **Importante al regenerar escenas:** después de **Caso404 → Construir todo**, ejecuta **Caso404 → Hornear iluminación (todas las escenas)**.
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
