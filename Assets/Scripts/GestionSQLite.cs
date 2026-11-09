using System.IO;
using UnityEngine;
using SQLite;   // viene del paquete SQLite-net for Unity (gilzoide)
using Vuforia;

public class GestionSQLite : MonoBehaviour
{
    private string rutaDB;

    [Header("Ficha de la estación (arrastra aquí el objeto Interfaz)")]
    public DetalleEstacionController detalle;

    [Header("Escala física del modelo (1 unidad Unity = 1 metro)")]
    [Min(0.01f)] public float escalaModelo = 1f;

    AnchorBehaviour anclaPiso;
    PlaneFinderBehaviour detectorPiso;
    ContentPositioningBehaviour posicionContenido;
    GameObject modeloPendiente;
    bool esperandoPiso;
    bool eventoPisoRegistrado;

    // Recibe el resultado de la consulta. Los nombres coinciden con las columnas de la tabla.
    // [Preserve] evita que el build de Android borre estas propiedades (SQLite-net las usa por reflexión).
    [UnityEngine.Scripting.Preserve]
    public class FilaEstacion
    {
        public string prefab { get; set; }
        public string nombre_estacion { get; set; }
        public string descripcion { get; set; }
        public string ficha_informativa { get; set; }
        public string recursos_multimedia { get; set; }
    }

    void Start()
    {
        // Usa los objetos Ground Plane creados en la escena desde el menú de Vuforia.
        // No se deben crear behaviours nuevos al escanear: Vuforia ya está iniciado.
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

    // Imprime el tipo, el mensaje y los errores internos SIN pedir la traza completa
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
        }
    }

    private void InsertarEstacionesDePrueba()
    {
        using (var db = new SQLiteConnection(rutaDB))
        {
            // INSERT OR REPLACE: cada vez que se abre la app se actualiza la estación 1,
            // así que cambiar un texto, el audio o el modelo aquí se aplica sin borrar la base de datos.
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
        }
    }

    // Este es el método que llama LectorQR. Solo protege con try/catch;
    // la lógica real vive en ProcesarEscaneoQRInterno.
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

    private bool ProcesarEscaneoQRInterno(string textoEscaneadoPorVuforia)
    {
        // Validación: se descartan textos vacíos o demasiado largos
        if (string.IsNullOrWhiteSpace(textoEscaneadoPorVuforia) || textoEscaneadoPorVuforia.Length > 100)
        {
            Debug.LogWarning("QR inválido.");
            return false;
        }

        using (var db = new SQLiteConnection(rutaDB))
        {
            // Consulta con parámetro (?): el texto del QR nunca se pega dentro del SQL
            var filas = db.Query<FilaEstacion>(
                "SELECT prefab, nombre_estacion, descripcion, ficha_informativa, recursos_multimedia " +
                "FROM ESTACION WHERE codigo_qr = ?",
                textoEscaneadoPorVuforia);

            if (filas.Count == 0)
            {
                Debug.LogWarning("[DIAG] QR no encontrado en la BD: " + textoEscaneadoPorVuforia);
                return false;
            }

            FilaEstacion fila = filas[0];
            string rutaPrefab  = fila.prefab;
            string nombre      = fila.nombre_estacion;
            string descripcion = fila.descripcion ?? "";
            string ficha       = fila.ficha_informativa ?? "";
            string rutaAudio   = fila.recursos_multimedia ?? "";

            Debug.Log("¡QR Válido! Mostrando ficha e instanciando modelo.");

            // El QR solo identifica la estación. Ground Plane fija el modelo al piso.
            GameObject modelo = null;
            GameObject modeloCargado = Resources.Load<GameObject>(rutaPrefab);
            if (modeloCargado != null)
            {
                modelo = PrepararModeloParaPiso(modeloCargado);
                if (modelo == null)
                    Debug.LogWarning("[DIAG] No se pudo preparar el modelo para Ground Plane.");
            }
            else
            {
                Debug.LogWarning("No se encontró el prefab en Resources: " + rutaPrefab);
            }

            // Ficha: textos, audio y cambio de pantalla
            if (detalle != null) detalle.Mostrar(nombre, descripcion, ficha, rutaAudio, modelo, textoEscaneadoPorVuforia);
            else Debug.LogWarning("Falta arrastrar el objeto Interfaz en el campo 'Detalle' de GestionSQLite.");
            return detalle != null;
        }
    }

    GameObject PrepararModeloParaPiso(GameObject prefab)
    {
        try
        {
            if (!ConfigurarGroundPlane()) return null;
        }
        catch (System.Exception e)
        {
            // No bloquea la ficha de la estación si Ground Plane falla en el equipo.
            LogDiag("PrepararGroundPlane", e);
            return null;
        }

        if (modeloPendiente != null) Destroy(modeloPendiente);

        modeloPendiente = Instantiate(prefab, anclaPiso.transform);
        modeloPendiente.name = prefab.name;
        modeloPendiente.transform.localPosition = Vector3.zero;
        modeloPendiente.transform.localRotation = Quaternion.Euler(0f, prefab.transform.eulerAngles.y, 0f);
        modeloPendiente.transform.localScale = Vector3.one * escalaModelo;

        // Alinea el punto más bajo de las mallas con la superficie detectada.
        Renderer[] renderers = modeloPendiente.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            float minimoY = float.PositiveInfinity;
            foreach (Renderer renderer in renderers)
                minimoY = Mathf.Min(minimoY, renderer.bounds.min.y);
            modeloPendiente.transform.localPosition += Vector3.up * -minimoY;
        }

        modeloPendiente.SetActive(false);
        esperandoPiso = true;
        Debug.Log("QR válido. Buscando automáticamente el piso para colocar el modelo.");
        return modeloPendiente;
    }

    bool ConfigurarGroundPlane()
    {
        if (detectorPiso == null)
            detectorPiso = FindAnyObjectByType<PlaneFinderBehaviour>();
        if (posicionContenido == null)
            posicionContenido = FindAnyObjectByType<ContentPositioningBehaviour>();

        if (posicionContenido == null || detectorPiso == null)
        {
            Debug.LogError("No se encontró Plane Finder con Content Positioning Behaviour en la escena.");
            return false;
        }

        anclaPiso = posicionContenido.AnchorStage;
        if (anclaPiso == null)
        {
            Debug.LogError("Asigna Ground Plane Stage en el campo Anchor Stage del Plane Finder.");
            return false;
        }

        posicionContenido.AnchorStage = anclaPiso;
        posicionContenido.DuplicateStage = false;
        detectorPiso.HitTestMode = HitTestMode.AUTOMATIC;

        if (!eventoPisoRegistrado)
        {
            detectorPiso.OnAutomaticHitTest.AddListener(AlDetectarPiso);
            eventoPisoRegistrado = true;
        }

        return true;
    }

    void AlDetectarPiso(HitTestResult resultado)
    {
        if (!esperandoPiso || resultado == null) return;

        esperandoPiso = false;
        posicionContenido.PositionContentAtPlaneAnchor(resultado);
        ActivarModeloColocado();
    }

    void ActivarModeloColocado()
    {
        if (modeloPendiente != null) modeloPendiente.SetActive(true);
    }
}
