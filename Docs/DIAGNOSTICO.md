# Diagnóstico completo — El Caso 404 (19 de noviembre de 2026)

> Revisión hecha a partir del código, los datos, las escenas, las pruebas, las capturas, los documentos (`GUION.md`, `DECISIONES.md`, `REPORTE-SESION.md`, `ARQUITECTURA.md`, `AUDITORIA-VISUAL.md`) y el estado de Git de la rama `dev-jugabilidad`.
> No se jugó el juego con teclado y mouse durante esta revisión. Lo visual se evaluó con las capturas guardadas.

---

## 1. Resumen en una línea

**El juego está completo en contenido y lógica.** Los dos casos, las 7 escenas y los 7 finales se pueden jugar de principio a fin, la arquitectura es sólida y hay pruebas automáticas. **Lo más débil es la sensación de juego terminado**: los escenarios se ven armados con cajas, los interrogatorios son cortos y repetitivos, no hay tutorial y casi 500 archivos están sin commit.

## 2. Calificación por área

| Área | Nota (1–10) | En una frase |
|---|---|---|
| Historia y diseño del caso | **9** | Guion muy completo, con IDs, mentiras refutables y diseño justo verificado. |
| Lógica de juego y arquitectura | **8.5** | Data-driven, servicios puros, eventos y finales deterministas. Falta una escena Bootstrap persistente. |
| Pruebas automáticas | **8** | 47 EditMode y 14 PlayMode en verde (9 nov). Recorren los finales desde el menú. |
| Alcance cumplido | **9** | 2 casos, 7 escenas, 8 personajes + ALEX, 7 finales y todos los sistemas pedidos. |
| ALEX (IA) | **7.5** | Conversa y manipula vía servidor público (Cloudflare + Gemini gratis), con modo sin conexión. Depende del límite gratuito. |
| Interrogatorios con personajes | **5** | Desde J22 son solo frases fijas: pocas respuestas por personaje y se repiten rápido. |
| Interfaz (UI) | **7.5** | Estilo noir coherente, atajos y laboratorio vistoso. Hay detalles de pulido. |
| Escenarios 3D | **5.5** | Buena luz y post-procesado, pero muebles y objetos son primitivas (cajas) con texturas generadas. |
| Personajes 3D | **6** | Ropa modelada low-poly (Quaternius), pero con caras planas y un estilo que choca con la luz realista. |
| Audio | **5** | Solo música generada y clics. Se quitaron los pasos y el ambiente; no hay voces ni sonidos de objetos. |
| Primera experiencia (onboarding) | **4** | No hay tutorial. El jugador depende de la línea de objetivo y de la lista de teclas en Opciones. |
| Estado del repositorio | **3** | ~500 archivos sin commit (J16–J28), muchas ramas por persona y `main` local con historia distinta. |
| Rendimiento | **8.5** | Más de 60 FPS en Intel UHD en las 7 escenas, según la última medición (antes de J24–J28). |

**Promedio general: ~6.8 / 10.** Funciona y está bien construido; ahora necesita pulido de presentación y profundidad en los interrogatorios.

---

## 3. Lo que está bien (no tocar sin razón)

1. **La regla de oro se cumple.** La verdad del caso está en ScriptableObjects generados desde el guion. `EndingEvaluator` es determinista y la IA no decide nada.
2. **La arquitectura es fácil de sustentar.** `GameContext` funciona como raíz de composición, los servicios son C# puro, `GameState` es serializable y no se usa `FindObjectOfType` en `Scripts/`. Patrones: Observer, Strategy (`IAiClient`) y Data-driven.
3. **Las pruebas recorren el juego como un jugador**, incluidos los caminos mínimos del GUION §5 y el alcance de cada objeto interactivo a 0.75, 1.6 y 2.3 m.
4. **La IA es segura.** La clave vive como secreto del Worker y las respuestas se validan con `AiReplyValidator`; si fallan, se usan las frases del guion.
5. **Las mecánicas especiales están implementadas:** ERROR 404, Cuestionar, contraseña LUCERO, laboratorio externo, copia espejo, glitch de ALEX según su confianza y Comisaría compartida.
6. **El rendimiento está cuidado:** occlusion culling, luz horneada y una sola sombra puntual por escena.

---

## 4. Problemas encontrados, de mayor a menor impacto

### 4.1 Críticos (antes de cualquier otra cosa)

| # | Problema | Evidencia | Riesgo |
|---|---|---|---|
| C1 | **~500 archivos sin commit.** Incluye los personajes Quaternius modulares (J24), los carros (J25), la música en rotación (J27), el laboratorio 3D (J28), el diálogo sin IA (J22) y el borrado de las mallas y texturas viejas. | `git status`: 184 modificados, 150 borrados y 168 nuevos. | Si se pierde el disco o alguien hace `checkout`, se pierden 2 sesiones de trabajo. |
| C2 | **Contradicción entre CLAUDE.md y el juego.** CLAUDE.md dice "interroga personajes controlados por IA", pero J22 dejó a los personajes con frases fijas y solo ALEX usa IA. | `DECISIONES.md` J22 y `DialogueService.cs` ("sin IA"). | Hay que decidir la versión oficial antes de la sustentación. Hoy el documento del proyecto y el juego dicen cosas distintas. |
| C3 | **Nadie ha confirmado una partida completa con teclado y mouse** después de los cambios grandes de arte y UI (J17–J28). | Todos los reportes lo marcan como pendiente. | Puede haber objetos tapados, textos cortados o poses rotas que las pruebas no ven. |
| C4 | **El build de Windows está desactualizado.** La última medición es de la sesión visual (29 oct) y no incluye J16–J28. | `Builds/ElCaso404`, `REPORTE-SESION.md`. | Lo que se entregue o muestre no será lo último. |

### 4.2 Altos (lo que más se nota al jugar)

| # | Problema | Detalle |
|---|---|---|
| A1 | **Interrogatorios pobres.** | Cada personaje tiene unas 4 a 6 respuestas fijas: crimen, otra persona, presionar, calma, temas extra y presentar. Al segundo intento todo se repite y no hay reacción distinta por cada evidencia ni por el estado de la investigación. Es el corazón del género y hoy es lo más flojo. |
| A2 | **Escenarios hechos con cajas.** | El sofá, las mesas, los estantes, la nevera y los casilleros son primitivas (`CreatePrimitive`) con texturas generadas por código. La luz y el post-procesado son buenos, pero la geometría delata el greybox (ver `capturas/despues/c1s1_sala.png`). Solo se usan modelos de terceros para la ciudad (Kenney), los personajes y los carros (Quaternius). |
| A3 | **Choque de estilos.** | Los personajes son low-poly de colores planos con caras muy simples, mientras que la luz y las texturas buscan realismo noir. En primer plano (diálogo) se nota mucho. Hay que elegir una dirección: todo estilizado o todo más realista. |
| A4 | **No hay tutorial ni introducción a las mecánicas.** | Las mecánicas propias del juego (Cuestionar a ALEX, presentar evidencia, hipótesis, ERROR 404, laboratorio externo) no se enseñan. Un profesor o jugador nuevo puede perderse en los primeros 5 minutos. |
| A5 | **Audio pobre.** | En J27 se quitaron los pasos y el ambiente. Hoy no suena nada al caminar, al recoger una evidencia en el mundo, al abrir una puerta ni en el entorno (lluvia, tráfico, fluorescentes). No hay voces, ni siquiera murmullos. |

### 4.3 Medios (pulido)

| # | Problema | Detalle |
|---|---|---|
| M1 | **Detalles del laboratorio.** | En "Escritorio vacío" las fotos de la pared flotan como cuadros blancos sin pared detrás. El texto "Arrastra para girar" se monta sobre la esquina del marco. |
| M2 | **La etiqueta de emoción no cuadra.** | En el diálogo aparece "TRISTE" en respuestas neutras. Conviene revisar la emoción asignada a cada frase o mostrarla solo cuando aporta. |
| M3 | **Los escenarios son oscuros.** | El apartamento y el sótano se ven muy oscuros en pantallas de portátil (ya estaba anotado). Falta un control de brillo o gamma en Opciones. |
| M4 | **Controles básicos ausentes.** | No se puede reasignar teclas ni caminar más rápido; no hay volumen separado de música y efectos (verificar) ni un ajuste de tamaño de texto. |
| M5 | **No hay escena Bootstrap persistente.** | Cada escena crea sus servicios y el estado pasa por el archivo de guardado. Funciona, pero `MusicPlayer` y otros sistemas globales viven en un lugar improvisado. |
| M6 | **La UI se genera solo por código.** | `UIFactory` y las vistas suman ~4.700 líneas y no hay prefabs de UI. Cada ajuste visual exige tocar código; un compañero no puede retocar la UI desde el Inspector. |
| M7 | **Las escenas se generan solo por código.** | Hay ~7.100 líneas de builders de Editor. Si alguien edita una escena a mano, se pierde al correr "Construir todo". Hay que documentarlo bien para el equipo. |
| M8 | **ALEX depende del plan gratuito de Gemini.** | Con varios jugadores, o en una demo en clase, puede llegar al límite. El respaldo sin conexión existe, pero ALEX se vuelve más limitado. |

### 4.4 Bajos (limpieza)

| # | Problema | Detalle |
|---|---|---|
| B1 | **128 MB de un paquete que ya no se usa.** | `Art/ThirdParty/Universal Base Characters[Standard]` no tiene referencias en `Scenes/`, `Prefabs/` ni en los scripts tras J24. Hay que confirmarlo en Unity antes de borrar. |
| B2 | **Demasiadas ramas.** | Hay 25 ramas locales (`jose/*`, `majo/*`, `daniel/*`, `dev-*`) y PR viejos sin cerrar (#11, #12). Un `main` limpio y actualizado facilitaría la entrega. |
| B3 | **Finales de línea mezclados.** | Git avisa LF→CRLF en varios `.cs`. Conviene un `.gitattributes`. |
| B4 | **Reportes desactualizados.** | Los reportes citan conteos de pruebas viejos (37, 40, 45). Conviene un único estado actual. |

---

## 5. Cuánto se puede mejorar (estimación)

| Mejora | Impacto en la experiencia | Esfuerzo | Nota esperada después |
|---|---|---|---|
| Commit y orden de Git (C1, B2, B3) | Ninguno visible, pero evita un desastre | Bajo | Repositorio: 3 → 8 |
| Interrogatorios más ricos (A1) | **Muy alto** | Medio | Interrogatorios: 5 → 8 |
| Tutorial guiado en la primera escena (A4) | Alto | Bajo–medio | Onboarding: 4 → 8 |
| Audio de pasos, objetos y ambiente suave (A5) | Alto | Bajo | Audio: 5 → 7.5 |
| Muebles y utilería con modelos CC0 (A2) | **Muy alto** visualmente | Medio–alto | Escenarios: 5.5 → 7.5 |
| Unificar el estilo de personajes y escenarios (A3) | Alto | Medio | Personajes: 6 → 7.5 |
| Pulido de UI, brillo y opciones (M1–M4) | Medio | Bajo | UI: 7.5 → 8.5 |
| Partida completa jugada y build nuevo (C3, C4) | Alto (confianza para la entrega) | Bajo | — |

**Con estas mejoras el juego puede pasar de ~6.8 a ~8 / 10 sin ampliar el alcance**: los mismos 2 casos, 7 escenas, 8 personajes y 7 finales.

---

## 6. Restricciones que cualquier mejora debe respetar

- **No ampliar el alcance**: 2 casos, 7 escenas, 8 personajes + ALEX y 7 finales. Nada de casos, escenas ni personajes nuevos sin aprobación.
- **`Docs/GUION.md` es la fuente de verdad.** Las frases nuevas de los personajes solo pueden expresar hechos que ya están en el guion (`f_*`, `lie_*` y secretos). No se inventan pistas ni se cambia quién sabe qué.
- **La regla de oro:** la verdad vive en datos y los finales son deterministas.
- Código en inglés, textos del jugador en español. No usar `FindObjectOfType`; referencias por `[SerializeField]`.
- **No editar `.cs` con PowerShell** (`Set-Content` daña las tildes).
- Arte de terceros solo con licencia CC0 (Quaternius, Kenney, Poly Haven, ambientCG). Sin luces que parpadean. El laboratorio va en azul claro.
- El equipo de prueba tiene gráficos Intel UHD: mantener más de 60 FPS.
- Las pruebas (47 EditMode y 14 PlayMode) deben seguir en verde después de cada cambio.
- Antes de un cambio grande: proponer un plan corto y esperar el visto bueno.

---

## 7. Prompt listo para pedir el plan de mejora

> Copia esto en una sesión nueva de Claude Code dentro del proyecto:

```
Lee Docs/DIAGNOSTICO.md, CLAUDE.md y Docs/GUION.md. Quiero subir la calidad de
El Caso 404 de ~6.8 a ~8/10 SIN ampliar el alcance (mismos 2 casos, 7 escenas,
8 personajes + ALEX, 7 finales) y respetando la regla de oro (la verdad vive en
datos; finales deterministas).

Antes de tocar nada:
1. Verifica el estado real (git status, compilación en Unity vía MCP for Unity,
   pruebas EditMode y PlayMode) y dime si algo del diagnóstico ya no aplica.
2. Propón un plan por fases, cada una pequeña y verificable, en este orden
   de prioridad:
   - Fase 0: asegurar el trabajo sin commit (J16–J28) en commits ordenados por
     sistema en una rama nueva, .gitattributes, y confirmar si "Universal Base
     Characters" se puede borrar.
   - Fase 1: decidir conmigo si los personajes vuelven a usar IA o se quedan
     con diálogo fijo, y actualizar CLAUDE.md para que coincida.
   - Fase 2: interrogatorios más ricos: respuestas por evidencia presentada,
     reacciones según lo ya descubierto y frases que no se repitan, todo
     derivado de los hechos f_/lie_ del GUION, sin inventar pistas.
   - Fase 3: tutorial breve integrado en c1_s1 (moverse, inspeccionar,
     analizar, Cuestionar a ALEX, presentar evidencia, hipótesis).
   - Fase 4: audio: pasos, recoger evidencia, puertas y ambiente suave por
     escena, sin ruido molesto (CC0 o generado).
   - Fase 5: reemplazar muebles y utilería de primitivas por modelos CC0
     (Kenney Furniture Kit, Quaternius) y unificar el estilo con los personajes.
   - Fase 6: pulido de UI (fotos flotantes del laboratorio, texto encimado,
     etiqueta de emoción), brillo/gamma y volumen separado en Opciones.
   - Fase 7: build nuevo, medición de FPS y checklist de partida completa con
     teclado y mouse para los 7 finales.
3. Para cada fase: archivos que tocarás, riesgos, cómo lo verificarás y qué
   debo revisar yo en el Inspector o jugando.

Explícame el porqué de cada decisión (lo voy a sustentar ante el profesor) y
espera mi visto bueno antes de empezar cada fase.
```
