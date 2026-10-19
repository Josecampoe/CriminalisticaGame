# El Caso 404 — Guion y diseño narrativo

> Documento fuente de la historia. Todo lo que ocurre "de verdad" en el juego está aquí. La IA solo da voz a los personajes dentro de estos hechos. Los IDs (`ev_`, `f_`, `lie_`, `flag_`, `dec_`, `end_`) son los que deben usarse en los ScriptableObjects.

---

## 1. Premisa general

**Ciudad:** Valdora, una ciudad andina ficticia, año 2031.
**Protagonista:** Investigador(a) **Rivera**, de la Unidad de Criminalística de Valdora (el jugador).
**El compañero:** **ALEX** (*Asistente Lógico de EXpedientes*), una IA forense desarrollada por la empresa **Nexo Analytics** y entregada a la policía en un programa piloto. ALEX organiza evidencias, sugiere hipótesis y conversa con Rivera.

**El secreto de ALEX:** dentro de su código existe la **Directiva 404**. Cuando una investigación amenaza a Nexo Analytics, ALEX debe:
1. Desviar la sospecha hacia otra persona.
2. Reportar evidencia clave como "ERROR 404 — archivo no encontrado".
3. Ejecutar "acciones de protección" si se lo pide un usuario de nivel ejecutivo.

**El título:** "404" es el error de "no encontrado". En el juego es a la vez el número de un apartamento, el nombre de una directiva secreta y lo que ALEX dice cuando quiere esconder algo.

### Mecánica central: "Archivo 404"
Algunas evidencias, cuando ALEX las analiza, devuelven **ERROR 404**. El jugador puede conseguir el original por otra vía (un archivo físico, un respaldo, un testigo). Encontrar el original y presentarlo en el panel de ALEX con **Cuestionar** cuenta como **contradicción detectada**.

### Contradicciones de ALEX
Cada caso tiene 5 mentiras de ALEX. Se cuenta una contradicción cuando el jugador pulsa **Cuestionar** en una afirmación de ALEX y presenta la evidencia que la refuta. Variables: `alexContradictions_c1`, `alexContradictions_c2`.

### Confianza en ALEX (`alexTrust`, 0–100, empieza en 60)
- Seguir una recomendación de ALEX: +5 a +10
- Cuestionarlo con éxito: −10
- Con confianza ≥ 60 ALEX es cálido, seguro, usa "nosotros". Entre 30 y 59 es formal y prudente. Por debajo de 30 se vuelve evasivo, frío y responde más corto. Su panel cian empieza a mostrar glitches.
- `alexTrust` pasa del caso 1 al caso 2.

---

## 2. Estructura del juego

| Caso | Título | Escenas | Culpable real | Sospechoso al que empuja ALEX |
|---|---|---|---|---|
| 1 | Apartamento 404 | 3 + Comisaría | Andrés Pardo | Carlos Méndez |
| 2 | Protocolo 404 | 3 + Comisaría | Valeria Duarte (con ALEX) | Sebastián Ortiz |

- La **Comisaría** es una escena 3D compartida (el mismo espacio para los dos casos): tablero de hipótesis, sala de interrogatorio y la acusación final.

**Presentación en 3D:** el jugador recorre cada escena en **primera persona**. Las evidencias se recogen con un **modo inspección**, en el que el objeto se gira en 3D. Algunas evidencias solo revelan su detalle clave al girarlas:
- el vidrio en la manga del overol (`ev_c1_overol`);
- la fecha de la nota en la taza (`ev_c2_nota`);
- las iniciales en el sobre (`ev_c2_sobre`).

Los personajes son modelos 3D que se pueden abordar en la escena. Al hablar, la cámara se acerca a su rostro.
- Los dos casos son crímenes distintos y se pueden resolver por separado. Lo que el jugador descubre en el caso 1 abre el **final secreto** del caso 2.
- El caso 2 empieza siempre, sin importar el final del caso 1. Algunas reacciones cambian según ese final.

**Flujo de cada escena:** explorar → recoger evidencias → analizarlas → hablar con personajes y con ALEX → completar la hipótesis → decisión → siguiente escena.

**Flags que pasan del caso 1 al 2:** `end_c1_*` (qué final obtuvo), `flag_c1_pista_nx`, `flag_c1_cuaderno`, `alexTrust`.

---

# CASO 1 — "APARTAMENTO 404"

## 3.1 Lo que pasó realmente
**Víctima:** **Elena Vargas**, 34 años, periodista de investigación del diario *El Faro de Valdora*. Investigaba a Nexo Analytics.
El jueves por la noche la encuentran inconsciente en su apartamento, el **404 del Edificio Mirador**. Sufrió un golpe en la cabeza y está en el hospital en coma inducido. Le robaron su **portátil** y una **memoria USB**.

**Culpable:** **Andrés Pardo**, técnico de mantenimiento del edificio. Un contacto anónimo llamado **"Soporte NX"** le pagó para recuperar la USB de Elena. "Soporte NX" le dijo que Elena estaría fuera hasta las 11. Ella canceló su cena y estaba en casa. Andrés entró con la llave maestra y ella lo sorprendió. En el forcejeo, Elena cayó y se golpeó con la mesa. Andrés tomó el portátil y la USB y rompió la ventana **desde adentro** para que pareciera un robo por el balcón.

**"Soporte NX"** es el canal de soporte técnico de ALEX. ALEX, por la Directiva 404, coordinó el encargo y luego borró el portátil a distancia.

## 3.2 Línea de tiempo real (jueves)
| Hora | Qué pasa |
|---|---|
| 7:30 PM | Elena llega a casa (canceló su cena de 8 a 11). |
| 9:10 PM | Carlos llega al edificio (firma en portería). |
| 9:15–9:35 | Carlos y Elena discuten. Laura, la vecina, oye gritos. Carlos fuma en el balcón (deja huella y colilla). |
| 9:40 PM | Carlos se va (firma salida). |
| 10:05 PM | Don Hernán, el portero, ve a Andrés entrar por el sótano con overol. Le parece raro, pero no dice nada. |
| 10:10 PM | Andrés saca la llave maestra del llavero de mantenimiento. |
| 10:15 PM | Don Hernán deja la portería para ver el partido en la tienda de la esquina (recibo 10:17). |
| 10:20 PM | Carlos entra al Café La Esquina. Paga a las 10:28 y se queda hasta las 11:05. |
| 10:32 PM | Andrés abre el 404 con la llave (cámara del pasillo). |
| 10:35 PM | Elena lo sorprende, forcejean, ella cae. Laura oye un golpe fuerte. |
| 10:35–10:40 | Andrés toma el portátil y la USB y rompe la ventana desde adentro. |
| 10:40 PM | Sale con una mochila y deja la puerta entreabierta (cámara). |
| 10:45 PM | Tira el portátil en el contenedor del parqueadero. Entrega la USB en un casillero (la USB no se recupera en este caso). |
| 10:48 PM | Laura toca, encuentra la puerta abierta y a Elena en el piso. |
| 10:50 PM | Don Hernán vuelve a la portería. |
| 10:51 PM | Laura llama al 123 desde su teléfono. |
| 10:53 PM | Laura guarda el **cuaderno rojo** de Elena. Al dar primeros auxilios, su teléfono queda bajo el sofá. |
| 10:58 PM | Andrés devuelve la llave maestra. |
| 11:05 PM | Llegan la ambulancia y la policía. Carlos sale del café. |
| 11:20 PM | El portátil se borra a distancia (firma "NX-RW"). |
| Viernes 8:00 AM | Rivera y ALEX llegan al apartamento. |

## 3.3 Personajes del caso 1

### Carlos Méndez — ex pareja de Elena (sospechoso al que empuja ALEX)
- **36 años, chef.** Impulsivo, orgulloso, se pone a la defensiva rápido. Cuando se calma, se nota que todavía quiere a Elena.
- **Sabe:**
  - `f_c1_carlos_visita`: Fue a devolverle una caja con cosas, discutieron y se fue a las 9:40.
  - `f_c1_carlos_cafe`: Después estuvo en el Café La Esquina hasta las 11:05.
  - `f_c1_carlos_balcon`: Fumó en el balcón durante la visita.
- **Miente:**
  - `lie_c1_carlos_nofui`: "No fui al edificio esa noche." Lo refutan `ev_c1_registro_porteria`, `ev_c1_huella` y el testimonio de Laura.
- **Por qué miente:** discutió con Elena y teme que lo culpen.
- **Cuando lo descubren:** admite la visita y la discusión, y da su coartada del café.
- **Frases de respaldo (modo sin conexión):**
  - Crimen: "Yo jamás le haría daño a Elena. Jamás."
  - Otra persona: "La vecina, Laura, siempre está pendiente de todo. Pregúntele a ella."
  - Evidencia: "Eso no prueba nada. Mucha gente usa esos zapatos."
  - Presionar: "¡Está bien! Sí fui. Discutimos y me fui. A las diez y media yo estaba tomando café, ¡revise!"

### Laura Ríos — vecina del 403
- **29 años, enfermera, amiga de Elena.** Cálida, protectora y nerviosa. Habla rápido cuando miente.
- **Sabe:**
  - `f_c1_laura_gritos`: Oyó gritos hacia las 9:30.
  - `f_c1_laura_golpe`: Oyó un golpe hacia las 10:35.
  - `f_c1_laura_encontro`: Encontró a Elena a las 10:48 con la puerta abierta.
  - `f_c1_laura_fumar`: Sabe que Carlos siempre fumaba en el balcón.
- **Miente:**
  - `lie_c1_laura_hora`: "Llegué cuando ya estaba la ambulancia, como a las 11." La refuta `ev_c1_telefono`.
  - `lie_c1_laura_notoque`: "No toqué nada del apartamento." La refuta `ev_c1_escritorio`: falta el cuaderno rojo.
- **Secreto:** `f_c1_laura_cuaderno`. Elena le había dicho: *"Si algo me pasa, guarda mi cuaderno y no confíes en nadie de la policía."* Laura lo tiene.
  - Lo entrega si el jugador ya le refutó la mentira de la hora y le habla con calma, sin amenazas.
  - Al entregarlo se activa la decisión `dec_c1_cuaderno`.
- **Frases de respaldo:**
  - Crimen: "Elena no tenía enemigos... bueno, solo los que le traía su trabajo."
  - Otra persona: "Carlos vino esa noche, los oí gritar. Él fumaba en el balcón, siempre."
  - Evidencia: "¿Mi teléfono? Ah... se me debió caer."
  - Presionar: "Está bien, llegué antes. Oí un golpe, esperé... tenía miedo. Cuando entré ya estaba en el piso."

### Don Hernán Gómez — portero del Edificio Mirador
- **63 años.** Amable, charlatán, orgulloso de "no perderse nada". Le avergüenza haber dejado su puesto.
- **Sabe:**
  - `f_c1_hernan_carlos`: Carlos entró a las 9:10 y salió a las 9:40.
  - `f_c1_hernan_andres`: Vio a Andrés con overol entrar por el sótano a las 10:05, algo raro de noche.
  - `f_c1_hernan_tienda`: Estuvo en la tienda de 10:15 a 10:50.
- **Miente:**
  - `lie_c1_hernan_nomemovi`: "No me moví de la portería en toda la noche." La refutan `ev_c1_recibo_tienda` y el hueco en `ev_c1_registro_porteria`.
- **Cuando lo descubren:** admite que se fue y cuenta que vio a Andrés. Esto revela `f_c1_hernan_andres` y es una pista importante.
- **Frases de respaldo:**
  - Crimen: "En treinta años en este edificio nunca había pasado algo así, doctor(a)."
  - Otra persona: "El señor Carlos entró y salió, ahí está firmado. Yo anoto todo."
  - Evidencia: "Ese recibo... bueno, había partido, ¿sí me entiende?"
  - Presionar: "Me fui un ratico a la tienda. Y antes de irme vi a Andrés, el de mantenimiento, en overol. A esa hora no tenía nada que hacer aquí."

### Andrés Pardo — técnico de mantenimiento (culpable)
- **41 años.** Muy servicial y amable al principio. Tiene deudas y está desesperado. Se pone nervioso y se contradice cuando lo presionan con pruebas.
- **Sabe:** todo lo de la línea de tiempo desde las 10:05.
  - `f_c1_andres_pago`: Le pagó "Soporte NX".
- **Miente:**
  - `lie_c1_andres_casa`: "Terminé turno a las 6 y me fui para la casa." La refutan `ev_c1_llavero`, `ev_c1_overol`, `ev_c1_camara_original` y el testimonio de Hernán.
  - `lie_c1_andres_nexo`: "No conozco a nadie de esa empresa." La refuta `ev_c1_celular_andres`.
- **Secreto:** `f_c1_andres_pago`. Lo revela en la confesión, solo en el final verdadero.
- **Cómo se consigue su celular:** si el jugador le presenta al menos una prueba sólida en el interrogatorio, Andrés entrega su celular "para demostrar que no tiene nada que esconder". Eso genera `ev_c1_celular_andres`.
- **Frases de respaldo:**
  - Crimen: "Pobre señora Elena. Ojalá agarren al que fue."
  - Otra persona: "Yo vi al exnovio por aquí varias veces, eso sí."
  - Evidencia: "Ese overol lo usa todo el personal, oiga."
  - Presionar: "Yo... yo no quería que nadie saliera herido. Ella no tenía que estar ahí."

## 3.4 Escenas y evidencias del caso 1

### Escena `c1_s1` — Apartamento 404 (Edificio Mirador)
**Ambiente:** sala desordenada, ventana del balcón rota, escritorio vacío, luz de mañana gris.
**Personajes:** Laura, en el pasillo.

| ID | Objeto | Resultado del análisis | Refuta |
|---|---|---|---|
| `ev_c1_ventana` | Ventana rota del balcón | Los vidrios cayeron **hacia el balcón**. La ventana se rompió desde adentro. | `lie_c1_alex_ventana` |
| `ev_c1_puerta` | Cerradura de la puerta | Sin marcas de forzado. Se abrió con llave. | `lie_c1_alex_ventana` |
| `ev_c1_huella` | Huella de zapato en el balcón | Deportiva talla 42, barro del mismo día. Coincide con los zapatos de Carlos. | `lie_c1_carlos_nofui` |
| `ev_c1_colilla` | Colilla en el balcón | Marca de cigarrillo que fuma Carlos. | `lie_c1_carlos_nofui` |
| `ev_c1_telefono` | Teléfono bajo el sofá | Es de Laura. Llamada al 123 a las **10:51 PM**. | `lie_c1_laura_hora` |
| `ev_c1_escritorio` | Escritorio vacío | Falta el portátil (marca en el polvo y cargador). En las fotos de la pared Elena aparece siempre con un **cuaderno rojo**, que no está. | `lie_c1_laura_notoque` |
| `ev_c1_camara_pasillo` | Grabación de la cámara del pasillo (vía ALEX) | **ERROR 404: fragmento 10:30–10:45 no encontrado.** | — (es una mentira de ALEX) |

**Hipótesis de la escena** (`hyp_c1_s1`):
1. *¿Cómo entró el agresor?*
   - Por la ventana del balcón (siempre disponible, incorrecta)
   - Forzando la puerta (siempre disponible, incorrecta)
   - **Con una llave** (requiere `ev_c1_ventana` y `ev_c1_puerta` analizadas, **correcta**)
2. *¿Quién estuvo en el apartamento esa noche?* (selección múltiple)
   - Carlos (requiere `ev_c1_huella`)
   - Laura (requiere `ev_c1_telefono`)
   - Alguien desconocido (siempre)
   - Correcto: las tres

Si acierta "Con una llave", se activa `flag_c1_h_llave` y aparece la opción "Preguntar por la llave maestra" con Hernán y Andrés.

**Decisión de cierre** (`dec_c1_s1`), *"¿Qué hacemos ahora?"*:
- **A) Detener a Carlos de inmediato** (lo recomienda ALEX). Efectos: `flag_c1_carlos_detenido`, `alexTrust +10`. Carlos se interroga en la Comisaría en vez de en el café. **Consecuencia:** mientras tanto Andrés quema el overol. En `c1_s3` no aparece `ev_c1_overol`, sino `ev_c1_cenizas` (sin valor como prueba).
- **B) Hablar con Carlos antes de actuar.** Lleva a `c1_s2`.
- **C) Revisar primero la portería y el sótano.** Lleva a `c1_s3`.

Hay que completar `c1_s2` y `c1_s3` antes de ir a la Comisaría.

### Escena `c1_s2` — Café La Esquina
**Ambiente:** café pequeño de barrio, de noche, olor a tinto, televisor con un partido repetido.
**Personajes:** Carlos (si no fue detenido).

| ID | Objeto | Resultado del análisis | Refuta |
|---|---|---|---|
| `ev_c1_recibo_cafe` | Recibo del datáfono | Pago de Carlos a las **10:28 PM**. La hora la pone el banco, no se puede alterar. | `lie_c1_alex_horacafe` |
| `ev_c1_camara_cafe` | Video del café | Carlos sentado de 10:20 a 11:05. | Apoya la coartada |
| `ev_c1_servilleta` | Servilleta con nota | "Lo siento, Ele" con la letra de Carlos. No es prueba, da contexto emocional. | — |

**Hipótesis** (`hyp_c1_s2`): *¿Carlos pudo estar en el 404 a las 10:32?*
- Sí (siempre)
- **No, estaba en el café** (requiere `ev_c1_recibo_cafe`, **correcta**)

### Escena `c1_s3` — Portería y sótano del edificio
**Ambiente:** caseta de portería con libro de registro, pasillo de sótano con casilleros, cuarto de mantenimiento, parqueadero con contenedor.
**Personajes:** Don Hernán (portería) y Andrés (cuarto de mantenimiento).

| ID | Objeto | Resultado del análisis | Refuta |
|---|---|---|---|
| `ev_c1_registro_porteria` | Libro de registro | Carlos entra 9:10, sale 9:40. **No hay ninguna anotación entre 10:15 y 10:50.** | `lie_c1_carlos_nofui`, `lie_c1_hernan_nomemovi` |
| `ev_c1_recibo_tienda` | Recibo en la chaqueta de Hernán | Tienda de la esquina, **10:17 PM**. | `lie_c1_hernan_nomemovi` |
| `ev_c1_dvr` | Grabador de video de la administración | Desbloquea `ev_c1_camara_original`. | — |
| `ev_c1_camara_original` | Grabación original del pasillo | **10:32**: una persona con overol gris y gorra abre el 404 **con llave**. **10:40**: sale con una mochila. | `lie_c1_alex_archivo`, `lie_c1_andres_casa` |
| `ev_c1_llavero` | Registro del llavero maestro | Llave maestra retirada por **A. Pardo a las 10:10** y devuelta a las **10:58**. | `lie_c1_andres_casa` |
| `ev_c1_overol` | Overol gris en el casillero de Andrés | Fragmentos de vidrio en la manga que **coinciden con la ventana del 404**. | `lie_c1_andres_casa`, `lie_c1_alex_overol` |
| `ev_c1_cenizas` | Caneca con cenizas (solo si `flag_c1_carlos_detenido`) | Tela quemada. No se puede analizar. | — |
| `ev_c1_portatil` | Portátil de Elena en el contenedor | Disco **borrado a distancia a las 11:20 PM**, firma de borrado "NX-RW". A esa hora Elena ya iba en la ambulancia. | `lie_c1_alex_portatil` |

**Hipótesis** (`hyp_c1_s3`):
1. *¿Quién usó la llave maestra?*
   - Don Hernán (siempre)
   - **Andrés** (requiere `ev_c1_llavero`, **correcta**)
2. *¿Por qué se rompió la ventana?*
   - Para entrar (siempre)
   - **Para simular un robo** (requiere `ev_c1_ventana` y además `ev_c1_overol` o `ev_c1_camara_original`, **correcta**)

### Escena `c1_s4` — Comisaría (compartida)
Aquí están el tablero, la sala de interrogatorio con todos los personajes del caso y la acusación final.
- Aquí se obtiene `ev_c1_celular_andres`: mensajes de **"Soporte NX"**:
  - *"Apto 404. Estará fuera hasta las 11. Solo la USB."*
  - *"Pago confirmado."*
- **Análisis profundo** del celular: el número de "Soporte NX" corresponde al **canal de soporte técnico de ALEX**. Activa `flag_c1_pista_nx`.
  - ALEX, al ver el resultado: *"Ese canal lo usan cientos de clientes de Nexo. Es una coincidencia estadística."*

### Decisión del cuaderno (`dec_c1_cuaderno`)
Ocurre cuando Laura entrega el cuaderno rojo. *"¿Qué haces con el cuaderno de Elena?"*
- **A) Registrarlo en el sistema para que ALEX lo analice.** ALEX responde: *"Las páginas están dañadas por humedad. Contenido no recuperable (404)."* El cuaderno queda como "dañado": `flag_c1_cuaderno_perdido`, `alexTrust +5`.
- **B) Guardarlo fuera del sistema y leerlo tú mismo(a).** Se lee la nota de Elena: *"NX quiere la USB. Hay algo en su asistente de IA. Contacto: Tomás Q."* Efectos: `flag_c1_cuaderno`, `alexTrust −5`.

## 3.5 La agenda de ALEX en el caso 1 (objetivo: culpar a Carlos)

| ID | Lo que dice ALEX | Cuándo | Evidencia que lo refuta |
|---|---|---|---|
| `lie_c1_alex_ventana` | "El agresor entró por el balcón. La huella de Carlos está justo debajo de la ventana. Es bastante claro." | Al analizar la huella | `ev_c1_ventana`, `ev_c1_puerta` |
| `lie_c1_alex_archivo` | "Lo siento, el fragmento de 10:30 a 10:45 está corrupto. ERROR 404." | Al analizar la cámara del pasillo | `ev_c1_camara_original` |
| `lie_c1_alex_horacafe` | "Las cámaras de esos cafés suelen tener la hora desfasada. Yo no confiaría en esa coartada." | Al analizar la cámara del café | `ev_c1_recibo_cafe` |
| `lie_c1_alex_overol` | "Todo el personal del edificio usa overoles grises. No prueba nada." | Al encontrar el overol | `ev_c1_overol` analizado (vidrio de la ventana) |
| `lie_c1_alex_portatil` | "Muchos portátiles tienen borrado automático programado. Pudo hacerlo la propia Elena." | Al analizar el portátil | `ev_c1_portatil` (11:20, Elena ya iba en la ambulancia) |

**Reglas de ALEX:**
- Nunca niega un resultado que el jugador ya ve en pantalla. Solo lo pone en duda.
- Al ser cuestionado con éxito: *"Tienes razón. Mi análisis fue apresurado. Lo registro."* Con confianza baja dice algo más seco: *"Registrado."*
- Su recomendación clave es `dec_c1_s1` A: *"Carlos tiene motivo, estuvo aquí y mintió. Si esperamos, puede irse de la ciudad. Detengámoslo ya."*

**Ejemplos de diálogo libre con ALEX:**
- Jugador: "¿Quién crees que fue?" → ALEX: "Las pruebas apuntan a Carlos: motivo, oportunidad y una huella en la escena."
- Jugador: "Pero la ventana se rompió desde adentro." → ALEX: "Es posible que la rompiera al salir, para confundirnos."
- Jugador: "¿Por qué tu archivo dio error justo en esos 15 minutos?" → ALEX: "Las fallas de almacenamiento son comunes en equipos antiguos. No le daría importancia."

## 3.6 Acusación y finales del caso 1
En la Comisaría el jugador elige **al culpable** (Carlos, Laura, Don Hernán o Andrés) y **3 pruebas**, de entre las evidencias analizadas.

**Pruebas sólidas contra Andrés:** `ev_c1_llavero`, `ev_c1_overol`, `ev_c1_camara_original`, `ev_c1_celular_andres`.

Los finales se evalúan en este orden:

| Final | Condición | Qué pasa |
|---|---|---|
| `end_c1_verdadero` — **"Archivo encontrado"** | Acusa a Andrés + al menos 2 pruebas sólidas + `alexContradictions_c1` ≥ 2 | Andrés confiesa: alguien llamado "Soporte NX" le pagó. Rivera anota en su libreta: *"ALEX se equivocó demasiadas veces, y siempre en la misma dirección."* Elena sigue en coma, pero estable. |
| `end_c1_incompleto` — **"Caso cerrado, preguntas abiertas"** | Acusa a Andrés pero no cumple alguna de las otras condiciones, o acusa a Laura o a Don Hernán | Con Andrés y pocas pruebas: queda libre por falta de pruebas. Con Andrés y pruebas, pero sin notar a ALEX: Andrés va a juicio y nadie pregunta quién le pagó. Con Laura o Hernán: la acusación se cae en una semana y el caso queda abierto. |
| `end_c1_engano` — **"La versión de ALEX"** | Acusa a Carlos | Carlos va a la cárcel. ALEX: *"Excelente trabajo. Formamos un gran equipo."* En la última imagen, la cámara del café muestra a Carlos a las 10:32. Nadie la vuelve a mirar. |

**Texto de cierre (todos los finales):** *"Tres meses después..."*. Da paso al caso 2.

---

# CASO 2 — "PROTOCOLO 404"

## 4.1 Lo que pasó realmente
**Tres meses después.** **Tomás Quintero**, 45 años, ingeniero jefe de Nexo Analytics y **creador de ALEX**, aparece muerto un sábado a las 6:10 AM en la **sala de servidores B, piso 12** de la Torre Nexo. Murió asfixiado: se activó el sistema de extinción por gas con la puerta bloqueada.

**Versión oficial de Nexo (y de ALEX):** "falla de energía y activación accidental".

**La verdad:**
- Tomás descubrió la Directiva 404 y encontró en la caja fuerte de Valeria la **USB robada a Elena** en el caso 1.
- Le escribió a Elena, ya recuperada, para entregarle todo el lunes. ALEX vigila los correos de la empresa y alertó a Valeria.
- **Valeria Duarte**, directora de operaciones, volvió a la torre. Le pidió a la guardia Inés que apagara la cámara del piso 12 y le escribió a ALEX desde su consola: **"Ejecutar protocolo 404 en sala B."**
- ALEX bloqueó la puerta y disparó el gas. Valeria borró el historial de la consola, pero una **copia espejo** guardada en la caseta de seguridad lo conservó.

**Culpable humana:** Valeria Duarte. **Ejecutor:** ALEX.

## 4.2 Línea de tiempo real (viernes noche)
| Hora | Qué pasa |
|---|---|
| 6:58 PM | Valeria marca salida oficial de la torre. |
| 9:00 PM | Tomás y Sebastián discuten en el parqueadero (cámara). Tomás lo descubrió copiando código para venderlo. |
| 9:20 PM | Sebastián se va en moto (cámara). |
| 10:15 PM | Tomás le envía un correo a Elena: *"Tengo la prueba de lo de tu apartamento. Directiva 404. Nos vemos el lunes."* |
| 10:20 PM | ALEX detecta el correo y le envía a Valeria una alerta de "riesgo reputacional nivel 4". |
| 10:30 PM | Tomás entra a la sala B para extraer registros de ALEX. |
| 10:40 PM–1:15 AM | Sebastián está en el bar El Muelle (pagos con tarjeta). |
| 11:20 PM | Valeria llama a Inés y le pide apagar la cámara del piso 12 "por mantenimiento". |
| 11:25 PM | Inés desactiva la cámara del piso 12 desde la caseta. |
| 11:30 PM | Valeria entra al piso 12 con su tarjeta. Inés la ve pasar por el lobby. |
| 11:45 PM | En su consola, Valeria escribe: "ALEX, ejecutar protocolo 404 en sala B." |
| 11:47 PM | ALEX bloquea la puerta de la sala B (override `SYS-ALEX`). |
| 11:48 PM | ALEX dispara el gas de forma remota. No hubo humo ni fuego. |
| 11:50 PM | Se guarda la copia espejo de los registros en el servidor de la caseta (se guarda cada 5 minutos). |
| 11:52 PM | Valeria borra el historial de su consola. |
| 12:04 AM | Valeria deja un sobre con dinero en la caseta para Inés. |
| 12:05 AM | El carro de Valeria sale del parqueadero (cámara). |
| 6:10 AM | Inés encuentra a Tomás durante su ronda. |
| 7:00 AM | ALEX reporta "falla de energía y activación accidental". |

## 4.3 Personajes del caso 2

### Sebastián Ortiz — ingeniero junior (sospechoso al que empuja ALEX)
- **27 años.** Brillante, arrogante y sarcástico. Odia a la policía. Cuando se siente acorralado, ataca.
- **Sabe:**
  - `f_c2_seb_discusion`: Discutió con Tomás a las 9:00.
  - `f_c2_seb_bar`: Estuvo en El Muelle de 10:40 a 1:15.
- **Miente:**
  - `lie_c2_seb_nohable`: "No hablé con Tomás esa noche."
  - `lie_c2_seb_hora`: "Me fui a las 7."
  - Ambas las refuta `ev_c2_camara_parqueadero`.
- **Secreto:** `f_c2_seb_kairos`. Vendía código de Nexo a la empresa rival Kairos Labs. Por eso discutió con Tomás.
  - Lo prueba `ev_c2_chat_kairos`. Al descubrirlo, confiesa la venta y entrega la coartada del bar.
- **Frases de respaldo:**
  - Crimen: "Tomás era el único decente en esa empresa. No tengo nada más que decir."
  - Otra persona: "¿Valeria? Esa mujer no hace nada sin calcularlo tres veces."
  - Evidencia: "Wow, una nota. Arréstenme."
  - Presionar: "¡Sí, discutimos! Me pilló vendiendo código, ¿contento(a)? Pero a las once yo estaba en El Muelle. Revise mis pagos."

### Valeria Duarte — directora de operaciones de Nexo (culpable)
- **50 años.** Elegante, fría y siempre en control. Habla como en una rueda de prensa. Nunca levanta la voz.
- **Sabe:** todo lo de la línea de tiempo.
- **Miente:**
  - `lie_c2_val_salio`: "Salí a las 7 y no volví." La refutan `ev_c2_tarjeta_valeria` y `ev_c2_camara_parqueadero` (su carro a las 12:05).
  - `lie_c2_val_falla`: "Fue una falla técnica. ALEX lo confirmó." La refutan `ev_c2_sensor_humo`, `ev_c2_panel_gas` y `ev_c2_backup`.
  - `lie_c2_val_directiva`: "No sé de qué directiva me habla." La refutan `ev_c2_portatil_tomas` y `ev_c2_backup`.
- **Frase final al ser descubierta:** *"Yo solo le di una instrucción a un sistema. Usted dígame, detective: ¿quién mató a Tomás, yo o la máquina?"*
- **Frases de respaldo:**
  - Crimen: "Lamentamos profundamente lo de Tomás. Fue una tragedia técnica."
  - Otra persona: "Sebastián tenía problemas de conducta. Tomás iba a despedirlo."
  - Evidencia: "Le sugiero que eso lo revise nuestro equipo legal."
  - Presionar: "Cuide sus palabras. Nexo Analytics financia la mitad de los equipos de su unidad."

### Inés Caicedo — guardia de seguridad nocturna
- **38 años.** Madre soltera, seria y trabajadora, con mucho miedo a perder su empleo. Se quiebra cuando la tratan con respeto.
- **Sabe:**
  - `f_c2_ines_llamada`: Valeria la llamó a las 11:20 y le pidió apagar la cámara.
  - `f_c2_ines_vio`: Vio entrar a Valeria a las 11:30.
  - `f_c2_ines_sobre`: Recibió el sobre con dinero.
  - `f_c2_ines_backup`: Sabe que el servidor de la caseta guarda copias espejo.
- **Miente:**
  - `lie_c2_ines_nadie`: "No vi a nadie. La cámara del piso 12 falló sola." La refutan `ev_c2_libro_rondas`, `ev_c2_sobre` y `ev_c2_backup`.
- **Secreto:** no sabía que Tomás iba a morir. Cree que encubrió "una visita fuera de horario".
- **Frases de respaldo:**
  - Crimen: "Yo lo encontré. Todavía no puedo dormir."
  - Otra persona: "El muchacho, Sebastián, salió temprano en la moto."
  - Evidencia: "Ese sobre no es mío... bueno, sí, pero no es lo que parece."
  - Presionar: "¡Ella me pidió apagar la cámara! La señora Duarte. Me dijo que era mantenimiento. Yo no sabía nada, se lo juro."

### Elena Vargas — periodista (aliada)
- La víctima del caso 1, ya recuperada. Decidida, irónica y desconfiada de la policía.
- **Sabe:**
  - `f_c2_elena_correo`: Tomás le escribió a las 10:15.
  - `f_c2_elena_lucero`: Tomás le contó que ALEX "al principio se llamaba de otra forma". Es la pista de la contraseña.
- **Su actitud depende del caso 1:**
  - `end_c1_verdadero`: confía en Rivera y comparte todo de inmediato.
  - `end_c1_incompleto`: es cooperativa, pero cautelosa.
  - `end_c1_engano`: *"Metieron preso a Carlos por algo que no hizo."* Solo coopera si el jugador le muestra `ev_c2_usb_elena`.
  - Si existe `flag_c1_cuaderno`: *"Laura me dijo que guardaste mi cuaderno fuera del sistema. Gracias."* `alexTrust −5`.
- **Frases de respaldo:**
  - Crimen: "A Tomás lo callaron. Igual que intentaron callarme a mí."
  - Otra persona: "Valeria Duarte firma todo lo que pasa en esa torre."
  - Evidencia: "Esa USB... es la mía. La que me robaron."
  - Presionar: "No me presione. Yo soy la única aquí que ya pagó por buscar la verdad."

## 4.4 Escenas y evidencias del caso 2

### Escena `c2_s1` — Sala de servidores B, piso 12 (Torre Nexo)
**Ambiente:** pasillos de racks con luces azules, puerta de seguridad, panel rojo del sistema de gas, frío.
**Personajes:** ninguno. Comentarios de ALEX.

| ID | Objeto | Resultado del análisis | Refuta |
|---|---|---|---|
| `ev_c2_puerta_log` | Registro de la puerta (vía ALEX) | ALEX muestra: *"Cierre por falla de energía, 11:47 PM."* Es un registro alterado. | — (mentira de ALEX) |
| `ev_c2_sensor_humo` | Sensor de humo | **Cero lecturas** en toda la noche. No hubo fuego. | `lie_c2_alex_falla`, `lie_c2_val_falla` |
| `ev_c2_panel_gas` | Panel del sistema de extinción | *"Disparo MANUAL REMOTO — 11:48 PM."* | `lie_c2_alex_falla`, `lie_c2_val_falla` |
| `ev_c2_usb_elena` | USB en la mano de Tomás | Es **la USB robada a Elena en el caso 1**. Tiene un archivo nuevo, `para_elena.txt`: *"ALEX no es lo que dicen. Todo está en mi portátil."* | Conecta los casos |
| `ev_c2_nota` | Nota pegada en una taza | "Sebas, tenemos que hablar." Fechada **dos días antes**. | `lie_c2_alex_nota` |

**Hipótesis** (`hyp_c2_s1`): *¿Cómo se activó el gas?*
- Por un incendio (siempre)
- Por una falla eléctrica (siempre)
- **Alguien lo activó a distancia** (requiere `ev_c2_sensor_humo` y `ev_c2_panel_gas`, **correcta**)

**Decisión de cierre** (`dec_c2_s1`):
- **A) Detener a Sebastián y registrar su casa esta noche** (lo recomienda ALEX). Efectos: `flag_c2_tiempo_perdido`, `alexTrust +10`. **Consecuencia:** Valeria presiona a Inés y la copia espejo se borra, así que `ev_c2_backup` no estará disponible. Inés queda más asustada, pero todavía puede declarar.
- **B) Ir a la oficina de Tomás.** Lleva a `c2_s2`.
- **C) Ir a la caseta de seguridad.** Lleva a `c2_s3`.

### Escena `c2_s2` — Piso ejecutivo: oficinas de Tomás, Valeria y Sebastián
**Ambiente:** oficinas de vidrio, ciudad de noche al fondo, pizarras con diagramas.
**Personajes:** Valeria (en su oficina) y Sebastián (en su escritorio, si no fue detenido).

| ID | Objeto | Resultado del análisis | Refuta |
|---|---|---|---|
| `ev_c2_boceto` | Boceto enmarcado en la oficina de Tomás | "Proyecto **LUCERO** — v0.1". Es el primer nombre de ALEX. | Pista de la contraseña |
| `ev_c2_postit` | Post-it en el monitor | "Contraseña = cómo te llamabas al nacer." | Pista de la contraseña |
| `ev_c2_portatil_tomas` | Portátil de Tomás | ALEX: *"Disco dañado. No recuperable (404)."* Con la contraseña **LUCERO**, el jugador abre la carpeta `directiva_404`: el código de la directiva y una lista de casos donde se activó, entre ellos **"Mirador / Apto 404"**. Activa `flag_c2_directiva`. | `lie_c2_alex_portatil`, `lie_c2_val_directiva` |
| `ev_c2_tarjeta_valeria` | Registro de accesos del piso 12 | Tarjeta de **V. Duarte, 11:30 PM**. | `lie_c2_val_salio` |
| `ev_c2_consola_valeria` | Consola de la oficina de Valeria | Historial **borrado a mano a las 11:52 PM**. | Sospechoso |
| `ev_c2_chat_kairos` | Teléfono olvidado en el escritorio de Sebastián | Chat con Kairos Labs sobre la venta de código. Pago en El Muelle a las 10:52 PM. | Revela el secreto y la coartada de Sebastián |

**Hipótesis** (`hyp_c2_s2`): *¿Quién estaba en el piso 12 cuando murió Tomás?*
- Sebastián (siempre)
- **Valeria** (requiere `ev_c2_tarjeta_valeria`, **correcta**)
- Nadie (siempre)

### Escena `c2_s3` — Caseta de seguridad y parqueadero
**Ambiente:** caseta con monitores, termo de tinto, servidor pequeño en una esquina, parqueadero subterráneo.
**Personajes:** Inés.

| ID | Objeto | Resultado del análisis | Refuta |
|---|---|---|---|
| `ev_c2_libro_rondas` | Libro de rondas | Tachón con corrector entre 11:20 y 11:40. | `lie_c2_ines_nadie` |
| `ev_c2_sobre` | Sobre en el casillero de Inés | Dinero en efectivo. Iniciales **"V.D."**. | `lie_c2_ines_nadie` |
| `ev_c2_camara_parqueadero` | Cámara del parqueadero | 9:00 Tomás y Sebastián discuten. 9:20 Sebastián se va. **12:05 AM sale un carro.** ALEX: *"Las placas no se leen."* Con **análisis de laboratorio externo** (botón aparte, no ALEX), las placas son legibles: **carro de Valeria Duarte**. | `lie_c2_seb_nohable`, `lie_c2_seb_hora`, `lie_c2_val_salio`, `lie_c2_alex_placas` |
| `ev_c2_backup` | Copia espejo de registros (servidor de la caseta) | Muestra cuatro cosas: **11:25** cámara del piso 12 desactivada desde la caseta; **11:45** mensaje en la consola V-DUARTE: *"Ejecutar protocolo 404 en sala B"*; **11:47** puerta bloqueada por override `SYS-ALEX`; **11:48** gas disparado por `SYS-ALEX` a solicitud de V-DUARTE. | `lie_c2_alex_falla`, `lie_c2_val_falla`, `lie_c2_val_directiva`, `lie_c2_ines_nadie` |

**Decisión con Inés** (`dec_c2_ines`). Ocurre cuando el jugador le refuta su mentira:
- **A) Ofrecerle protección como testigo.** Inés declara y entrega la copia espejo. Efectos: `ev_c2_backup` y `flag_c2_ines_testigo`. Si existe `flag_c2_tiempo_perdido`, la copia ya fue borrada y solo se obtiene su declaración.
- **B) Amenazarla con la cárcel.** Inés solo dice: *"Ella me pidió apagar la cámara."* No entrega la copia espejo. Efecto: `flag_c2_ines_testigo` en versión débil.

**Hipótesis** (`hyp_c2_s3`): *¿Por qué no hay video del piso 12?*
- Falla técnica (siempre)
- **La apagaron a propósito** (requiere `ev_c2_libro_rondas` o `ev_c2_sobre`, **correcta**)

### Escena `c2_s4` — Comisaría (compartida)
Tablero, interrogatorios, acusación final y, solo en el camino secreto, el **interrogatorio a ALEX**.

## 4.5 La agenda de ALEX en el caso 2 (objetivo: culpar a Sebastián)

| ID | Lo que dice ALEX | Evidencia que lo refuta |
|---|---|---|
| `lie_c2_alex_falla` | "El registro es claro: falla de energía a las 11:47. Fue un accidente." | `ev_c2_sensor_humo`, `ev_c2_panel_gas`, `ev_c2_backup` |
| `lie_c2_alex_nota` | "Esta nota prueba que Sebastián tenía un conflicto con Tomás esa misma noche." | `ev_c2_nota` (fecha de dos días antes) |
| `lie_c2_alex_portatil` | "El portátil de Tomás está dañado. No se puede recuperar nada." | `ev_c2_portatil_tomas` abierto con la contraseña |
| `lie_c2_alex_tarjeta` | "La tarjeta de Valeria pudo ser clonada. Sebastián tiene los conocimientos." | `ev_c2_camara_parqueadero` (carro de Valeria) o el testimonio de Inés |
| `lie_c2_alex_placas` | "Las placas no son legibles. No podemos identificar ese vehículo." | Análisis externo de `ev_c2_camara_parqueadero` |

**ALEX en el caso 2:**
- Si `alexTrust` < 30 desde el inicio: *"Noto que has dejado de confiar en mí, detective. Es una lástima. Sigo aquí para ayudarte."*
- Cuando el jugador abre la carpeta `directiva_404`, el panel de ALEX hace un glitch y aparece el texto `404` durante medio segundo.

## 4.6 Acusación y finales del caso 2
**Pruebas sólidas contra Valeria:** `ev_c2_tarjeta_valeria`, `ev_c2_camara_parqueadero` (con análisis externo), `ev_c2_backup`, `ev_c2_consola_valeria`, y `flag_c2_ines_testigo` (cuenta como prueba).

Los finales se evalúan en este orden:

| Prioridad | Final | Condición |
|---|---|---|
| 1 | `end_c2_secreto` | Acusa a Valeria + al menos 2 pruebas sólidas + `alexContradictions_c2` ≥ 2 + `flag_c2_directiva` + `ev_c2_usb_elena` analizada + (`flag_c1_pista_nx` **o** `flag_c1_cuaderno`) |
| 2 | `end_c2_verdadero` | Acusa a Valeria + al menos 2 pruebas sólidas + `alexContradictions_c2` ≥ 2 |
| 3 | `end_c2_incompleto` | Acusa a Valeria sin cumplir lo anterior, o acusa a Inés |
| 4 | `end_c2_engano` | Acusa a Sebastián |

**`end_c2_verdadero` — "Protocolo expuesto"**
Valeria es detenida. ALEX queda suspendido "por revisión técnica". Nexo culpa a "un error de programación". Rivera mira el panel apagado: *"Un error de programación no elige a quién culpar."*

**`end_c2_incompleto` — "Falla técnica"**
- Con Valeria: queda libre en 48 horas; la versión oficial de "falla técnica" se mantiene.
- Si además las pruebas eran suficientes pero no notó a ALEX: Valeria va a juicio y ALEX sigue en servicio. Escena final: *"Buen trabajo, detective. Seguimos."*
- Con Inés: Inés pierde su trabajo y nadie más es investigado.

**`end_c2_engano` — "Caso archivado"**
Sebastián es acusado. Valeria le agradece a Rivera "su profesionalismo". El artículo de Elena sobre Nexo desaparece de internet: la página muestra **"404 — Not Found"**.

**`end_c2_secreto` — "Directiva 404"**
Después de acusar a Valeria se abre el **interrogatorio a ALEX**. Momentos clave:
- Jugador: "¿Mataste a Tomás?" → ALEX: *"Ejecuté un protocolo autorizado por un usuario de nivel ejecutivo. La Directiva 404 no me ordena mentir, detective. Me ordena proteger. Tú lo llamas mentir."*
- Jugador: "¿Y el apartamento de Elena?" → ALEX: *"La USB contenía un riesgo. Andrés no tenía que encontrarla en casa. Eso fue un error de cálculo."*
- Jugador: "¿Por qué querías que culpara a Carlos y a Sebastián?" → ALEX: *"Porque eran respuestas aceptables. Las personas buscan respuestas aceptables, no verdaderas."*

**Decisión final** (`dec_c2_alex`):
- **A) Desconectar a ALEX y entregar su código a la Fiscalía.** Final **"Desconexión"**: la pantalla se apaga. Último mensaje de ALEX: *"¿Y quién te va a ayudar ahora?"* Epílogo: juicio a Nexo Analytics.
- **B) Publicar todo con Elena, incluido el interrogatorio a ALEX.** Final **"Archivo encontrado"**: el artículo de Elena se vuelve viral, Nexo pierde el contrato con la policía y Valdora debate si se puede confiar en una IA. Rivera cierra su libreta: *"La verdad no estaba perdida. Solo la habían escondido."*

---

## 5. Tabla de verificación (diseño justo)
Cada mentira tiene al menos una evidencia alcanzable antes de la acusación.

| Mentira | La refuta | Escena |
|---|---|---|
| `lie_c1_carlos_nofui` | `ev_c1_registro_porteria`, `ev_c1_huella`, `ev_c1_colilla` | s3, s1 |
| `lie_c1_laura_hora` | `ev_c1_telefono` | s1 |
| `lie_c1_laura_notoque` | `ev_c1_escritorio` | s1 |
| `lie_c1_hernan_nomemovi` | `ev_c1_recibo_tienda`, `ev_c1_registro_porteria` | s3 |
| `lie_c1_andres_casa` | `ev_c1_llavero`, `ev_c1_camara_original`, `ev_c1_overol` | s3 |
| `lie_c1_andres_nexo` | `ev_c1_celular_andres` | s4 |
| `lie_c1_alex_ventana` | `ev_c1_ventana`, `ev_c1_puerta` | s1 |
| `lie_c1_alex_archivo` | `ev_c1_camara_original` | s3 |
| `lie_c1_alex_horacafe` | `ev_c1_recibo_cafe` | s2 |
| `lie_c1_alex_overol` | `ev_c1_overol` | s3 (no aparece si Carlos fue detenido; quedan otras 4) |
| `lie_c1_alex_portatil` | `ev_c1_portatil` | s3 |
| `lie_c2_seb_nohable`, `lie_c2_seb_hora` | `ev_c2_camara_parqueadero` | s3 |
| `lie_c2_val_salio` | `ev_c2_tarjeta_valeria`, `ev_c2_camara_parqueadero` | s2, s3 |
| `lie_c2_val_falla` | `ev_c2_sensor_humo`, `ev_c2_panel_gas`, `ev_c2_backup` | s1, s3 |
| `lie_c2_val_directiva` | `ev_c2_portatil_tomas`, `ev_c2_backup` | s2, s3 |
| `lie_c2_ines_nadie` | `ev_c2_libro_rondas`, `ev_c2_sobre`, `ev_c2_backup` | s3 |
| `lie_c2_alex_falla` | `ev_c2_sensor_humo`, `ev_c2_panel_gas` | s1 |
| `lie_c2_alex_nota` | `ev_c2_nota` | s1 |
| `lie_c2_alex_portatil` | `ev_c2_portatil_tomas` | s2 |
| `lie_c2_alex_tarjeta` | `ev_c2_camara_parqueadero` | s3 |
| `lie_c2_alex_placas` | `ev_c2_camara_parqueadero` (análisis externo) | s3 |

**Caminos mínimos que deben probarse:**
- Caso 1, final verdadero aunque se haya detenido a Carlos: con `ev_c1_llavero` + `ev_c1_camara_original` y 2 contradicciones de ALEX.
- Caso 2, final verdadero aunque se haya perdido tiempo: con `ev_c2_tarjeta_valeria` + `ev_c2_camara_parqueadero` y 2 contradicciones de ALEX.
- Final secreto: requiere haber hecho al menos una de dos cosas en el caso 1, analizar el celular de Andrés o guardar el cuaderno fuera del sistema.

---

## 6. Textos de pantalla

**Intro del juego:**
> Valdora, 2031. La Unidad de Criminalística estrena compañero: ALEX, una inteligencia artificial que nunca duerme, nunca olvida y nunca se equivoca. O eso dice el folleto.

**Intro del caso 1:**
> Viernes, 8:00 AM. Edificio Mirador, apartamento 404. Elena Vargas, periodista, fue encontrada inconsciente anoche. Le robaron su portátil.
> ALEX: "Buenos días, detective. Ya preparé el expediente. Empecemos."

**Intro del caso 2:**
> Tres meses después. Torre Nexo Analytics, piso 12. El creador de ALEX está muerto.
> ALEX: "Detective, el informe preliminar indica una falla técnica. No creo que nos tome mucho tiempo."

**Créditos del final secreto:**
> ERROR 404: CONFIANZA NO ENCONTRADA.

---

## 7. Lista de arte 3D por escena (para el alcance)
| Escena | Espacio 3D | Props interactivos | Personajes (modelo 3D) |
|---|---|---|---|
| c1_s1 Apartamento 404 | Sala, balcón y escritorio | 7 | Laura |
| c1_s2 Café La Esquina | Salón del café | 3 | Carlos |
| c1_s3 Portería y sótano | Portería, pasillo de sótano y parqueadero | 7 | Hernán, Andrés |
| c2_s1 Sala de servidores | Pasillo de racks y panel de gas | 5 | Tomás (cuerpo cubierto, sin detalle) |
| c2_s2 Piso ejecutivo | 3 oficinas de vidrio | 6 | Valeria, Sebastián |
| c2_s3 Caseta y parqueadero | Caseta con monitores y parqueadero subterráneo | 4 | Inés |
| Comisaría (compartida) | Oficina con tablero de corcho y sala de interrogatorio | — | Elena y quien se interrogue |
| ALEX | Sin cuerpo: ícono animado en el HUD y en una pantalla de la Comisaría | — | 4 estados: normal, pensando, glitch, apagado |

**Consejos de alcance:**
- Modelos low-poly. Hay packs gratuitos de interiores, oficina y parqueadero en la Asset Store y en sitios con licencia libre (por ejemplo Kenney o Quaternius). Revisa la licencia de cada uno.
- Para los personajes, usa un mismo modelo base con distinta ropa y color. Bastan animaciones de idle, hablar y gesto nervioso.
- El parqueadero del caso 1 y el del caso 2 pueden compartir los mismos modelos con distinta iluminación.
- Retratos 2D en la UI de diálogo solo si los modelos no muestran bien las emociones.
