using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;
using UnityEngine.UIElements;

public class GestionSQLite : MonoBehaviour
{
    private string rutaDB;

    [Header("Conexión UI Toolkit")]
    public UIDocument uiDocument;
    private Label txtTitulo;
    private Label txtDescripcion;
    
    // Contenedores de las pantallas
    private VisualElement pantallaEscaner;
    private VisualElement pantallaDetalle;

    void Start()
    {
        rutaDB = "URI=file:" + Application.persistentDataPath + "/registro_interacciones.db";
        
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;
            txtTitulo = root.Q<Label>("TXT_Titulo"); 
            txtDescripcion = root.Q<Label>("TXT_Descripcion");

            // Buscamos las pantallas por su nombre (Asegúrate de que se llamen así en UI Builder)
            pantallaEscaner = root.Q<VisualElement>("pantalla-escaner"); 
            pantallaDetalle = root.Q<VisualElement>("pantalla-detalle"); 

            // Aseguramos que al inicio la pantalla de detalle esté oculta
            if (pantallaDetalle != null) pantallaDetalle.style.display = DisplayStyle.None;
        }

        CrearTablasIniciales();
        InsertarEstacionesDePrueba();
    }

    private void CrearTablasIniciales()
    {
        // (Mantén tu código actual de CrearTablasIniciales aquí, no cambia)
        using (IDbConnection dbConnection = new SqliteConnection(rutaDB))
        {
            dbConnection.Open();
            using (IDbCommand dbCmd = dbConnection.CreateCommand())
            {
                dbCmd.CommandText = @"CREATE TABLE IF NOT EXISTS SESION_ANONIMA (
                    id_sesion VARCHAR(36) PRIMARY KEY,
                    fecha_inicio DATETIME,
                    sincronizacion BOOLEAN,
                    fecha_sincronizada DATE
                )";
                dbCmd.ExecuteNonQuery();

                dbCmd.CommandText = @"CREATE TABLE IF NOT EXISTS ESTACION (
                    id_estacion INTEGER PRIMARY KEY,
                    nombre_estacion VARCHAR(50),
                    codigo_qr VARCHAR(255),
                    descripcion VARCHAR(150),
                    ficha_informativa VARCHAR(500),
                    recursos_multimedia VARCHAR(255),
                    nombre_modelo VARCHAR(20),
                    prefab VARCHAR(255),
                    formato VARCHAR(10)
                )";
                dbCmd.ExecuteNonQuery();

                dbCmd.CommandText = @"CREATE TABLE IF NOT EXISTS REGISTRO_INTERACCION (
                    id_interaccion INTEGER PRIMARY KEY AUTOINCREMENT,
                    id_sesion VARCHAR(36),
                    id_estacion INTEGER,
                    tiempo_inicio DATETIME,
                    tiempo_retencion INTEGER,
                    FOREIGN KEY(id_sesion) REFERENCES SESION_ANONIMA(id_sesion),
                    FOREIGN KEY(id_estacion) REFERENCES ESTACION(id_estacion)
                )";
                dbCmd.ExecuteNonQuery();
            }
            dbConnection.Close();
        }
    }

    private void InsertarEstacionesDePrueba()
    {
        // (Mantén tu código actual de InsertarEstacionesDePrueba aquí, no cambia)
        using (IDbConnection dbConnection = new SqliteConnection(rutaDB))
        {
            dbConnection.Open();
            using (IDbCommand dbCmd = dbConnection.CreateCommand())
            {
                dbCmd.CommandText = @"INSERT OR REPLACE INTO ESTACION 
                                      (id_estacion, nombre_estacion, codigo_qr, descripcion, ficha_informativa, recursos_multimedia, nombre_modelo, prefab, formato) 
                                      VALUES (1, 
                                      'Estación 1: El Ciclo del Agua', 
                                      'QR_AGUA_01', 
                                      'El origen de la vida (H2O). Molécula formada por dos átomos de hidrógeno y uno de oxígeno.', 
                                      'El agua modela la Tierra y provee energía. En su ciclo de permanente transformación, posee tres estados: sólido, líquido y gaseoso. Los ríos fluyen continuamente desde la alta montaña hasta desembocar en el mar, lagos u otros ríos.', 
                                      'Audios/Estacion1_Agua', 
                                      'CuboPrueba1', 
                                      'Modelos3D/Estacion1/CuboPrueba1', 
                                      '.prefab')";
                dbCmd.ExecuteNonQuery();
            }
            dbConnection.Close();
        }
    }

    public void ProcesarEscaneoQR(string textoEscaneadoPorVuforia)
    {
        using (IDbConnection dbConnection = new SqliteConnection(rutaDB))
        {
            dbConnection.Open();
            using (IDbCommand dbCmd = dbConnection.CreateCommand())
            {
                dbCmd.CommandText = $"SELECT prefab, nombre_estacion, ficha_informativa FROM ESTACION WHERE codigo_qr = '{textoEscaneadoPorVuforia}'";
                
                using (IDataReader reader = dbCmd.ExecuteReader())
                {
                    if (reader.Read()) 
                    {
                        string rutaPrefab = reader.GetString(0);
                        string nombreEstacion = reader.GetString(1);
                        string fichaInformativa = reader.GetString(2);

                        // Inyectamos textos
                        if (txtTitulo != null) txtTitulo.text = nombreEstacion;
                        if (txtDescripcion != null) txtDescripcion.text = fichaInformativa;
                        
                        // --- LÓGICA DE TRANSICIÓN DE PANTALLA ---
                        if (pantallaEscaner != null) pantallaEscaner.style.display = DisplayStyle.None; // Oculta el UI del escáner
                        if (pantallaDetalle != null) pantallaDetalle.style.display = DisplayStyle.Flex; // Muestra la tarjeta del hito

                        Debug.Log("¡QR Válido! Pantalla cambiada e instanciando modelo.");
                        
                        GameObject modeloCargado = Resources.Load<GameObject>(rutaPrefab);
                        if (modeloCargado != null)
                        {
                            GameObject modelo = Instantiate(modeloCargado);
                            modelo.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                            modelo.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Vector3.up * 0.2f;
                            modelo.transform.LookAt(Camera.main.transform);
                        }
                    }
                }
            }
            dbConnection.Close();
        }
    }
}