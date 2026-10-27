using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;

public class GestionSQLite : MonoBehaviour
{
    private string rutaDB;

    [Header("Ficha de la estación (arrastra aquí el objeto Interfaz)")]
    public DetalleEstacionController detalle;

    [Header("Cómo aparece el modelo 3D frente a la cámara")]
    public float distanciaModelo = 1.5f;   // metros hacia adelante
    public float alturaModelo    = 0.5f;   // metros hacia arriba (para que quede sobre la ficha)
    public float escalaModelo    = 0.2f;

    void Start()
    {
        rutaDB = "URI=file:" + Application.persistentDataPath + "/registro_interacciones.db";
        CrearTablasIniciales();
        InsertarEstacionesDePrueba();
    }

    private void CrearTablasIniciales()
    {
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
        using (IDbConnection dbConnection = new SqliteConnection(rutaDB))
        {
            dbConnection.Open();
            using (IDbCommand dbCmd = dbConnection.CreateCommand())
            {
                // INSERT OR REPLACE: cada vez que se abre la app se actualiza la estación 1,
                // así que cambiar un texto, el audio o el modelo aquí se aplica sin borrar la base de datos.
                dbCmd.CommandText = @"INSERT OR REPLACE INTO ESTACION 
                                      (id_estacion, nombre_estacion, codigo_qr, descripcion, ficha_informativa, recursos_multimedia, nombre_modelo, prefab, formato) 
                                      VALUES (1, 
                                      'Estación 1: El Ciclo del Agua', 
                                      'QR_AGUA_01', 
                                      'El origen de la vida (H2O). Molécula formada por dos átomos de hidrógeno y uno de oxígeno.', 
                                      'El agua modela la Tierra y provee energía. En su ciclo de permanente transformación, posee tres estados: sólido, líquido y gaseoso. Los ríos fluyen continuamente desde la alta montaña hasta desembocar en el mar, lagos u otros ríos.', 
                                      'Modelos3D/Audios/Estacion1_Agua', 
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
        // Validación: se descartan textos vacíos o demasiado largos
        if (string.IsNullOrWhiteSpace(textoEscaneadoPorVuforia) || textoEscaneadoPorVuforia.Length > 100)
        {
            Debug.LogWarning("QR inválido.");
            return;
        }

        using (IDbConnection dbConnection = new SqliteConnection(rutaDB))
        {
            dbConnection.Open();
            using (IDbCommand dbCmd = dbConnection.CreateCommand())
            {
                // Consulta con parámetro (@codigo): el texto del QR nunca se pega dentro del SQL
                dbCmd.CommandText = "SELECT prefab, nombre_estacion, descripcion, ficha_informativa, recursos_multimedia " +
                                    "FROM ESTACION WHERE codigo_qr = @codigo";
                var parametro = dbCmd.CreateParameter();
                parametro.ParameterName = "@codigo";
                parametro.Value = textoEscaneadoPorVuforia;
                dbCmd.Parameters.Add(parametro);

                using (IDataReader reader = dbCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string rutaPrefab  = reader.GetString(0);
                        string nombre      = reader.GetString(1);
                        string descripcion = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        string ficha       = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        string rutaAudio   = reader.IsDBNull(4) ? "" : reader.GetString(4);

                        Debug.Log("¡QR Válido! Mostrando ficha e instanciando modelo.");

                        // Modelo 3D frente a la cámara
                        GameObject modelo = null;
                        GameObject modeloCargado = Resources.Load<GameObject>(rutaPrefab);
                        if (modeloCargado != null)
                        {
                            Transform cam = Camera.main.transform;
                            modelo = Instantiate(modeloCargado);
                            modelo.transform.localScale = Vector3.one * escalaModelo;
                            modelo.transform.position = cam.position + cam.forward * distanciaModelo + Vector3.up * alturaModelo;
                            modelo.transform.LookAt(cam);
                        }
                        else
                        {
                            Debug.LogWarning("No se encontró el prefab en Resources: " + rutaPrefab);
                        }

                        // Ficha: textos, audio y cambio de pantalla
                        if (detalle != null) detalle.Mostrar(nombre, descripcion, ficha, rutaAudio, modelo);
                        else Debug.LogWarning("Falta arrastrar el objeto Interfaz en el campo 'Detalle' de GestionSQLite.");
                    }
                }
            }
            dbConnection.Close();
        }
    }
}