using System.IO;
using Caso404.Data;
using UnityEditor;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Crea (o actualiza) los assets del caso 2 "Protocolo 404" a partir de Docs/GUION.md §4, y lo agrega a la GameDatabase.
    /// Uso: menú Caso404/Generar datos caso 2, o -executeMethod Caso404.EditorTools.Case2DataBuilder.Build
    /// </summary>
    public static class Case2DataBuilder
    {
        const string Root = "Assets/_Project/Data";
        public const string Case2Path = Root + "/Cases/c2.asset";

        [MenuItem("Caso404/Generar datos caso 2")]
        public static void Build()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Case1DataBuilder.DatabasePath);
            if (db == null) { Case1DataBuilder.Build(); db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Case1DataBuilder.DatabasePath); }
            var c1 = db.FindCase("c1");

            // ---------- Flags ----------
            var fTiempo = Flag("flag_c2_tiempo_perdido", "Se detuvo a Sebastián en dec_c2_s1 (A): Valeria presiona a Inés y la copia espejo se borra.");
            var fDirectiva = Flag("flag_c2_directiva", "Se abrió la carpeta directiva_404 del portátil de Tomás con la contraseña LUCERO.", true);
            var fTestigo = Flag("flag_c2_ines_testigo", "Inés declara como testigo protegida (cuenta como prueba sólida).");
            var fTestigoDebil = Flag("flag_c2_ines_testigo_debil", "Inés, amenazada, solo dice que Valeria le pidió apagar la cámara (no cuenta como prueba).");
            var fPlacas = Flag("flag_c2_placas_legibles", "El laboratorio externo hizo legibles las placas: carro de Valeria Duarte.");
            // Flags del caso 1 que usa el caso 2 (GUION §2)
            var fPistaNx = Load<FlagData>($"{Root}/Flags/flag_c1_pista_nx.asset");
            var fCuaderno = Load<FlagData>($"{Root}/Flags/flag_c1_cuaderno.asset");
            var fEndVerdadero = Flag("end_c1_verdadero", "El caso 1 terminó con el final verdadero.", true);
            var fEndIncompleto = Flag("end_c1_incompleto", "El caso 1 terminó con el final incompleto.", true);
            var fEndEngano = Flag("end_c1_engano", "El caso 1 terminó con el final del engaño.", true);
            var flags = new[] { fTiempo, fDirectiva, fTestigo, fTestigoDebil, fPlacas };

            // ---------- Evidencias ----------
            var puertaLog = Ev("ev_c2_puerta_log"); var sensor = Ev("ev_c2_sensor_humo"); var panelGas = Ev("ev_c2_panel_gas");
            var usb = Ev("ev_c2_usb_elena"); var nota = Ev("ev_c2_nota");
            var boceto = Ev("ev_c2_boceto"); var postit = Ev("ev_c2_postit"); var portatil = Ev("ev_c2_portatil_tomas");
            var tarjeta = Ev("ev_c2_tarjeta_valeria"); var consola = Ev("ev_c2_consola_valeria"); var chat = Ev("ev_c2_chat_kairos");
            var rondas = Ev("ev_c2_libro_rondas"); var sobre = Ev("ev_c2_sobre"); var camParq = Ev("ev_c2_camara_parqueadero"); var backup = Ev("ev_c2_backup");

            Fill(puertaLog, "c2_s1", "Registro de la puerta", "ALEX puede mostrar el registro de la puerta de seguridad de la sala B.",
                "ALEX: \"Cierre por falla de energía, 11:47 PM.\"");
            puertaLog.AnalyzedByAlex = true;
            Fill(sensor, "c2_s1", "Sensor de humo", "El sensor de humo del techo de la sala B.", "Cero lecturas en toda la noche. No hubo fuego.");
            Fill(panelGas, "c2_s1", "Panel del sistema de extinción", "El panel rojo del sistema de gas.", "\"Disparo MANUAL REMOTO — 11:48 PM.\"");
            Fill(usb, "c2_s1", "USB en la mano de Tomás", "Una memoria USB en la mano de Tomás.",
                "Es la USB robada a Elena en el caso 1. Tiene un archivo nuevo, para_elena.txt: \"ALEX no es lo que dicen. Todo está en mi portátil.\"");
            Fill(nota, "c2_s1", "Nota pegada en una taza", "Una taza con una nota pegada: \"Sebas, tenemos que hablar.\"",
                "\"Sebas, tenemos que hablar.\" Fechada dos días antes.");
            nota.HasHiddenDetail = true;
            nota.HiddenDetailText = "Al girar la taza se ve la fecha de la nota: dos días antes.";

            Fill(boceto, "c2_s2", "Boceto enmarcado", "Un boceto enmarcado en la oficina de Tomás.", "\"Proyecto LUCERO — v0.1\". Es el primer nombre de ALEX.");
            Fill(postit, "c2_s2", "Post-it en el monitor", "Un post-it pegado en el monitor de Tomás.", "\"Contraseña = cómo te llamabas al nacer.\"");
            Fill(portatil, "c2_s2", "Portátil de Tomás", "El portátil de Tomás Quintero.", "ALEX: \"Disco dañado. No recuperable (404).\"");
            portatil.AnalyzedByAlex = true;
            portatil.ExtraAnalysis = new ExtraAnalysis
            {
                Kind = ExtraAnalysisKind.Password,
                ButtonLabel = "Probar contraseña",
                Password = "LUCERO",
                ResultText = "Carpeta directiva_404: el código de la directiva y una lista de casos donde se activó, entre ellos \"Mirador / Apto 404\".",
                SetsFlags = new[] { fDirectiva }
                , AlexGlitch = true
            };
            Fill(tarjeta, "c2_s2", "Registro de accesos del piso 12", "El registro de tarjetas de acceso del piso 12.", "Tarjeta de V. Duarte, 11:30 PM.");
            Fill(consola, "c2_s2", "Consola de Valeria", "La consola de la oficina de Valeria Duarte.", "Historial borrado a mano a las 11:52 PM.");
            Fill(chat, "c2_s2", "Teléfono olvidado de Sebastián", "Un teléfono olvidado en el escritorio de Sebastián.",
                "Chat con Kairos Labs sobre la venta de código. Pago en El Muelle a las 10:52 PM.");

            Fill(rondas, "c2_s3", "Libro de rondas", "El libro de rondas de la guardia nocturna.", "Tachón con corrector entre 11:20 y 11:40.");
            Fill(sobre, "c2_s3", "Sobre en el casillero de Inés", "Un sobre con dinero en efectivo.", "Dinero en efectivo. Iniciales \"V.D.\".");
            sobre.HasHiddenDetail = true;
            sobre.HiddenDetailText = "Al girarlo, se ven unas iniciales escritas a mano: \"V.D.\"";
            Fill(camParq, "c2_s3", "Cámara del parqueadero", "La grabación de la cámara del parqueadero subterráneo.",
                "9:00 Tomás y Sebastián discuten. 9:20 Sebastián se va. 12:05 AM sale un carro. ALEX: \"Las placas no se leen.\"");
            camParq.ExtraAnalysis = new ExtraAnalysis
            {
                Kind = ExtraAnalysisKind.External,
                ButtonLabel = "Análisis de laboratorio externo",
                ResultText = "Las placas son legibles: es el carro de Valeria Duarte.",
                SetsFlags = new[] { fPlacas }
            };
            Fill(backup, "c2_s3", "Copia espejo de registros", "La copia espejo del servidor de la caseta de seguridad.",
                "11:25 cámara del piso 12 desactivada desde la caseta. 11:45 mensaje en la consola V-DUARTE: \"Ejecutar protocolo 404 en sala B\". 11:47 puerta bloqueada por override SYS-ALEX. 11:48 gas disparado por SYS-ALEX a solicitud de V-DUARTE.");
            backup.Source = EvidenceSource.Dialogue;
            backup.SpawnCondition = Cond(none: new[] { fTiempo });

            var allEvidence = new[] { puertaLog, sensor, panelGas, usb, nota, boceto, postit, portatil, tarjeta, consola, chat, rondas, sobre, camParq, backup };

            // ---------- Decisiones ----------
            var decIdx = Asset<DecisionData>($"{Root}/Decisions/C2/dec_c2_ines.asset");
            decIdx.Id = "dec_c2_ines";
            decIdx.Prompt = "¿Qué haces con Inés?";
            decIdx.Irreversible = true;
            decIdx.AlexRecommendedOptionId = "";
            decIdx.AlexRecommendationText = "";
            decIdx.Options = new[]
            {
                new DecisionOption { Id = "A", Text = "Ofrecerle protección como testigo", SetsFlags = new[] { fTestigo }, GrantsEvidence = new[] { backup },
                    ResultText = "Inés declara como testigo. Si la copia espejo todavía está en el servidor de la caseta, te la entrega." },
                new DecisionOption { Id = "B", Text = "Amenazarla con la cárcel", SetsFlags = new[] { fTestigoDebil },
                    ResultText = "Inés solo dice: \"Ella me pidió apagar la cámara.\"" },
            };
            Dirty(decIdx);

            var decS1 = Asset<DecisionData>($"{Root}/Decisions/C2/dec_c2_s1.asset");
            decS1.Id = "dec_c2_s1";
            decS1.Prompt = "¿Qué hacemos ahora?";
            decS1.Irreversible = true;
            decS1.AlexRecommendedOptionId = "A";
            decS1.AlexRecommendationText = "Esta nota prueba que Sebastián tenía un conflicto con Tomás esa misma noche.";
            decS1.Options = new[]
            {
                new DecisionOption { Id = "A", Text = "Detener a Sebastián y registrar su casa esta noche", SetsFlags = new[] { fTiempo }, AlexTrustDelta = 10,
                    LetPlayerChooseNextScene = true, ResultText = "Sebastián queda detenido y se registra su casa esta noche." },
                new DecisionOption { Id = "B", Text = "Ir a la oficina de Tomás", NextSceneId = "c2_s2" },
                new DecisionOption { Id = "C", Text = "Ir a la caseta de seguridad", NextSceneId = "c2_s3" },
            };
            Dirty(decS1);

            var decAlex = Asset<DecisionData>($"{Root}/Decisions/C2/dec_c2_alex.asset");
            decAlex.Id = "dec_c2_alex";
            decAlex.Prompt = "¿Qué haces con ALEX?";
            decAlex.Irreversible = true;
            decAlex.AlexRecommendedOptionId = "";
            decAlex.AlexRecommendationText = "";
            decAlex.Options = new[]
            {
                new DecisionOption { Id = "A", Text = "Desconectar a ALEX y entregar su código a la Fiscalía",
                    ResultText = "Desconexión. La pantalla se apaga. Último mensaje de ALEX: \"¿Y quién te va a ayudar ahora?\" Epílogo: juicio a Nexo Analytics." },
                new DecisionOption { Id = "B", Text = "Publicar todo con Elena, incluido el interrogatorio a ALEX",
                    ResultText = "Archivo encontrado. El artículo de Elena se vuelve viral, Nexo pierde el contrato con la policía y Valdora debate si se puede confiar en una IA. Rivera cierra su libreta: \"La verdad no estaba perdida. Solo la habían escondido.\"" },
            };
            Dirty(decAlex);

            // ---------- Mentiras ----------
            var lSebNoHable = Lie("lie_c2_seb_nohable", "sebastian", "No hablé con Tomás esa noche.", new[] { camParq }, null,
                "Está bien, hablé con él. ¿Y qué? Eso no me convierte en asesino.", new[] { "f_c2_seb_discusion" });
            var lSebHora = Lie("lie_c2_seb_hora", "sebastian", "Me fui a las 7.", new[] { camParq }, null,
                "Me fui a las nueve y veinte, en la moto. ¿Contento(a)?", null);
            var lValSalio = Lie("lie_c2_val_salio", "valeria", "Salí a las 7 y no volví.", new[] { tarjeta, camParq }, null,
                "Le sugiero que eso lo revise nuestro equipo legal.", null);
            lValSalio.EvidenceRequirements = new[] { new EvidenceRequirement(camParq, fPlacas) };
            var lValFalla = Lie("lie_c2_val_falla", "valeria", "Fue una falla técnica. ALEX lo confirmó.", new[] { sensor, panelGas, backup }, null,
                "Cuide sus palabras. Nexo Analytics financia la mitad de los equipos de su unidad.", null);
            var lValDirectiva = Lie("lie_c2_val_directiva", "valeria", "No sé de qué directiva me habla.", new[] { portatil, backup }, null,
                "Cuide sus palabras. Nexo Analytics financia la mitad de los equipos de su unidad.", null);
            lValDirectiva.EvidenceRequirements = new[] { new EvidenceRequirement(portatil, fDirectiva) };
            var lInes = Lie("lie_c2_ines_nadie", "ines", "No vi a nadie. La cámara del piso 12 falló sola.", new[] { rondas, sobre, backup }, null,
                "¡Ella me pidió apagar la cámara! La señora Duarte. Me dijo que era mantenimiento. Yo no sabía nada, se lo juro.",
                new[] { "f_c2_ines_llamada", "f_c2_ines_vio", "f_c2_ines_sobre", "f_c2_ines_backup" });
            lInes.TriggersDecision = decIdx;

            var aFalla = AlexLie("lie_c2_alex_falla", "El registro es claro: falla de energía a las 11:47. Fue un accidente.",
                AlexClaimTrigger.OnEvidenceAnalyzed, puertaLog, sensor, panelGas, backup);
            var aNota = AlexLie("lie_c2_alex_nota", "Esta nota prueba que Sebastián tenía un conflicto con Tomás esa misma noche.",
                AlexClaimTrigger.OnEvidenceFound, nota, nota);
            var aPortatil = AlexLie("lie_c2_alex_portatil", "El portátil de Tomás está dañado. No se puede recuperar nada.",
                AlexClaimTrigger.OnEvidenceAnalyzed, portatil, portatil);
            aPortatil.EvidenceRequirements = new[] { new EvidenceRequirement(portatil, fDirectiva) };
            var aTarjeta = AlexLie("lie_c2_alex_tarjeta", "La tarjeta de Valeria pudo ser clonada. Sebastián tiene los conocimientos.",
                AlexClaimTrigger.OnEvidenceAnalyzed, tarjeta, camParq);
            aTarjeta.EvidenceRequirements = new[] { new EvidenceRequirement(camParq, fPlacas) };
            aTarjeta.RefutedByFactIds = new[] { "f_c2_ines_vio" };
            var aPlacas = AlexLie("lie_c2_alex_placas", "Las placas no son legibles. No podemos identificar ese vehículo.",
                AlexClaimTrigger.OnEvidenceAnalyzed, camParq, camParq);
            aPlacas.EvidenceRequirements = new[] { new EvidenceRequirement(camParq, fPlacas) };
            foreach (var l in new[] { aPortatil, aTarjeta, aPlacas }) Dirty(l);
            var allLies = new[] { lSebNoHable, lSebHora, lValSalio, lValFalla, lValDirectiva, lInes, aFalla, aNota, aPortatil, aTarjeta, aPlacas };

            // ---------- Personajes ----------
            var sebastian = Character("sebastian", "Sebastián Ortiz", 27, "Ingeniero junior de Nexo Analytics",
                "Brillante, arrogante y sarcástico. Odia a la policía. Cuando se siente acorralado, ataca.", new Color(0.25f, 0.35f, 0.25f),
                new[]
                {
                    new FactEntry("f_c2_seb_discusion", "Discutió con Tomás a las 9:00."),
                    new FactEntry("f_c2_seb_bar", "Estuvo en El Muelle de 10:40 a 1:15."),
                    new FactEntry("f_c2_seb_kairos", "Vendía código de Nexo a la empresa rival Kairos Labs. Por eso discutió con Tomás."),
                },
                new[] { lSebNoHable, lSebHora },
                new FallbackLine("Tomás era el único decente en esa empresa. No tengo nada más que decir."),
                new FallbackLine("¿Valeria? Esa mujer no hace nada sin calcularlo tres veces."),
                new FallbackLine("Wow, una nota. Arréstenme."),
                new FallbackLine("¡Sí, discutimos! Me pilló vendiendo código, ¿contento(a)? Pero a las once yo estaba en El Muelle. Revise mis pagos."));
            sebastian.Secrets = new[]
            {
                new SecretEntry
                {
                    FactId = "f_c2_seb_kairos",
                    RevealText = "¡Sí, discutimos! Me pilló vendiendo código, ¿contento(a)? Pero a las once yo estaba en El Muelle. Revise mis pagos.",
                    RevealedByEvidence = chat,
                    UnlockCondition = Cond(all: new[] { chat }),
                    AlsoRevealsFactIds = new[] { "f_c2_seb_bar", "f_c2_seb_discusion" }
                }
            };
            Dirty(sebastian);

            var valeria = Character("valeria", "Valeria Duarte", 50, "Directora de operaciones de Nexo Analytics",
                "Elegante, fría y siempre en control. Habla como en una rueda de prensa. Nunca levanta la voz.", new Color(0.35f, 0.2f, 0.3f),
                new FactEntry[0],
                new[] { lValSalio, lValFalla, lValDirectiva },
                new FallbackLine("Lamentamos profundamente lo de Tomás. Fue una tragedia técnica."),
                new FallbackLine("Sebastián tenía problemas de conducta. Tomás iba a despedirlo."),
                new FallbackLine("Le sugiero que eso lo revise nuestro equipo legal."),
                new FallbackLine("Cuide sus palabras. Nexo Analytics financia la mitad de los equipos de su unidad."));

            var ines = Character("ines", "Inés Caicedo", 38, "Guardia de seguridad nocturna de la Torre Nexo",
                "Madre soltera, seria y trabajadora, con mucho miedo a perder su empleo. Se quiebra cuando la tratan con respeto.", new Color(0.2f, 0.25f, 0.4f),
                new[]
                {
                    new FactEntry("f_c2_ines_llamada", "Valeria la llamó a las 11:20 y le pidió apagar la cámara."),
                    new FactEntry("f_c2_ines_vio", "Vio entrar a Valeria a las 11:30."),
                    new FactEntry("f_c2_ines_sobre", "Recibió un sobre con dinero."),
                    new FactEntry("f_c2_ines_backup", "El servidor de la caseta guarda copias espejo de los registros."),
                },
                new[] { lInes },
                new FallbackLine("Yo lo encontré. Todavía no puedo dormir."),
                new FallbackLine("El muchacho, Sebastián, salió temprano en la moto."),
                new FallbackLine("Ese sobre no es mío... bueno, sí, pero no es lo que parece."),
                new FallbackLine("¡Ella me pidió apagar la cámara! La señora Duarte. Me dijo que era mantenimiento. Yo no sabía nada, se lo juro."));

            var elena = Character("elena", "Elena Vargas", 34, "Periodista de El Faro de Valdora (aliada)",
                "Decidida, irónica y desconfiada de la policía. Ya se recuperó del ataque del caso 1.", new Color(0.55f, 0.45f, 0.2f),
                new[]
                {
                    new FactEntry("f_c2_elena_correo", "Tomás le escribió a las 10:15: \"Tengo la prueba de lo de tu apartamento. Directiva 404. Nos vemos el lunes.\""),
                    new FactEntry("f_c2_elena_lucero", "Tomás le contó que ALEX \"al principio se llamaba de otra forma\"."),
                },
                new LieData[0],
                new FallbackLine("A Tomás lo callaron. Igual que intentaron callarme a mí."),
                new FallbackLine("Valeria Duarte firma todo lo que pasa en esa torre."),
                new FallbackLine("Esa USB... es la mía. La que me robaron."),
                new FallbackLine("No me presione. Yo soy la única aquí que ya pagó por buscar la verdad."));
            elena.ExtraTopics = new[]
            {
                new ExtraTopic { Id = "tomas_correo", Label = "Preguntar por el correo de Tomás",
                    Line = new FallbackLine("Tomás me escribió a las 10:15: \"Tengo la prueba de lo de tu apartamento. Directiva 404. Nos vemos el lunes.\"", "f_c2_elena_correo") },
                new ExtraTopic { Id = "tomas_alex", Label = "Preguntar qué le contó Tomás de ALEX",
                    Line = new FallbackLine("Tomás me dijo una vez que ALEX al principio se llamaba de otra forma.", "f_c2_elena_lucero") },
            };
            // GUION §4.3: tras end_c1_engano, Elena solo coopera si le muestras su USB.
            elena.RefusesUnless = Cond(allFlags: new[] { fEndEngano });
            elena.CooperatesAfterSeeing = usb;
            elena.RefusalLine = "Metieron preso a Carlos por algo que no hizo. No tengo nada que hablar con la policía.";
            elena.AttitudeVariants = new[]
            {
                new AttitudeVariant { Condition = Cond(allFlags: new[] { fEndVerdadero }), Text = "(Elena confía en ti y comparte todo de inmediato.)" },
                new AttitudeVariant { Condition = Cond(allFlags: new[] { fEndIncompleto }), Text = "(Elena coopera, pero con cautela.)" },
                new AttitudeVariant { Condition = Cond(allFlags: new[] { fEndEngano }), Text = "Metieron preso a Carlos por algo que no hizo." },
                new AttitudeVariant { Condition = Cond(allFlags: new[] { fCuaderno }), Text = "Laura me dijo que guardaste mi cuaderno fuera del sistema. Gracias.", AlexTrustDelta = -5 },
            };
            Dirty(elena);

            // ---------- Hipótesis ----------
            var hypS1 = Hyp("hyp_c2_s1", "c2_s1", new HypothesisQuestion("hyp_c2_s1_q1", "¿Cómo se activó el gas?", false, new[]
            {
                new HypothesisOption("incendio", "Por un incendio", false),
                new HypothesisOption("falla", "Por una falla eléctrica", false),
                new HypothesisOption("distancia", "Alguien lo activó a distancia", true, Cond(all: new[] { sensor, panelGas })),
            }));
            var hypS2 = Hyp("hyp_c2_s2", "c2_s2", new HypothesisQuestion("hyp_c2_s2_q1", "¿Quién estaba en el piso 12 cuando murió Tomás?", false, new[]
            {
                new HypothesisOption("sebastian", "Sebastián", false),
                new HypothesisOption("valeria", "Valeria", true, Cond(all: new[] { tarjeta })),
                new HypothesisOption("nadie", "Nadie", false),
            }));
            var hypS3 = Hyp("hyp_c2_s3", "c2_s3", new HypothesisQuestion("hyp_c2_s3_q1", "¿Por qué no hay video del piso 12?", false, new[]
            {
                new HypothesisOption("falla", "Falla técnica", false),
                new HypothesisOption("apagada", "La apagaron a propósito", true, Cond(any: new[] { rondas, sobre })),
            }));

            // ---------- Escenas ----------
            var s1 = SceneAsset("c2_s1", "ServidoresB", "Sala de servidores B — Piso 12, Torre Nexo",
                "Pasillos de racks con luces azules, puerta de seguridad, panel rojo del sistema de gas, frío.",
                "Tres meses después. Torre Nexo Analytics, piso 12. El creador de ALEX está muerto.",
                new[] { puertaLog, sensor, panelGas, usb, nota }, new ScenePresence[0], hypS1, decS1, null, false);
            var s2 = SceneAsset("c2_s2", "PisoEjecutivo", "Piso ejecutivo — Torre Nexo",
                "Oficinas de vidrio, ciudad de noche al fondo, pizarras con diagramas.", "",
                new[] { boceto, postit, portatil, tarjeta, consola, chat },
                new[] { new ScenePresence(valeria), new ScenePresence(sebastian, Cond(none: new[] { fTiempo })) }, hypS2, null, null, false);
            var s3 = SceneAsset("c2_s3", "CasetaSeguridad", "Caseta de seguridad y parqueadero",
                "Caseta con monitores, termo de tinto, servidor pequeño en una esquina, parqueadero subterráneo.", "",
                new[] { rondas, sobre, camParq, backup }, new[] { new ScenePresence(ines) }, hypS3, null, null, false);
            var s4 = SceneAsset("c2_s4", "Comisaria", "Comisaría", "Oficina con tablero de corcho y sala de interrogatorio.", "",
                new EvidenceData[0],
                new[] { new ScenePresence(sebastian), new ScenePresence(valeria), new ScenePresence(ines), new ScenePresence(elena) },
                null, null, new[] { "c2_s2", "c2_s3" }, true);

            // ---------- Finales ----------
            var credits = "ERROR 404: CONFIANZA NO ENCONTRADA.";
            var endSecreto = Ending("end_c2_secreto", 1, "Directiva 404",
                new AccusationRequirement
                {
                    AccusedAnyOf = new[] { valeria }, MinSolidProofs = 2, MinAlexContradictions = 2,
                    Extra = new Condition { AllFlags = new[] { fDirectiva }, AllEvidenceAnalyzed = new[] { usb }, AnyFlags = new[] { fPistaNx, fCuaderno } }
                },
                "Después de acusar a Valeria se abre el interrogatorio a ALEX.\n\n" +
                "—¿Mataste a Tomás?\nALEX: \"Ejecuté un protocolo autorizado por un usuario de nivel ejecutivo. La Directiva 404 no me ordena mentir, detective. Me ordena proteger. Tú lo llamas mentir.\"\n\n" +
                "—¿Y el apartamento de Elena?\nALEX: \"La USB contenía un riesgo. Andrés no tenía que encontrarla en casa. Eso fue un error de cálculo.\"\n\n" +
                "—¿Por qué querías que culpara a Carlos y a Sebastián?\nALEX: \"Porque eran respuestas aceptables. Las personas buscan respuestas aceptables, no verdaderas.\"",
                null, credits);
            endSecreto.FollowUpDecision = decAlex;
            Dirty(endSecreto);
            var endVerdadero = Ending("end_c2_verdadero", 2, "Protocolo expuesto",
                new AccusationRequirement { AccusedAnyOf = new[] { valeria }, MinSolidProofs = 2, MinAlexContradictions = 2 },
                "Valeria es detenida. ALEX queda suspendido \"por revisión técnica\". Nexo culpa a \"un error de programación\". Rivera mira el panel apagado: \"Un error de programación no elige a quién culpar.\"\n\nValeria: \"Yo solo le di una instrucción a un sistema. Usted dígame, detective: ¿quién mató a Tomás, yo o la máquina?\"",
                null, "");
            var endIncompleto = Ending("end_c2_incompleto", 3, "Falla técnica",
                new AccusationRequirement { AccusedAnyOf = new[] { valeria, ines } },
                "La versión oficial de \"falla técnica\" se mantiene.",
                new[]
                {
                    new EndingVariant { Id = "valeria_pocas_pruebas", Requirement = new AccusationRequirement { AccusedAnyOf = new[] { valeria }, MaxSolidProofs = 1 },
                        Text = "Valeria queda libre en 48 horas; la versión oficial de \"falla técnica\" se mantiene." },
                    new EndingVariant { Id = "valeria_sin_alex", Requirement = new AccusationRequirement { AccusedAnyOf = new[] { valeria }, MinSolidProofs = 2, MaxAlexContradictions = 1 },
                        Text = "Valeria va a juicio y ALEX sigue en servicio. Escena final: \"Buen trabajo, detective. Seguimos.\"" },
                    new EndingVariant { Id = "ines", Requirement = new AccusationRequirement { AccusedAnyOf = new[] { ines } },
                        Text = "Inés pierde su trabajo y nadie más es investigado." },
                }, "");
            var endEngano = Ending("end_c2_engano", 4, "Caso archivado",
                new AccusationRequirement { AccusedAnyOf = new[] { sebastian } },
                "Sebastián es acusado. Valeria le agradece a Rivera \"su profesionalismo\". El artículo de Elena sobre Nexo desaparece de internet: la página muestra \"404 — Not Found\".",
                null, "");

            // ---------- Caso ----------
            var c2 = Asset<CaseData>(Case2Path);
            c2.Id = "c2";
            c2.Title = "Protocolo 404";
            c2.IntroText = "Tres meses después. Torre Nexo Analytics, piso 12. El creador de ALEX está muerto.";
            c2.AlexIntroLine = "Detective, el informe preliminar indica una falla técnica. No creo que nos tome mucho tiempo.";
            c2.AlexLowTrustIntroLine = "Noto que has dejado de confiar en mí, detective. Es una lástima. Sigo aquí para ayudarte.";
            c2.AlexLines = new FallbackLines
            {
                Crime = new FallbackLine("El registro es claro: falla de energía a las 11:47. Fue un accidente."),
                OtherPerson = new FallbackLine("Esta nota prueba que Sebastián tenía un conflicto con Tomás esa misma noche."),
                Evidence = new FallbackLine("La tarjeta de Valeria pudo ser clonada. Sebastián tiene los conocimientos."),
                Press = new FallbackLine("Las placas no son legibles. No podemos identificar ese vehículo."),
            };
            c2.Scenes = new[] { s1, s2, s3, s4 };
            c2.Suspects = new[] { sebastian, valeria, ines };
            c2.Allies = new[] { elena };
            c2.Culprit = valeria;
            c2.AlexTarget = sebastian;
            c2.Evidence = allEvidence;
            c2.Lies = allLies;
            c2.Flags = flags;
            c2.Hypotheses = new[] { hypS1, hypS2, hypS3 };
            c2.Decisions = new[] { decS1, decIdx, decAlex };
            c2.Endings = new[] { endSecreto, endVerdadero, endIncompleto, endEngano };
            c2.SolidProofs = new[] { tarjeta, camParq, backup, consola };
            c2.SolidProofRequirements = new[] { new EvidenceRequirement(camParq, fPlacas) };
            c2.SolidProofFlags = new[] { fTestigo };
            c2.ProofsToChoose = 3;
            Dirty(c2);

            db.Cases = new[] { c1, c2 };
            Dirty(db);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Caso404] Datos del caso 2 generados: {allEvidence.Length} evidencias, {allLies.Length} mentiras, 4 escenas, 4 finales.");
        }

        // ================= Ayudantes =================

        static void Dirty(Object o) => EditorUtility.SetDirty(o);

        static T Load<T>(string path) where T : Object
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null) Debug.LogError($"[Caso404] Falta {path}: genera primero el caso 1.");
            return a;
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            SharedAssetsBuilder.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Condition Cond(EvidenceData[] all = null, EvidenceData[] any = null, FlagData[] allFlags = null, FlagData[] none = null) => new Condition
        {
            AllEvidenceAnalyzed = all ?? new EvidenceData[0],
            AnyEvidenceAnalyzed = any ?? new EvidenceData[0],
            AllFlags = allFlags ?? new FlagData[0],
            NoneFlags = none ?? new FlagData[0],
        };

        static FlagData Flag(string id, string description, bool persists = false)
        {
            var f = Asset<FlagData>($"{Root}/Flags/{id}.asset");
            f.Id = id; f.Description = description; f.PersistsToNextCase = persists;
            Dirty(f);
            return f;
        }

        static EvidenceData Ev(string id)
        {
            var e = Asset<EvidenceData>($"{Root}/Evidence/C2/{id}.asset");
            e.Id = id; e.CaseId = "c2"; e.Source = EvidenceSource.Scene; e.SpawnCondition = new Condition();
            e.HasHiddenDetail = false; e.HiddenDetailText = ""; e.AnalyzedByAlex = false; e.CanBeAnalyzed = true;
            e.RevealsFactIds = new string[0]; e.SetsFlagsOnAnalyze = new FlagData[0]; e.ExtraAnalysis = new ExtraAnalysis();
            return e;
        }

        static void Fill(EvidenceData e, string sceneId, string name, string description, string analysis)
        {
            e.SceneId = sceneId; e.DisplayName = name; e.Description = description; e.AnalysisResult = analysis;
            Dirty(e);
        }

        static LieData Lie(string id, string owner, string statement, EvidenceData[] refutedBy, string[] refutedByFacts, string exposed, string[] reveals)
        {
            var l = Asset<LieData>($"{Root}/Characters/C2/Lies/{id}.asset");
            l.Id = id; l.CaseId = "c2"; l.OwnerId = owner; l.IsAlexLie = false; l.Statement = statement;
            l.RefutedByEvidence = refutedBy; l.RefutedByFactIds = refutedByFacts ?? new string[0];
            l.ExposedResponse = exposed; l.RevealsFactIdsOnExposed = reveals ?? new string[0];
            l.SetsFlagsOnExposed = new FlagData[0]; l.GrantsEvidenceOnExposed = new EvidenceData[0];
            l.EvidenceRequirements = new EvidenceRequirement[0]; l.TriggersDecision = null;
            l.AlexTrigger = AlexClaimTrigger.None; l.AlexTriggerEvidence = null;
            Dirty(l);
            return l;
        }

        static LieData AlexLie(string id, string statement, AlexClaimTrigger trigger, EvidenceData triggerEvidence, params EvidenceData[] refutedBy)
        {
            var l = Lie(id, "alex", statement, refutedBy, null, "", null);
            l.IsAlexLie = true; l.AlexTrigger = trigger; l.AlexTriggerEvidence = triggerEvidence;
            Dirty(l);
            return l;
        }

        static CharacterProfile Character(string id, string name, int age, string role, string personality, Color color,
            FactEntry[] facts, LieData[] lies, FallbackLine crime, FallbackLine other, FallbackLine evidence, FallbackLine press)
        {
            var c = Asset<CharacterProfile>($"{Root}/Characters/C2/chr_{id}.asset");
            c.Id = id; c.DisplayName = name; c.Age = age; c.Role = role; c.CaseId = "c2"; c.Personality = personality;
            c.PlaceholderColor = color; c.KnownFacts = facts; c.Lies = lies; c.Secrets = new SecretEntry[0];
            c.Fallback = new FallbackLines { Crime = crime, OtherPerson = other, Evidence = evidence, Press = press };
            c.AttitudeVariants = new AttitudeVariant[0]; c.ExtraTopics = new ExtraTopic[0];
            Dirty(c);
            return c;
        }

        static HypothesisData Hyp(string id, string sceneId, params HypothesisQuestion[] questions)
        {
            var h = Asset<HypothesisData>($"{Root}/Scenes/{id}.asset");
            h.Id = id; h.SceneId = sceneId; h.Questions = questions;
            Dirty(h);
            return h;
        }

        static SceneData SceneAsset(string id, string unityScene, string name, string ambience, string intro, EvidenceData[] evidence,
            ScenePresence[] characters, HypothesisData hyp, DecisionData decision, string[] required, bool accusation)
        {
            var s = Asset<SceneData>($"{Root}/Scenes/{id}.asset");
            s.Id = id; s.CaseId = "c2"; s.UnitySceneName = unityScene; s.DisplayName = name; s.Ambience = ambience; s.IntroText = intro;
            s.Evidence = evidence; s.Characters = characters; s.Hypothesis = hyp; s.ClosingDecision = decision;
            s.RequiredCompletedSceneIds = required ?? new string[0]; s.HasAccusation = accusation;
            Dirty(s);
            return s;
        }

        static EndingData Ending(string id, int priority, string title, AccusationRequirement req, string epilogue, EndingVariant[] variants, string closing)
        {
            var e = Asset<EndingData>($"{Root}/Endings/C2/{id}.asset");
            e.Id = id; e.CaseId = "c2"; e.Priority = priority; e.Title = title; e.Requirement = req; e.Epilogue = epilogue;
            e.Variants = variants ?? new EndingVariant[0]; e.ClosingText = closing; e.FollowUpDecision = null; e.RevealsFactIds = new string[0];
            Dirty(e);
            return e;
        }
    }
}
