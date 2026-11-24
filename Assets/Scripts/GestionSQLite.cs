using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SQLite;
using Vuforia;

public class GestionSQLite : MonoBehaviour
{
    public static GestionSQLite Instancia { get; private set; }

    private string rutaDB;

    [Header("Ficha de la estación (arrastra aquí el objeto Interfaz)")]
    public DetalleEstacionController detalle;

    [Header("Tamaño de los modelos en AR")]
    [Tooltip("Tamaño máximo en metros para que ambos modelos tengan una escala visual similar y quepan cómodos en el celular.")]
    [Range(0.3f, 2.0f)] public float tamanoObjetivoMetros = 0.80f;

    [Header("Posición y Flotación AR")]
    [Tooltip("Altura en metros sobre el suelo a la que flotará el modelo para que la gente no tenga que agacharse a verlo.")]
    [Range(0.2f, 1.5f)] public float alturaFlotando = 0.85f;

    [Tooltip("Distancia en metros hacia el usuario para no tener que dar pasos hacia adelante.")]
    [Range(0f, 0.8f)] public float acercarHaciaCamara = 0.25f;

    AnchorBehaviour anclaTemplate;
    PlaneFinderBehaviour detectorPiso;
    ContentPositioningBehaviour posicionContenido;
    GameObject prefabPendiente;
    GameObject anclaColocadaActual;
    bool esperandoPiso;
    bool eventosRegistrados;

    [UnityEngine.Scripting.Preserve]
    public class FilaEstacion
    {
        public string prefab { get; set; }
        public string nombre_estacion { get; set; }
        public string descripcion { get; set; }
        public string ficha_informativa { get; set; }
        public string recursos_multimedia { get; set; }
    }

    void Awake()
    {
        Instancia = this;
    }

    void Start()
    {
        ConfigurarGroundPlane();

        try
        {
            rutaDB = Path.Combine(Application.persistentDataPath, "registro_interacciones.db");
            CrearTablasIniciales();
            InsertarEstacionesDePrueba();
            Debug.Log("[DIAG] BD lista en: " + rutaDB);
        }
        catch (System.Exception e)
        {
            LogDiag("Start", e);
        }
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;

        if (eventosRegistrados)
        {
            if (detectorPiso != null)
            {
                detectorPiso.OnAutomaticHitTest.RemoveListener(AlDetectarPiso);
                detectorPiso.OnInteractiveHitTest.RemoveListener(AlDetectarPiso);
            }
            if (posicionContenido != null)
            {
                posicionContenido.OnContentPlaced.RemoveListener(AlColocarContenidoEnAncla);
            }
            eventosRegistrados = false;
        }
    }

    private void LogDiag(string donde, System.Exception e)
    {
        Debug.Log("[DIAG] " + donde + " TIPO: " + e.GetType().FullName);
        Debug.Log("[DIAG] " + donde + " MENSAJE: " + e.Message);
        var interno = e.InnerException;
        while (interno != null)
        {
            Debug.Log("[DIAG] " + donde + " INTERNO: " + interno.GetType().FullName + " - " + interno.Message);
            interno = interno.InnerException;
        }
    }

    private void CrearTablasIniciales()
    {
        using (var db = new SQLiteConnection(rutaDB))
        {
            db.Execute(@"CREATE TABLE IF NOT EXISTS SESION_ANONIMA (
                    id_sesion VARCHAR(36) PRIMARY KEY,
                    fecha_inicio DATETIME,
                    sincronizacion BOOLEAN,
                    fecha_sincronizada DATE
                )");

            db.Execute(@"CREATE TABLE IF NOT EXISTS ESTACION (
                    id_estacion INTEGER PRIMARY KEY,
                    nombre_estacion VARCHAR(50),
                    codigo_qr VARCHAR(255),
                    descripcion VARCHAR(150),
                    ficha_informativa VARCHAR(500),
                    recursos_multimedia VARCHAR(255),
                    nombre_modelo VARCHAR(20),
                    prefab VARCHAR(255),
                    formato VARCHAR(10)
                )");

            db.Execute(@"CREATE TABLE IF NOT EXISTS REGISTRO_INTERACCION (
                    id_interaccion INTEGER PRIMARY KEY AUTOINCREMENT,
                    id_sesion VARCHAR(36),
                    id_estacion INTEGER,
                    tiempo_inicio DATETIME,
                    tiempo_retencion INTEGER,
                    FOREIGN KEY(id_sesion) REFERENCES SESION_ANONIMA(id_sesion),
                    FOREIGN KEY(id_estacion) REFERENCES ESTACION(id_estacion)
                )");

            db.Execute(@"CREATE TABLE IF NOT EXISTS ENCUESTA_VISITANTE (
                    id_encuesta INTEGER PRIMARY KEY AUTOINCREMENT,
                    id_sesion VARCHAR(36),
                    origen VARCHAR(50),
                    rango_edad VARCHAR(20),
                    calificacion INTEGER,
                    comentarios VARCHAR(200),
                    fecha_respuesta DATE,
                    sincronizacion_encuesta INTEGER DEFAULT 0,
                    FOREIGN KEY(id_sesion) REFERENCES SESION_ANONIMA(id_sesion)
                )");
        }
    }

    private void InsertarEstacionesDePrueba()
    {
        using (var db = new SQLiteConnection(rutaDB))
        {
            db.Execute(@"INSERT OR REPLACE INTO ESTACION 
                         (id_estacion, nombre_estacion, codigo_qr, descripcion, ficha_informativa, recursos_multimedia, nombre_modelo, prefab, formato) 
                         VALUES (1, 
                         'Estación 1: El Ciclo del Agua', 
                         'QR_AGUA_01', 
                         'El origen de la vida (H2O). Molécula formada por dos átomos de hidrógeno y uno de oxígeno.', 
                         'El agua modela la Tierra y provee energía. En su ciclo de permanente transformación, posee tres estados: sólido, líquido y gaseoso. Los ríos fluyen continuamente desde la alta montaña hasta desembocar en el mar, lagos u otros ríos.', 
                         'Modelos3D/Audios/Estacion1_Agua', 
                         'CicloDeAguaPrefab', 
                         'Modelos3D/Estacion1/CicloDeAguaPrefab', 
                         '.prefab')");

            db.Execute(@"INSERT OR REPLACE INTO ESTACION 
                         (id_estacion, nombre_estacion, codigo_qr, descripcion, ficha_informativa, recursos_multimedia, nombre_modelo, prefab, formato) 
                         VALUES (2, 
                         'Estación 2: El Agua, Origen de la Vida', 
                         'QR_VIDA_02', 
                         'De los primeros organismos marinos a los vertebrados terrestres.', 
                         'Hace cuatro mil millones de años se originó la vida en el mar.

De los peces a la tierra firme:
Los primeros vertebrados fueron los peces. Al surgir los continentes, algunos emigraron a la tierra adaptándose a su nuevo medio: sus aletas lobuladas se convirtieron en patas y su cola se volvió más larga y musculosa.

Así surgieron los anfibios (tetrápodos), combinando respiración pulmonar y branquial. Viven en tierra pero regresan al agua a criar. Más tarde, los reptiles dominaron la era Mesozoica; en el Jurásico surgieron las primeras aves con dientes y cola emplumada, seguidas por los mamíferos vivíparos de temperatura constante.

Transición evolutiva (fósiles mar a tierra):
Eusthenopteron -> Panderichthys -> Tiktaalik (período de transición) -> Acanthostega -> Ichthyostega.

El agua en el cuerpo humano:
Así como el 75% del planeta está cubierto por agua, el 70% de nuestro cuerpo está formado por ella:
• Sangre: 82%
• Cerebro y músculos: 75%
• Huesos: 22%

Funciones vitales:
Transporta oxígeno y nutrientes a las células, transmite impulsos nerviosos, regula la temperatura corporal, amortigua las articulaciones, ayuda a convertir alimentos en energía y elimina toxinas.', 
                         'Modelos3D/Audios/Estacion2', 
                         'OrigenDeLaVidaPrefab', 
                         'Modelos3D/Estacion2/OrigenDeLaVidaPrefab', 
                         '.prefab')");

            db.Execute(@"INSERT OR REPLACE INTO ESTACION 
                         (id_estacion, nombre_estacion, codigo_qr, descripcion, ficha_informativa, recursos_multimedia, nombre_modelo, prefab, formato) 
                         VALUES (3, 
                         'Estación 3: Los Peces', 
                         'QR_PECES_03', 
                         'Anatomía y clasificación de peces de agua dulce.', 
                         'Existen tres clases de peces:
• Agnatos o peces sin mandíbulas.
• Condrictos o peces cartilaginosos.
• Osteictos o peces óseos.

Poseen esqueleto óseo y espina dorsal; respiran mediante branquias, nadan con aletas y su cuerpo está protegido por escamas. Son ovíparos, se alimentan de peces más pequeños, algas e insectos.', 
                         'Modelos3D/Audios/Estacion3', 
                         'LosPecesPrefab', 
                         'Modelos3D/Estacion3/LosPecesPrefab', 
                         '.prefab')");

            db.Execute(@"INSERT OR REPLACE INTO ESTACION 
                         (id_estacion, nombre_estacion, codigo_qr, descripcion, ficha_informativa, recursos_multimedia, nombre_modelo, prefab, formato) 
                         VALUES (4, 
                         'Estación 4: Los Peces de la Cuenca del Biobío', 
                         'QR_BIOBIO_04', 
                         'Biodiversidad y especies endémicas de la cuenca del río Biobío.', 
                         'La cuenca del Biobío es hogar de peces nativos, varios exclusivos de Chile y de tamaño muy pequeño. Muchos están amenazados.

                            Peces nativos
                            • Bagrecito (Bullockia maldonadoi): 7 cm. Vive en ríos andinos del centro y sur de Chile. En peligro de extinción.
                            • Tollo de agua dulce (Diplomystes nahuelbutaensis): 7 cm. Habita sectores caudalosos y profundos de ríos entre el Maule y la Araucanía. En peligro de extinción.
                            • Carmelita de Concepción (Percilia irwini): 7 cm. También llamada coloradita. Vive en ríos y lagos de la región del Biobío, desplazándose sobre las piedras. En peligro de extinción.
                            • Bagre chico (Trichomycterus areolatus): 15 cm. Conocido como pez gato o bagre de centro, tiene amplia distribución en Chile. Vive bajo piedras en aguas calmas y litoral arenoso. Vulnerable.
                            • Pejerrey chileno (Basilichthys australis): 30 cm. Prefiere aguas oxigenadas de baja velocidad. Casi amenazado.
                            • Perca trucha (Percichthys trucha): 35 cm. Vive en lagos y zonas bajas de los ríos. Preocupación menor.
                            • Puye (Galaxias maculatus): 5 cm. Abunda en esteros poco profundos con hojas y ramas. Preocupación menor.

                            Peces introducidos
                            • Trucha arcoíris (Oncorhynchus mykiss): hasta 60 cm. Conocida como cabeza de acero, vive en ríos, arroyos y lagos de aguas frías y transparentes.
                            • Trucha fario (Salmo trutta fario): hasta 100 cm. Es la trucha café, de las zonas altas de los ríos donde el agua es limpia y oxigenada.

                            Las especies introducidas pueden desplazar a los peces nativos.

                            Estado de conservación según el Ministerio del Medio Ambiente (2013).', 
                         'Modelos3D/Audios/Estacion4', 
                         'BiobioPrefab', 
                         'Modelos3D/Estacion4/BiobioPrefab', 
                         '.prefab')");

            db.Execute(@"INSERT OR REPLACE INTO ESTACION 
                         (id_estacion, nombre_estacion, codigo_qr, descripcion, ficha_informativa, recursos_multimedia, nombre_modelo, prefab, formato) 
                         VALUES (5, 
                         'Estación 5: La Energía del Agua, un Río de Vida', 
                         'QR_ENERGIA_05', 
                         'Aprovechamiento hidroeléctrico, sustentabilidad y desarrollo.', 
                         'El agua de un río no solo mueve turbinas: también sostiene la vida a su alrededor. Un río limpio y con caudal estable alimenta a peces, aves acuáticas y mamíferos como el zorro, que baja a beber a sus orillas.

                            La energía hidroeléctrica aprovecha el flujo del agua para generar electricidad sin quemar combustibles, por eso es una fuente renovable. Pero su desarrollo debe cuidar el río: mantener un caudal ecológico y permitir que la fauna se siga moviendo y reproduciendo.

                            Esta estación muestra ese equilibrio: producir energía y proteger la vida que depende del río.', 
                         'Modelos3D/Audios/Estacion5', 
                         'EnergiaAguaPrefab', 
                         'Modelos3D/Estacion5/EnergiaAguaPrefab', 
                         '.prefab')");
        }
    }

    public bool ProcesarEscaneoQR(string textoEscaneadoPorVuforia)
    {
        try
        {
            return ProcesarEscaneoQRInterno(textoEscaneadoPorVuforia);
        }
        catch (System.Exception e)
        {
            LogDiag("ProcesarEscaneoQR", e);
            return false;
        }
    }

    public static string NormalizarCodigoQR(string entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada)) return "";
        string s = entrada.Trim().ToUpperInvariant();

        // 1. Detección por número (prioridad infalible: si tiene un 5, es la Estación 5 sin importar qué palabras contenga)
        if (s.Contains("5") || s == "CINCO" || s.Contains("QUINTO") || s.Contains("QUINTA"))
            return "QR_ENERGIA_05";

        if (s.Contains("4") || s == "CUATRO" || s.Contains("CUARTO") || s.Contains("CUARTA"))
            return "QR_BIOBIO_04";

        if (s.Contains("3") || s == "TRES" || s.Contains("TERCERO") || s.Contains("TERCERA"))
            return "QR_PECES_03";

        if (s.Contains("2") || s == "DOS" || s.Contains("SEGUNDO") || s.Contains("SEGUNDA"))
            return "QR_VIDA_02";

        if (s.Contains("1") || s == "UNO" || s == "PRIMERO" || s.Contains("PRIMERA"))
            return "QR_AGUA_01";

        // 2. Detección por palabras clave (en caso de que el QR físico no contenga ningún dígito)
        // Estación 5: Energía / Hidroeléctrica / Central / Turbinas / Sustentabilidad / Río de Vida
        if (s.Contains("ENERG") || s.Contains("HIDRO") || s.Contains("CENTRAL") ||
            s.Contains("TURBIN") || s.Contains("SUSTENTAB") ||
            (s.Contains("RIO") && !s.Contains("BIO")))
            return "QR_ENERGIA_05";

        // Estación 4: Peces del Biobío
        if (s.Contains("BIO") || s.Contains("BAGRE") || s.Contains("TOLLO") || s.Contains("CARMELITA"))
            return "QR_BIOBIO_04";

        // Estación 3: Peces
        if (s.Contains("PECE") || s.Contains("PEZ"))
            return "QR_PECES_03";

        // Estación 2: Origen de la Vida
        if (s.Contains("ORIGEN") || s.Contains("VIDA") || s.Contains("FOSIL") || s.Contains("EVOLUC"))
            return "QR_VIDA_02";

        // Estación 1: Ciclo del Agua
        if (s.Contains("AGUA") || s.Contains("CICLO"))
            return "QR_AGUA_01";

        return s;
    }

    public static int ObtenerNumeroEstacion(string codigoQR)
    {
        string norm = NormalizarCodigoQR(codigoQR);
        switch (norm)
        {
            case "QR_AGUA_01": return 1;
            case "QR_VIDA_02": return 2;
            case "QR_PECES_03": return 3;
            case "QR_BIOBIO_04": return 4;
            case "QR_ENERGIA_05": return 5;
            default: return 0;
        }
    }

    private bool ProcesarEscaneoQRInterno(string textoEscaneadoPorVuforia)
    {
        if (string.IsNullOrWhiteSpace(textoEscaneadoPorVuforia) || textoEscaneadoPorVuforia.Length > 100)
        {
            Debug.LogWarning("QR inválido.");
            return false;
        }

        string textoBuscado = NormalizarCodigoQR(textoEscaneadoPorVuforia);

        // Control secuencial estricto del sendero (1 -> 2 -> 3 -> 4 -> 5)
        int numEstacion = ObtenerNumeroEstacion(textoBuscado);
        if (numEstacion > 1)
        {
            bool yaDescubierta = PlayerPrefs.GetInt("estacion_" + numEstacion, 0) == 1;
            bool anteriorDescubierta = PlayerPrefs.GetInt("estacion_" + (numEstacion - 1), 0) == 1;

            if (!yaDescubierta && !anteriorDescubierta)
            {
                Debug.LogWarning($"[DIAG] Escaneo bloqueado: Se intentó escanear Estación {numEstacion} sin haber completado la Estación {numEstacion - 1}.");
                if (PasaporteController.Instancia != null)
                {
                    PasaporteController.Instancia.MostrarMensaje($"¡Estación {numEstacion} bloqueada! Primero debes escanear la Estación {numEstacion - 1}.", true);
                }
                return false;
            }
        }

        using (var db = new SQLiteConnection(rutaDB))
        {
            var filas = db.Query<FilaEstacion>(
                "SELECT prefab, nombre_estacion, descripcion, ficha_informativa, recursos_multimedia " +
                "FROM ESTACION WHERE codigo_qr = ?",
                textoBuscado);

            if (filas.Count == 0)
            {
                Debug.LogWarning("[DIAG] QR no encontrado en la BD: " + textoEscaneadoPorVuforia + " (normalizado: " + textoBuscado + "). Reintentando inserción...");
                try
                {
                    InsertarEstacionesDePrueba();
                    filas = db.Query<FilaEstacion>(
                        "SELECT prefab, nombre_estacion, descripcion, ficha_informativa, recursos_multimedia " +
                        "FROM ESTACION WHERE codigo_qr = ?",
                        textoBuscado);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("[DIAG] Excepción al reintentar inserción: " + ex.Message);
                }
            }

            if (filas.Count == 0)
            {
                // Fallback directo para Estación 5 si la base de datos en el móvil no tiene el registro
                if (textoBuscado == "QR_ENERGIA_05")
                {
                    var mod5 = Resources.Load<GameObject>("Modelos3D/Estacion5/EnergiaAguaPrefab");
                    if (mod5 != null) PrepararModeloParaPiso(mod5);

                    if (detalle != null)
                    {
                        detalle.Mostrar(
                            "Estación 5: La Energía del Agua, un Río de Vida",
                            "Aprovechamiento hidroeléctrico, sustentabilidad y desarrollo.",
                            "El agua de un río no solo mueve turbinas: también sostiene la vida a su alrededor. Un río limpio y con caudal estable alimenta a peces, aves acuáticas y mamíferos como el zorro, que baja a beber a sus orillas.\n\nLa energía hidroeléctrica aprovecha el flujo del agua para generar electricidad sin quemar combustibles, por eso es una fuente renovable. Pero su desarrollo debe cuidar el río: mantener un caudal ecológico y permitir que la fauna se siga moviendo y reproduciendo.\n\nEsta estación muestra ese equilibrio: producir energía y proteger la vida que depende del río.",
                            "Modelos3D/Audios/Estacion5",
                            null,
                            textoBuscado);
                        return true;
                    }
                }
                // Fallback directo para Estación 4
                if (textoBuscado == "QR_BIOBIO_04")
                {
                    if (detalle != null)
                    {
                        detalle.Mostrar(
                            "Estación 4: Los Peces de la Cuenca del Biobío",
                            "Biodiversidad y especies endémicas de la cuenca del río Biobío.",
                            "La cuenca del Biobío es hogar de peces nativos, varios exclusivos de Chile y de tamaño muy pequeño. Muchos están amenazados.\n\nPeces nativos:\n• Bagrecito: En peligro de extinción.\n• Tollo de agua dulce: En peligro de extinción.\n• Carmelita de Concepción: En peligro de extinción.\n• Bagre chico: Vulnerable.\n• Pejerrey chileno: Casi amenazado.\n• Perca trucha: Preocupación menor.\n• Puye: Preocupación menor.\n\nPeces introducidos:\n• Trucha arcoíris y Trucha fario.",
                            "Modelos3D/Audios/Estacion4",
                            null,
                            textoBuscado);
                        return true;
                    }
                }

                Debug.LogWarning("[DIAG] QR no encontrado en la BD ni en fallback: " + textoEscaneadoPorVuforia);
                return false;
            }

            FilaEstacion fila = filas[0];
            string rutaPrefab  = fila.prefab;
            string nombre      = fila.nombre_estacion;
            string descripcion = fila.descripcion ?? "";
            string ficha       = fila.ficha_informativa ?? "";
            string rutaAudio   = fila.recursos_multimedia ?? "";

            Debug.Log("[DIAG] ¡QR Válido! Mostrando ficha e iniciando preparación de modelo: " + rutaPrefab);

            // Priorizar siempre la carga del Prefab (.prefab) antes que un archivo crudo (.fbx)
            GameObject modeloCargado = null;
            if (!rutaPrefab.EndsWith("Prefab"))
            {
                modeloCargado = Resources.Load<GameObject>(rutaPrefab + "Prefab");
            }
            if (modeloCargado == null)
            {
                modeloCargado = Resources.Load<GameObject>(rutaPrefab);
            }
            if (modeloCargado == null && rutaPrefab.EndsWith("Prefab"))
            {
                modeloCargado = Resources.Load<GameObject>(rutaPrefab.Substring(0, rutaPrefab.Length - "Prefab".Length));
            }

            if (modeloCargado != null)
            {
                PrepararModeloParaPiso(modeloCargado);
            }
            else
            {
                Debug.LogWarning("[DIAG] No se encontró el prefab en Resources: " + rutaPrefab);
            }

            // Muestra la ficha de inmediato. El modelo 3D se vinculará tan pronto Vuforia detecte el piso
            if (detalle != null) detalle.Mostrar(nombre, descripcion, ficha, rutaAudio, null, textoBuscado);
            else Debug.LogWarning("Falta arrastrar el objeto Interfaz en el campo 'Detalle' de GestionSQLite.");
            return detalle != null;
        }
    }

    void PrepararModeloParaPiso(GameObject prefab)
    {
        try
        {
            if (!ConfigurarGroundPlane()) return;
        }
        catch (System.Exception e)
        {
            LogDiag("PrepararGroundPlane", e);
            return;
        }

        // Destruir cualquier ancla y modelo previo en el suelo
        LimpiarModeloColocado();

        prefabPendiente = prefab;
        esperandoPiso = true;
        Debug.Log("[DIAG] Modelo pendiente listo: " + prefab.name + ". Buscando superficie/piso para colocarlo.");

        // Intentar hit test interactivo inmediato en el centro de la pantalla
        if (detectorPiso != null)
        {
            Vector2 centro = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            detectorPiso.PerformHitTest(centro);
        }
    }

    bool ConfigurarGroundPlane()
    {
        if (detectorPiso == null)
            detectorPiso = FindAnyObjectByType<PlaneFinderBehaviour>();
        if (posicionContenido == null)
            posicionContenido = FindAnyObjectByType<ContentPositioningBehaviour>();

        if (posicionContenido == null || detectorPiso == null)
        {
            Debug.LogError("[DIAG] No se encontró Plane Finder con Content Positioning Behaviour en la escena.");
            return false;
        }

        if (posicionContenido.AnchorStage == null)
        {
            var stage = GameObject.Find("Ground Plane Stage");
            if (stage != null)
            {
                posicionContenido.AnchorStage = stage.GetComponent<AnchorBehaviour>();
            }
        }

        anclaTemplate = posicionContenido.AnchorStage;
        if (anclaTemplate == null)
        {
            Debug.LogError("[DIAG] Asigna Ground Plane Stage en el campo Anchor Stage del Plane Finder.");
            return false;
        }

        // Mantener DuplicateStage = true para que Vuforia preserve el Ground Plane Stage original
        // como plantilla inmutable y clone una nueva ancla en tiempo de ejecución. De esta forma,
        // cuando Vuforia se detenga o reinicie entre estaciones, nunca destruirá el AnchorStage de la escena.
        posicionContenido.DuplicateStage = true;
        detectorPiso.HitTestMode = HitTestMode.AUTOMATIC;

        if (!eventosRegistrados)
        {
            detectorPiso.OnAutomaticHitTest.AddListener(AlDetectarPiso);
            detectorPiso.OnInteractiveHitTest.AddListener(AlDetectarPiso);
            posicionContenido.OnContentPlaced.AddListener(AlColocarContenidoEnAncla);
            eventosRegistrados = true;
            Debug.Log("[DIAG] Listeners OnAutomaticHitTest, OnInteractiveHitTest y OnContentPlaced registrados correctamente.");
        }

        return true;
    }

    void AlDetectarPiso(HitTestResult resultado)
    {
        Debug.Log($"[DIAG] Evento HitTest recibido. esperandoPiso={esperandoPiso}, resultado={(resultado != null ? "VÁLIDO" : "NULO")}");

        if (!esperandoPiso || resultado == null || prefabPendiente == null) return;

        esperandoPiso = false;
        posicionContenido.PositionContentAtPlaneAnchor(resultado);
    }

    void AlColocarContenidoEnAncla(GameObject anclaInstanciada)
    {
        Debug.Log($"[DIAG] OnContentPlaced recibido. ancla={anclaInstanciada?.name}, prefabPendiente={prefabPendiente?.name}");

        if (prefabPendiente == null || anclaInstanciada == null) return;

        // Limpiar cualquier objeto hijo residual que pudiera tener el ancla clonada
        for (int i = anclaInstanciada.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(anclaInstanciada.transform.GetChild(i).gameObject);
        }

        GameObject modelo = Instantiate(prefabPendiente, anclaInstanciada.transform);
        modelo.name = prefabPendiente.name;
        modelo.transform.localPosition = Vector3.zero;

        // Orientar el modelo hacia la vista de la cámara para que siempre se vea de perfil con la cabeza mirando a la izquierda
        Camera cam = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        Vector3 adelanteCamara = cam != null
            ? Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized
            : Vector3.forward;

        if (adelanteCamara.sqrMagnitude < 0.001f) adelanteCamara = Vector3.forward;
        Quaternion rotacionVista = Quaternion.LookRotation(adelanteCamara, Vector3.up);
        if (prefabPendiente.name.Contains("OrigenDeLaVida"))
        {
            // Girar 180° para que el Panel Izquierdo (caracola) quede a la izquierda del usuario
            // y el Panel Derecho (peces y humano) quede a la derecha
            rotacionVista *= Quaternion.Euler(0f, 180f, 0f);
        }
        modelo.transform.rotation = rotacionVista * prefabPendiente.transform.localRotation;
        modelo.transform.localScale = prefabPendiente.transform.localScale;

        // Normalizar tamaño visual
        Renderer[] renderers = modelo.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds boundsTotal = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                boundsTotal.Encapsulate(renderers[i].bounds);
            }

            float mayorDimension = Mathf.Max(boundsTotal.size.x, boundsTotal.size.y, boundsTotal.size.z);
            float objetivoMetros = tamanoObjetivoMetros;
            if (prefabPendiente.name.Contains("OrigenDeLaVida"))
            {
                // El mural de la estación 2 tiene 14 especies: darle 2.4 metros de ancho total
                // para que cada animal tenga el porte real e imponente de las estaciones 1 y 3
                objetivoMetros = 2.40f;
            }
            else if (prefabPendiente.name.Contains("Biobio"))
            {
                // Cardumen de 9 peces del Biobío: darle 1.30 metros para apreciarlos en grupo
                objetivoMetros = 1.30f;
            }

            if (mayorDimension > 0.001f && objetivoMetros > 0.01f)
            {
                float factor = objetivoMetros / mayorDimension;
                modelo.transform.localScale *= factor;
            }

            // Recalcular bounds tras el escalado
            boundsTotal = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                boundsTotal.Encapsulate(renderers[i].bounds);
            }

            // Centrar horizontalmente el modelo (X y Z) respecto al ancla
            Vector3 centroEnAncla = anclaInstanciada.transform.InverseTransformPoint(boundsTotal.center);
            modelo.transform.localPosition -= new Vector3(centroEnAncla.x, 0f, centroEnAncla.z);

            // Alinear la base del modelo y levantarlo a la altura de flotación
            float alturaObjetivo = alturaFlotando;
            if (prefabPendiente.name.Contains("OrigenDeLaVida"))
            {
                alturaObjetivo = 1.15f; // Flotar a 1.15m (nivel del pecho/ojos) para apreciarlo de pie
            }
            else if (prefabPendiente.name.Contains("Biobio"))
            {
                alturaObjetivo = 0.90f; // Flotar a 0.90m (nivel de la cintura/pecho) como peces nadando en el agua
            }

            float minimoYLocal = float.PositiveInfinity;
            foreach (Renderer renderer in renderers)
            {
                Vector3 min = renderer.bounds.min;
                Vector3 max = renderer.bounds.max;
                Vector3[] esquinas = new Vector3[]
                {
                    new Vector3(min.x, min.y, min.z),
                    new Vector3(min.x, min.y, max.z),
                    new Vector3(min.x, max.y, min.z),
                    new Vector3(min.x, max.y, max.z),
                    new Vector3(max.x, min.y, min.z),
                    new Vector3(max.x, min.y, max.z),
                    new Vector3(max.x, max.y, min.z),
                    new Vector3(max.x, max.y, max.z),
                };

                foreach (var esquina in esquinas)
                {
                    Vector3 localEnAncla = anclaInstanciada.transform.InverseTransformPoint(esquina);
                    minimoYLocal = Mathf.Min(minimoYLocal, localEnAncla.y);
                }
            }

            if (!float.IsInfinity(minimoYLocal))
            {
                float desplazamientoY = -minimoYLocal + alturaObjetivo;
                modelo.transform.localPosition += Vector3.up * desplazamientoY;
            }
        }

        // Acercar el modelo horizontalmente hacia la cámara para no tener que dar pasos
        if (acercarHaciaCamara > 0.01f && cam != null)
        {
            Vector3 haciaCamara = Vector3.ProjectOnPlane(cam.transform.position - anclaInstanciada.transform.position, Vector3.up).normalized;
            Vector3 desplazamientoLocal = anclaInstanciada.transform.InverseTransformDirection(haciaCamara * acercarHaciaCamara);
            modelo.transform.localPosition += desplazamientoLocal;
        }

        // Asegurar gestor interactivo de los Peces del Biobío (Estación 4)
        if (modelo.name.Contains("Biobio") && modelo.GetComponent<GestorPuntosPecesBiobioAR>() == null)
        {
            modelo.AddComponent<GestorPuntosPecesBiobioAR>();
        }

        modelo.SetActive(true);
        anclaColocadaActual = anclaInstanciada;
        prefabPendiente = null;
        esperandoPiso = false;

        if (detalle != null)
        {
            detalle.AsignarModeloActual(anclaInstanciada);
        }

        Debug.Log($"[DIAG] ¡Modelo {modelo.name} colocado y activado exitosamente en el ancla del piso {anclaInstanciada.name}!");
    }

    public void LimpiarModeloColocado()
    {
        if (anclaColocadaActual != null)
        {
            Destroy(anclaColocadaActual);
            anclaColocadaActual = null;
        }
        prefabPendiente = null;
        esperandoPiso = false;
    }

    // ── Encuestas ──────────────────────────────────────────────────────
    public class FilaEncuesta
    {
        public int id_encuesta { get; set; }
        public string id_sesion { get; set; }
        public string origen { get; set; }
        public string rango_edad { get; set; }
        public int calificacion { get; set; }
        public string comentarios { get; set; }
        public string fecha_respuesta { get; set; }
        public int sincronizacion_encuesta { get; set; }
    }

    public bool GuardarEncuesta(string idSesion, string origen, string rangoEdad, int calificacion, string comentarios)
    {
        try
        {
            using (var db = new SQLiteConnection(rutaDB))
            {
                // Asegurar integridad referencial con SESION_ANONIMA
                db.Execute(@"INSERT OR IGNORE INTO SESION_ANONIMA 
                    (id_sesion, fecha_inicio, sincronizacion) 
                    VALUES (?, datetime('now'), 0)", 
                    idSesion);

                db.Execute(@"INSERT INTO ENCUESTA_VISITANTE 
                    (id_sesion, origen, rango_edad, calificacion, comentarios, fecha_respuesta, sincronizacion_encuesta)
                    VALUES (?, ?, ?, ?, ?, date('now'), 0)",
                    idSesion, origen, rangoEdad, calificacion, comentarios ?? "");
            }
            Debug.Log("[DIAG] Encuesta guardada en SQLite exitosamente.");
            return true;
        }
        catch (System.Exception e)
        {
            LogDiag("GuardarEncuesta", e);
            return false;
        }
    }

    public List<FilaEncuesta> ObtenerEncuestasGuardadas()
    {
        try
        {
            if (string.IsNullOrEmpty(rutaDB)) rutaDB = Path.Combine(Application.persistentDataPath, "registro_interacciones.db");
            using (var db = new SQLiteConnection(rutaDB))
            {
                return db.Query<FilaEncuesta>("SELECT * FROM ENCUESTA_VISITANTE");
            }
        }
        catch (System.Exception e)
        {
            LogDiag("ObtenerEncuestasGuardadas", e);
            return new List<FilaEncuesta>();
        }
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Colbún/Ver Encuestas Guardadas en SQLite")]
    public static void MenuVerEncuestas()
    {
        var gestor = FindAnyObjectByType<GestionSQLite>();
        if (gestor == null)
        {
            Debug.LogWarning(">> [Colbún] No se encontró GestionSQLite en la escena.");
            return;
        }
        var lista = gestor.ObtenerEncuestasGuardadas();
        Debug.Log($">> [Colbún] Total de encuestas guardadas en SQLite: {lista.Count}");
        foreach (var enc in lista)
        {
            Debug.Log($"   [ID {enc.id_encuesta}] Sesión: {enc.id_sesion} | Origen: {enc.origen} | Edad: {enc.rango_edad} | Calificación: {enc.calificacion}/5 | Comentario: '{enc.comentarios}' | Fecha: {enc.fecha_respuesta}");
        }
    }
#endif
}