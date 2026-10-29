# Auditoría visual — El Caso 404 (29 de octubre de 2026)

Capturas en `Docs/capturas/antes/`: 23 vistas a la altura de los ojos (1.65 m) en las 7 escenas, 16 primeros planos de personajes y tiempos de frame (`tiempos_frame.csv`). Las pantallas de UI son las de la sesión 2 (`Logs/ui_*.png`), porque no han cambiado desde entonces.

**Herramienta:** `Caso404 → Auditoría visual (capturas y tiempos)` (`VisualAudit.cs`). Las mismas vistas sirven para el "después".

## Problemas, de mayor a menor impacto (este es el plan de trabajo)

| # | Área | Problema | Dónde se nota más |
|---|---|---|---|
| 1 | **Iluminación y render** | No hay post-procesado: sin tone mapping, la luz de las lámparas se "quema" en manchas blancas en el techo y el resto queda plano. No hay viñeta, grano ni ajuste de color: la imagen no se siente noir. La cámara del jugador no tiene activado el post-procesado ni antialiasing (bordes en escalera). El color grading está en LDR. | Todas. Sala del apartamento, barra del café, oficinas del piso ejecutivo. |
| 2 | **Iluminación** | Las lámparas son luces sin objeto: brilla el techo pero no hay bombillo, lámpara ni tubo. Ninguna luz puntual proyecta sombras: los muebles "flotan". Poco contraste entre luz cálida y fría. Sin reflection probes: el metal y el vidrio reflejan el cielo genérico. | Sótano (pasillo uniforme), parqueaderos (casi negros), Comisaría. |
| 3 | **Materiales** | Todos los materiales son colores planos, sin textura ni relieve: paredes de "cartón", piso de un solo tono, madera sin vetas, metal mate. | Todas; sobre todo pisos y paredes grandes. |
| 4 | **Vidrios** | El vidrio es gris claro con 22 % de opacidad y sin reflejos: de noche "lava" la imagen (velo gris sobre el ventanal) y no parece vidrio. | Piso ejecutivo (ventanal y oficinas), café, balcón. |
| 5 | **Espacios vacíos y utilería** | Pocas cosas en las superficies; sin rodapiés, enchufes, interruptores, cuadros, plantas ni cables. El desorden de la escena del crimen se reduce a papeles cuadrados y una silla volcada. | Apartamento (sala y estudio), Comisaría, caseta. |
| 6 | **Escala y proporciones** | Las proporciones generales están bien (techos de 2.7 m, puertas de 2.1 m, mesas de 0.74 m). Los taburetes del café son cilindros sin asiento y el pasillo del sótano se ve angosto y sin detalle. | Café, sótano. |
| 7 | **Ciudad exterior** | La ciudad existe (sesión 4), pero de día los edificios cercanos se ven planos y de noche faltan ventanas encendidas cercanas, avisos y reflejos en el piso. | Balcón, ventanal del piso 12. |
| 8 | **Personajes** | Ropa pintada sobre el cuerpo; el pelo atraviesa la cara en algunas poses; la piel y la ropa tienen el mismo brillo. En reposo no respiran. | Primeros planos (Valeria, Laura). |
| 9 | **Vida y ambiente** | Silencio total (solo sonidos de UI), sin polvo en la luz, sin parpadeo de fluorescentes, cámara rígida al caminar. | Todas. |
| 10 | **UI** | Funcional, pero los paneles son rectángulos planos; el expediente y la carta no parecen papel. | Expediente, carta, panel de ALEX. |

## Rendimiento antes (render a 1920×1080 dentro del Editor)

Medido con `VisualAudit`: 40 frames por vista, esperando a la GPU al final. En el Editor hay sobrecosto, así que el ejecutable rinde más; sirve para comparar antes y después en la misma máquina.

| Escena | ms por frame (mín–máx de las vistas) |
|---|---|
| Apartamento 404 | 17.4 – 25.6 |
| Café La Esquina | 20.6 – 22.8 |
| Portería y sótano | 17.7 – 20.6 |
| Comisaría | 16.1 – 17.5 |
| Sala de servidores B | 18.3 – 19.0 |
| Piso ejecutivo | 19.6 – 25.9 |
| Caseta y parqueadero | 18.7 – 23.0 |

## Recursos disponibles

En `Art/ThirdParty/` solo están **Kenney City Kit Commercial** (edificios) y **Universal Base Characters** (Quaternius, cuerpos sin ropa ni animaciones). **No hay** carpetas de texturas, HDRI ni Mixamo, así que las texturas se generan por código y el cielo sigue siendo procedural.
