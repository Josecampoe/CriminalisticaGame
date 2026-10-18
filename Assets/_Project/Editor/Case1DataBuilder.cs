using System.Collections.Generic;
using System.IO;
using Caso404.Data;
using UnityEditor;
using UnityEngine;

namespace Caso404.EditorTools
{
    /// <summary>
    /// Crea (o actualiza, conservando los GUID) todos los assets del caso 1 a partir de Docs/GUION.md.
    /// Uso: menú Caso404/Generar datos caso 1, o -executeMethod Caso404.EditorTools.Case1DataBuilder.Build
    /// Después de generarlos, los assets son la fuente de verdad del juego.
    /// </summary>
    public static class Case1DataBuilder
    {
        const string Root = "Assets/_Project/Data";
        public const string DatabasePath = Root + "/GameDatabase.asset";
        public const string Case1Path = Root + "/Cases/c1.asset";

        [MenuItem("Caso404/Generar datos caso 1")]
        public static void Build()
        {
            // ---------- Flags ----------
            var fLlave = Flag("flag_c1_h_llave", "El jugador dedujo que el agresor entró con una llave.");
            var fDetenido = Flag("flag_c1_carlos_detenido", "Se detuvo a Carlos en dec_c1_s1 (opción A).");
            var fPistaNx = Flag("flag_c1_pista_nx", "El análisis profundo del celular de Andrés liga 'Soporte NX' con el canal de ALEX.", true);
            var fCuaderno = Flag("flag_c1_cuaderno", "El cuaderno de Elena se guardó fuera del sistema y se leyó.", true);
            var fCuadernoPerdido = Flag("flag_c1_cuaderno_perdido", "El cuaderno se registró en el sistema y ALEX lo dio por dañado.");
            var flags = new[] { fLlave, fDetenido, fPistaNx, fCuaderno, fCuadernoPerdido };

            // ---------- Evidencias (primero se crean, luego se llenan, porque se referencian entre sí) ----------
            var ventana = Ev("ev_c1_ventana"); var puerta = Ev("ev_c1_puerta"); var huella = Ev("ev_c1_huella");
            var colilla = Ev("ev_c1_colilla"); var telefono = Ev("ev_c1_telefono"); var escritorio = Ev("ev_c1_escritorio");
            var camPasillo = Ev("ev_c1_camara_pasillo");
            var reciboCafe = Ev("ev_c1_recibo_cafe"); var camCafe = Ev("ev_c1_camara_cafe"); var servilleta = Ev("ev_c1_servilleta");
            var registro = Ev("ev_c1_registro_porteria"); var reciboTienda = Ev("ev_c1_recibo_tienda"); var dvr = Ev("ev_c1_dvr");
            var camOriginal = Ev("ev_c1_camara_original"); var llavero = Ev("ev_c1_llavero"); var overol = Ev("ev_c1_overol");
            var cenizas = Ev("ev_c1_cenizas"); var portatil = Ev("ev_c1_portatil"); var celular = Ev("ev_c1_celular_andres");

            Fill(ventana, "c1_s1", "Ventana rota del balcón", "La ventana del balcón está rota. Hay vidrios por todas partes.",
                "Los vidrios cayeron hacia el balcón. La ventana se rompió desde adentro.");
            Fill(puerta, "c1_s1", "Cerradura de la puerta", "La cerradura de la puerta principal del 404.",
                "Sin marcas de forzado. Se abrió con llave.");
            Fill(huella, "c1_s1", "Huella de zapato en el balcón", "Una huella de zapato marcada con barro en el piso del balcón.",
                "Deportiva talla 42, barro del mismo día. Coincide con los zapatos de Carlos.");
            Fill(colilla, "c1_s1", "Colilla en el balcón", "Una colilla de cigarrillo en el piso del balcón.",
                "Marca de cigarrillo que fuma Carlos.");
            Fill(telefono, "c1_s1", "Teléfono bajo el sofá", "Un teléfono celular olvidado debajo del sofá.",
                "Es de Laura. Llamada al 123 a las 10:51 PM.");
            Fill(escritorio, "c1_s1", "Escritorio vacío", "El escritorio de Elena, casi vacío. En la pared hay fotos de ella.",
                "Falta el portátil (marca en el polvo y cargador). En las fotos de la pared Elena aparece siempre con un cuaderno rojo, que no está.");
            Fill(camPasillo, "c1_s1", "Grabación de la cámara del pasillo", "ALEX puede recuperar la grabación de la cámara del pasillo.",
                "ERROR 404: fragmento 10:30–10:45 no encontrado.");
            camPasillo.AnalyzedByAlex = true;

            Fill(reciboCafe, "c1_s2", "Recibo del datáfono", "Un recibo de pago con tarjeta del Café La Esquina.",
                "Pago de Carlos a las 10:28 PM. La hora la pone el banco, no se puede alterar.");
            Fill(camCafe, "c1_s2", "Video del café", "La grabación de la cámara de seguridad del café.",
                "Carlos sentado de 10:20 a 11:05.");
            Fill(servilleta, "c1_s2", "Servilleta con nota", "Una servilleta con algo escrito a mano.",
                "\"Lo siento, Ele\" con la letra de Carlos. No es prueba, da contexto emocional.");

            Fill(registro, "c1_s3", "Libro de registro", "El libro de registro de la portería del Edificio Mirador.",
                "Carlos entra 9:10, sale 9:40. No hay ninguna anotación entre 10:15 y 10:50.");
            Fill(reciboTienda, "c1_s3", "Recibo en la chaqueta de Hernán", "Un recibo arrugado en el bolsillo de la chaqueta del portero.",
                "Tienda de la esquina, 10:17 PM.");
            Fill(dvr, "c1_s3", "Grabador de video de la administración", "El grabador (DVR) de las cámaras del edificio.",
                "El grabador conserva la grabación original del pasillo.");
            Fill(camOriginal, "c1_s3", "Grabación original del pasillo", "La grabación original del pasillo del cuarto piso, sacada del DVR.",
                "10:32: una persona con overol gris y gorra abre el 404 con llave. 10:40: sale con una mochila.");
            camOriginal.Source = EvidenceSource.Unlock;
            camOriginal.SpawnCondition = Cond(all: new[] { dvr });
            Fill(llavero, "c1_s3", "Registro del llavero maestro", "La hoja de control del llavero de mantenimiento.",
                "Llave maestra retirada por A. Pardo a las 10:10 y devuelta a las 10:58.");
            Fill(overol, "c1_s3", "Overol gris", "Un overol gris colgado en el casillero de Andrés.",
                "Fragmentos de vidrio en la manga que coinciden con la ventana del 404.");
            overol.SpawnCondition = Cond(none: new[] { fDetenido });
            overol.HasHiddenDetail = true;
            overol.HiddenDetailText = "Al girarlo, algo brilla en la manga: pequeños fragmentos de vidrio.";
            Fill(cenizas, "c1_s3", "Caneca con cenizas", "Una caneca metálica con restos quemados.",
                "Tela quemada. No se puede analizar.");
            cenizas.SpawnCondition = Cond(allFlags: new[] { fDetenido });
            cenizas.CanBeAnalyzed = false;
            Fill(portatil, "c1_s3", "Portátil de Elena", "Un portátil tirado en el contenedor del parqueadero.",
                "Disco borrado a distancia a las 11:20 PM, firma de borrado \"NX-RW\". A esa hora Elena ya iba en la ambulancia.");

            Fill(celular, "c1_s4", "Celular de Andrés", "El celular que Andrés entregó \"para demostrar que no tiene nada que esconder\".",
                "Mensajes de \"Soporte NX\": \"Apto 404. Estará fuera hasta las 11. Solo la USB.\" — \"Pago confirmado.\"");
            celular.Source = EvidenceSource.Dialogue;
            celular.ExtraAnalysis = new ExtraAnalysis
            {
                Kind = ExtraAnalysisKind.Deep,
                ButtonLabel = "Análisis profundo",
                ResultText = "El número de \"Soporte NX\" corresponde al canal de soporte técnico de ALEX.",
                AlexComment = "Ese canal lo usan cientos de clientes de Nexo. Es una coincidencia estadística.",
                SetsFlags = new[] { fPistaNx }
            };

            var allEvidence = new[] { ventana, puerta, huella, colilla, telefono, escritorio, camPasillo, reciboCafe, camCafe, servilleta,
                registro, reciboTienda, dvr, camOriginal, llavero, overol, cenizas, portatil, celular };

            // ---------- Mentiras ----------
            var lCarlos = Lie("lie_c1_carlos_nofui", "carlos", "No fui al edificio esa noche.",
                new[] { registro, huella, colilla }, new[] { "f_c1_laura_gritos" },
                "¡Está bien! Sí fui. Discutimos y me fui. A las diez y media yo estaba tomando café, ¡revise!",
                new[] { "f_c1_carlos_visita", "f_c1_carlos_cafe", "f_c1_carlos_balcon" });
            var lLauraHora = Lie("lie_c1_laura_hora", "laura", "Llegué cuando ya estaba la ambulancia, como a las 11.",
                new[] { telefono }, null,
                "Está bien, llegué antes. Oí un golpe, esperé... tenía miedo. Cuando entré ya estaba en el piso.",
                new[] { "f_c1_laura_golpe", "f_c1_laura_encontro" });
            var lLauraToque = Lie("lie_c1_laura_notoque", "laura", "No toqué nada del apartamento.",
                new[] { escritorio }, null,
                "Yo... solo quería ayudarla. No me pregunte eso todavía.", null);
            var lHernan = Lie("lie_c1_hernan_nomemovi", "hernan", "No me moví de la portería en toda la noche.",
                new[] { reciboTienda, registro }, null,
                "Me fui un ratico a la tienda. Y antes de irme vi a Andrés, el de mantenimiento, en overol. A esa hora no tenía nada que hacer aquí.",
                new[] { "f_c1_hernan_tienda", "f_c1_hernan_andres" });
            var lAndresCasa = Lie("lie_c1_andres_casa", "andres", "Terminé turno a las 6 y me fui para la casa.",
                new[] { llavero, overol, camOriginal }, new[] { "f_c1_hernan_andres" },
                "Yo no tengo nada que esconder, oiga. Tome mi celular y revíselo, para que vea.", null);
            lAndresCasa.GrantsEvidenceOnExposed = new[] { celular };
            var lAndresNexo = Lie("lie_c1_andres_nexo", "andres", "No conozco a nadie de esa empresa.",
                new[] { celular }, null,
                "Yo... yo no quería que nadie saliera herido. Ella no tenía que estar ahí.", null);

            var aVentana = AlexLie("lie_c1_alex_ventana", "El agresor entró por el balcón. La huella de Carlos está justo debajo de la ventana. Es bastante claro.",
                AlexClaimTrigger.OnEvidenceAnalyzed, huella, ventana, puerta);
            var aArchivo = AlexLie("lie_c1_alex_archivo", "Lo siento, el fragmento de 10:30 a 10:45 está corrupto. ERROR 404.",
                AlexClaimTrigger.OnEvidenceAnalyzed, camPasillo, camOriginal);
            var aHoraCafe = AlexLie("lie_c1_alex_horacafe", "Las cámaras de esos cafés suelen tener la hora desfasada. Yo no confiaría en esa coartada.",
                AlexClaimTrigger.OnEvidenceAnalyzed, camCafe, reciboCafe);
            var aOverol = AlexLie("lie_c1_alex_overol", "Todo el personal del edificio usa overoles grises. No prueba nada.",
                AlexClaimTrigger.OnEvidenceFound, overol, overol);
            var aPortatil = AlexLie("lie_c1_alex_portatil", "Muchos portátiles tienen borrado automático programado. Pudo hacerlo la propia Elena.",
                AlexClaimTrigger.OnEvidenceAnalyzed, portatil, portatil);

            var allLies = new[] { lCarlos, lLauraHora, lLauraToque, lHernan, lAndresCasa, lAndresNexo, aVentana, aArchivo, aHoraCafe, aOverol, aPortatil };

            // ---------- Decisiones ----------
            var decS1 = Asset<DecisionData>($"{Root}/Decisions/C1/dec_c1_s1.asset");
            decS1.Id = "dec_c1_s1";
            decS1.Prompt = "¿Qué hacemos ahora?";
            decS1.Irreversible = true;
            decS1.AlexRecommendedOptionId = "A";
            decS1.AlexRecommendationText = "Carlos tiene motivo, estuvo aquí y mintió. Si esperamos, puede irse de la ciudad. Detengámoslo ya.";
            decS1.Options = new[]
            {
                new DecisionOption { Id = "A", Text = "Detener a Carlos de inmediato", SetsFlags = new[] { fDetenido }, AlexTrustDelta = 10, LetPlayerChooseNextScene = true,
                    ResultText = "Carlos queda detenido. Lo interrogarás en la Comisaría." },
                new DecisionOption { Id = "B", Text = "Hablar con Carlos antes de actuar", NextSceneId = "c1_s2" },
                new DecisionOption { Id = "C", Text = "Revisar primero la portería y el sótano", NextSceneId = "c1_s3" },
            };
            Dirty(decS1);

            var decCuaderno = Asset<DecisionData>($"{Root}/Decisions/C1/dec_c1_cuaderno.asset");
            decCuaderno.Id = "dec_c1_cuaderno";
            decCuaderno.Prompt = "¿Qué haces con el cuaderno de Elena?";
            decCuaderno.Irreversible = true;
            decCuaderno.Options = new[]
            {
                new DecisionOption { Id = "A", Text = "Registrarlo en el sistema para que ALEX lo analice", SetsFlags = new[] { fCuadernoPerdido }, AlexTrustDelta = 5,
                    ResultText = "Las páginas están dañadas por humedad. Contenido no recuperable (404).", ResultSpokenByAlex = true },
                new DecisionOption { Id = "B", Text = "Guardarlo fuera del sistema y leerlo tú mismo(a)", SetsFlags = new[] { fCuaderno }, AlexTrustDelta = -5,
                    ResultText = "Nota de Elena: \"NX quiere la USB. Hay algo en su asistente de IA. Contacto: Tomás Q.\"" },
            };
            Dirty(decCuaderno);

            // ---------- Personajes ----------
            var carlos = Character("carlos", "Carlos Méndez", 36, "Chef, ex pareja de Elena",
                "Impulsivo, orgulloso, se pone a la defensiva rápido. Cuando se calma, se nota que todavía quiere a Elena.",
                new Color(0.55f, 0.27f, 0.2f),
                new[] {
                    new FactEntry("f_c1_carlos_visita", "Fue a devolverle una caja con cosas, discutieron y se fue a las 9:40."),
                    new FactEntry("f_c1_carlos_cafe", "Después estuvo en el Café La Esquina hasta las 11:05."),
                    new FactEntry("f_c1_carlos_balcon", "Fumó en el balcón durante la visita."),
                },
                new[] { lCarlos },
                new FallbackLine("Yo jamás le haría daño a Elena. Jamás."),
                new FallbackLine("La vecina, Laura, siempre está pendiente de todo. Pregúntele a ella."),
                new FallbackLine("Eso no prueba nada. Mucha gente usa esos zapatos."),
                new FallbackLine("¡Está bien! Sí fui. Discutimos y me fui. A las diez y media yo estaba tomando café, ¡revise!"));

            var laura = Character("laura", "Laura Ríos", 29, "Enfermera, vecina del 403 y amiga de Elena",
                "Cálida, protectora y nerviosa. Habla rápido cuando miente.",
                new Color(0.3f, 0.45f, 0.6f),
                new[] {
                    new FactEntry("f_c1_laura_gritos", "Oyó gritos hacia las 9:30."),
                    new FactEntry("f_c1_laura_golpe", "Oyó un golpe hacia las 10:35."),
                    new FactEntry("f_c1_laura_encontro", "Encontró a Elena a las 10:48 con la puerta abierta."),
                    new FactEntry("f_c1_laura_fumar", "Sabe que Carlos siempre fumaba en el balcón."),
                    new FactEntry("f_c1_laura_cuaderno", "Elena le pidió guardar su cuaderno si algo le pasaba. Laura lo tiene."),
                },
                new[] { lLauraHora, lLauraToque },
                new FallbackLine("Elena no tenía enemigos... bueno, solo los que le traía su trabajo."),
                new FallbackLine("Carlos vino esa noche, los oí gritar. Él fumaba en el balcón, siempre.", "f_c1_laura_gritos", "f_c1_laura_fumar"),
                new FallbackLine("¿Mi teléfono? Ah... se me debió caer."),
                new FallbackLine("Está bien, llegué antes. Oí un golpe, esperé... tenía miedo. Cuando entré ya estaba en el piso."));
            laura.Secrets = new[]
            {
                new SecretEntry
                {
                    FactId = "f_c1_laura_cuaderno",
                    RevealText = "Elena me dijo: \"Si algo me pasa, guarda mi cuaderno y no confíes en nadie de la policía.\" Aquí está. Su cuaderno rojo.",
                    UnlockCondition = Cond(lies: new[] { lLauraHora }),
                    RequiresCalmApproach = true,
                    TriggersDecision = decCuaderno
                }
            };
            Dirty(laura);

            var hernan = Character("hernan", "Don Hernán Gómez", 63, "Portero del Edificio Mirador",
                "Amable, charlatán, orgulloso de \"no perderse nada\". Le avergüenza haber dejado su puesto.",
                new Color(0.45f, 0.4f, 0.3f),
                new[] {
                    new FactEntry("f_c1_hernan_carlos", "Carlos entró a las 9:10 y salió a las 9:40."),
                    new FactEntry("f_c1_hernan_andres", "Vio a Andrés con overol entrar por el sótano a las 10:05, algo raro de noche."),
                    new FactEntry("f_c1_hernan_tienda", "Estuvo en la tienda de 10:15 a 10:50."),
                },
                new[] { lHernan },
                new FallbackLine("En treinta años en este edificio nunca había pasado algo así, doctor(a)."),
                new FallbackLine("El señor Carlos entró y salió, ahí está firmado. Yo anoto todo.", "f_c1_hernan_carlos"),
                new FallbackLine("Ese recibo... bueno, había partido, ¿sí me entiende?"),
                new FallbackLine("Me fui un ratico a la tienda. Y antes de irme vi a Andrés, el de mantenimiento, en overol. A esa hora no tenía nada que hacer aquí."));

            var andres = Character("andres", "Andrés Pardo", 41, "Técnico de mantenimiento del edificio",
                "Muy servicial y amable al principio. Tiene deudas y está desesperado. Se pone nervioso y se contradice cuando lo presionan con pruebas.",
                new Color(0.4f, 0.42f, 0.45f),
                new[] {
                    new FactEntry("f_c1_andres_pago", "Le pagó \"Soporte NX\"."),
                },
                new[] { lAndresCasa, lAndresNexo },
                new FallbackLine("Pobre señora Elena. Ojalá agarren al que fue."),
                new FallbackLine("Yo vi al exnovio por aquí varias veces, eso sí."),
                new FallbackLine("Ese overol lo usa todo el personal, oiga."),
                new FallbackLine("Yo... yo no quería que nadie saliera herido. Ella no tenía que estar ahí."));
            andres.Secrets = new[]
            {
                new SecretEntry { FactId = "f_c1_andres_pago", RevealText = "Alguien llamado \"Soporte NX\" me pagó.", OnlyInEnding = true }
            };
            Dirty(andres);

            // ---------- ALEX ----------
            var alex = Asset<AlexProfile>($"{Root}/Characters/chr_alex.asset");
            alex.Id = "alex";
            alex.DisplayName = "ALEX";
            alex.Role = "Asistente Lógico de EXpedientes (Nexo Analytics)";
            alex.CaseId = "";
            alex.Personality = "IA forense. Con confianza alta es cálido, seguro y usa \"nosotros\". Con confianza media es formal y prudente. Con confianza baja es evasivo, frío y responde corto. Nunca niega un resultado que el jugador ya ve: solo lo pone en duda.";
            alex.PlaceholderColor = new Color(0.31f, 0.82f, 0.77f);
            alex.StartTrust = 60;
            alex.HighTrustThreshold = 60;
            alex.LowTrustThreshold = 30;
            alex.AcceptTrustDelta = 5;
            alex.QuestionSuccessTrustDelta = -10;
            alex.QuestionedSuccessHigh = "Tienes razón. Mi análisis fue apresurado. Lo registro.";
            alex.QuestionedSuccessLow = "Registrado.";
            alex.QuestionedFail = "Esa evidencia no contradice mi análisis, detective.";
            alex.AcceptedLine = "Bien. Seguimos con esa línea.";
            alex.Lies = new[] { aVentana, aArchivo, aHoraCafe, aOverol, aPortatil };
            Dirty(alex);

            // ---------- Hipótesis ----------
            var hypS1 = Asset<HypothesisData>($"{Root}/Scenes/hyp_c1_s1.asset");
            hypS1.Id = "hyp_c1_s1"; hypS1.SceneId = "c1_s1";
            hypS1.Questions = new[]
            {
                new HypothesisQuestion("hyp_c1_s1_q1", "¿Cómo entró el agresor?", false, new[]
                {
                    new HypothesisOption("ventana", "Por la ventana del balcón", false),
                    new HypothesisOption("forzando", "Forzando la puerta", false),
                    new HypothesisOption("llave", "Con una llave", true, Cond(all: new[] { ventana, puerta })),
                }, new[] { fLlave }),
                new HypothesisQuestion("hyp_c1_s1_q2", "¿Quién estuvo en el apartamento esa noche?", true, new[]
                {
                    new HypothesisOption("carlos", "Carlos", true, Cond(all: new[] { huella })),
                    new HypothesisOption("laura", "Laura", true, Cond(all: new[] { telefono })),
                    new HypothesisOption("desconocido", "Alguien desconocido", true),
                }),
            };
            Dirty(hypS1);

            var hypS2 = Asset<HypothesisData>($"{Root}/Scenes/hyp_c1_s2.asset");
            hypS2.Id = "hyp_c1_s2"; hypS2.SceneId = "c1_s2";
            hypS2.Questions = new[]
            {
                new HypothesisQuestion("hyp_c1_s2_q1", "¿Carlos pudo estar en el 404 a las 10:32?", false, new[]
                {
                    new HypothesisOption("si", "Sí", false),
                    new HypothesisOption("no", "No, estaba en el café", true, Cond(all: new[] { reciboCafe })),
                }),
            };
            Dirty(hypS2);

            var hypS3 = Asset<HypothesisData>($"{Root}/Scenes/hyp_c1_s3.asset");
            hypS3.Id = "hyp_c1_s3"; hypS3.SceneId = "c1_s3";
            hypS3.Questions = new[]
            {
                new HypothesisQuestion("hyp_c1_s3_q1", "¿Quién usó la llave maestra?", false, new[]
                {
                    new HypothesisOption("hernan", "Don Hernán", false),
                    new HypothesisOption("andres", "Andrés", true, Cond(all: new[] { llavero })),
                }),
                new HypothesisQuestion("hyp_c1_s3_q2", "¿Por qué se rompió la ventana?", false, new[]
                {
                    new HypothesisOption("entrar", "Para entrar", false),
                    new HypothesisOption("simular", "Para simular un robo", true, Cond(all: new[] { ventana }, any: new[] { overol, camOriginal })),
                }),
            };
            Dirty(hypS3);

            // ---------- Escenas ----------
            var s1 = SceneAsset("c1_s1", "Apartamento404", "Apartamento 404 — Edificio Mirador",
                "Sala desordenada, ventana del balcón rota, escritorio vacío, luz de mañana gris.",
                "Viernes, 8:00 AM. Edificio Mirador, apartamento 404. Elena Vargas, periodista, fue encontrada inconsciente anoche. Le robaron su portátil.",
                new[] { ventana, puerta, huella, colilla, telefono, escritorio, camPasillo },
                new[] { new ScenePresence(laura) }, hypS1, decS1, null);
            var s2 = SceneAsset("c1_s2", "CafeLaEsquina", "Café La Esquina",
                "Café pequeño de barrio, de noche, olor a tinto, televisor con un partido repetido.", "",
                new[] { reciboCafe, camCafe, servilleta },
                new[] { new ScenePresence(carlos, Cond(none: new[] { fDetenido })) }, hypS2, null, null);
            var s3 = SceneAsset("c1_s3", "PorteriaSotano", "Portería y sótano del Edificio Mirador",
                "Caseta de portería con libro de registro, pasillo de sótano con casilleros, cuarto de mantenimiento, parqueadero con contenedor.", "",
                new[] { registro, reciboTienda, dvr, camOriginal, llavero, overol, cenizas, portatil },
                new[] { new ScenePresence(hernan), new ScenePresence(andres) }, hypS3, null, null);
            var s4 = SceneAsset("c1_s4", "Comisaria", "Comisaría",
                "Oficina con tablero de corcho y sala de interrogatorio.", "",
                new[] { celular },
                new[] { new ScenePresence(carlos), new ScenePresence(laura), new ScenePresence(hernan), new ScenePresence(andres) },
                null, null, new[] { "c1_s2", "c1_s3" });

            // ---------- Finales ----------
            const string closing = "Tres meses después...";
            var endVerdadero = Ending("end_c1_verdadero", 1, "Archivo encontrado",
                new AccusationRequirement { AccusedAnyOf = new[] { andres }, MinSolidProofs = 2, MinAlexContradictions = 2 },
                "Andrés confiesa: alguien llamado \"Soporte NX\" le pagó. Rivera anota en su libreta: \"ALEX se equivocó demasiadas veces, y siempre en la misma dirección.\" Elena sigue en coma, pero estable.",
                null, closing);
            var endIncompleto = Ending("end_c1_incompleto", 2, "Caso cerrado, preguntas abiertas",
                new AccusationRequirement { AccusedAnyOf = new[] { andres, laura, hernan } },
                "El caso se cierra, pero quedan preguntas abiertas.",
                new[]
                {
                    new EndingVariant { Id = "andres_pocas_pruebas", Requirement = new AccusationRequirement { AccusedAnyOf = new[] { andres }, MaxSolidProofs = 1 },
                        Text = "Andrés queda libre por falta de pruebas." },
                    new EndingVariant { Id = "andres_sin_alex", Requirement = new AccusationRequirement { AccusedAnyOf = new[] { andres }, MinSolidProofs = 2, MaxAlexContradictions = 1 },
                        Text = "Andrés va a juicio y nadie pregunta quién le pagó." },
                    new EndingVariant { Id = "inocente", Requirement = new AccusationRequirement { AccusedAnyOf = new[] { laura, hernan } },
                        Text = "La acusación se cae en una semana y el caso queda abierto." },
                }, closing);
            var endEngano = Ending("end_c1_engano", 3, "La versión de ALEX",
                new AccusationRequirement { AccusedAnyOf = new[] { carlos } },
                "Carlos va a la cárcel. ALEX: \"Excelente trabajo. Formamos un gran equipo.\" En la última imagen, la cámara del café muestra a Carlos a las 10:32. Nadie la vuelve a mirar.",
                null, closing);

            // ---------- Caso ----------
            var c1 = Asset<CaseData>(Case1Path);
            c1.Id = "c1";
            c1.Title = "Apartamento 404";
            c1.IntroText = "Viernes, 8:00 AM. Edificio Mirador, apartamento 404. Elena Vargas, periodista, fue encontrada inconsciente anoche. Le robaron su portátil.";
            c1.AlexIntroLine = "Buenos días, detective. Ya preparé el expediente. Empecemos.";
            c1.AlexLines = new FallbackLines
            {
                Crime = new FallbackLine("Las pruebas apuntan a Carlos: motivo, oportunidad y una huella en la escena."),
                OtherPerson = new FallbackLine("Carlos tiene motivo, estuvo aquí y mintió."),
                Evidence = new FallbackLine("Es posible que la rompiera al salir, para confundirnos."),
                Press = new FallbackLine("Las fallas de almacenamiento son comunes en equipos antiguos. No le daría importancia."),
            };
            c1.Scenes = new[] { s1, s2, s3, s4 };
            c1.Suspects = new[] { carlos, laura, hernan, andres };
            c1.Culprit = andres;
            c1.AlexTarget = carlos;
            c1.Evidence = allEvidence;
            c1.Lies = allLies;
            c1.Flags = flags;
            c1.Hypotheses = new[] { hypS1, hypS2, hypS3 };
            c1.Decisions = new[] { decS1, decCuaderno };
            c1.Endings = new[] { endVerdadero, endIncompleto, endEngano };
            c1.SolidProofs = new[] { llavero, overol, camOriginal, celular };
            c1.ProofsToChoose = 3;
            Dirty(c1);

            var db = Asset<GameDatabase>(DatabasePath);
            db.Cases = new[] { c1 };
            db.Alex = alex;
            db.IntroText = "Valdora, 2031. La Unidad de Criminalística estrena compañero: ALEX, una inteligencia artificial que nunca duerme, nunca olvida y nunca se equivoca. O eso dice el folleto.";
            Dirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Caso404] Datos del caso 1 generados: {allEvidence.Length} evidencias, {allLies.Length} mentiras, 4 escenas, 3 finales.");
        }

        // ================= Ayudantes =================

        static void Dirty(Object o) => EditorUtility.SetDirty(o);

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            var parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }

        static Condition Cond(EvidenceData[] all = null, EvidenceData[] any = null, FlagData[] allFlags = null,
            FlagData[] none = null, LieData[] lies = null) => new Condition
        {
            AllEvidenceAnalyzed = all ?? new EvidenceData[0],
            AnyEvidenceAnalyzed = any ?? new EvidenceData[0],
            AllFlags = allFlags ?? new FlagData[0],
            NoneFlags = none ?? new FlagData[0],
            ExposedLies = lies ?? new LieData[0],
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
            var e = Asset<EvidenceData>($"{Root}/Evidence/C1/{id}.asset");
            e.Id = id;
            e.CaseId = "c1";
            // Valores por defecto: se reinician en cada generación para que el asset refleje el guion.
            e.Source = EvidenceSource.Scene;
            e.SpawnCondition = new Condition();
            e.HasHiddenDetail = false;
            e.HiddenDetailText = "";
            e.AnalyzedByAlex = false;
            e.CanBeAnalyzed = true;
            e.RevealsFactIds = new string[0];
            e.SetsFlagsOnAnalyze = new FlagData[0];
            e.ExtraAnalysis = new ExtraAnalysis();
            return e;
        }

        static void Fill(EvidenceData e, string sceneId, string name, string description, string analysis)
        {
            e.SceneId = sceneId; e.DisplayName = name; e.Description = description; e.AnalysisResult = analysis;
            Dirty(e);
        }

        static LieData Lie(string id, string owner, string statement, EvidenceData[] refutedBy, string[] refutedByFacts,
            string exposedResponse, string[] revealsOnExposed)
        {
            var l = Asset<LieData>($"{Root}/Characters/C1/Lies/{id}.asset");
            l.Id = id; l.CaseId = "c1"; l.OwnerId = owner; l.IsAlexLie = false; l.Statement = statement;
            l.RefutedByEvidence = refutedBy;
            l.RefutedByFactIds = refutedByFacts ?? new string[0];
            l.ExposedResponse = exposedResponse;
            l.RevealsFactIdsOnExposed = revealsOnExposed ?? new string[0];
            l.SetsFlagsOnExposed = new FlagData[0];
            l.GrantsEvidenceOnExposed = new EvidenceData[0];
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
            var c = Asset<CharacterProfile>($"{Root}/Characters/C1/chr_{id}.asset");
            c.Id = id; c.DisplayName = name; c.Age = age; c.Role = role; c.CaseId = "c1"; c.Personality = personality;
            c.PlaceholderColor = color; c.KnownFacts = facts; c.Lies = lies; c.Secrets = new SecretEntry[0];
            c.Fallback = new FallbackLines { Crime = crime, OtherPerson = other, Evidence = evidence, Press = press };
            c.AttitudeVariants = new AttitudeVariant[0];
            Dirty(c);
            return c;
        }

        static SceneData SceneAsset(string id, string unityScene, string name, string ambience, string intro, EvidenceData[] evidence,
            ScenePresence[] characters, HypothesisData hyp, DecisionData decision, string[] required)
        {
            var s = Asset<SceneData>($"{Root}/Scenes/{id}.asset");
            s.Id = id; s.CaseId = "c1"; s.UnitySceneName = unityScene; s.DisplayName = name; s.Ambience = ambience; s.IntroText = intro;
            s.Evidence = evidence; s.Characters = characters; s.Hypothesis = hyp; s.ClosingDecision = decision;
            s.RequiredCompletedSceneIds = required ?? new string[0];
            Dirty(s);
            return s;
        }

        static EndingData Ending(string id, int priority, string title, AccusationRequirement req, string epilogue, EndingVariant[] variants, string closing)
        {
            var e = Asset<EndingData>($"{Root}/Endings/C1/{id}.asset");
            e.Id = id; e.CaseId = "c1"; e.Priority = priority; e.Title = title; e.Requirement = req; e.Epilogue = epilogue;
            e.Variants = variants ?? new EndingVariant[0]; e.ClosingText = closing; e.FollowUpDecision = null;
            Dirty(e);
            return e;
        }
    }
}
