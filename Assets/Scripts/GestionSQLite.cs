using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.IO;

public class GestionSQLite : MonoBehaviour
{
    private string rutaDB;

    void Start()
    {
        rutaDB = "URI=file:" + Application.persistentDataPath + "/registro_interacciones.db";
        CrearTablasIniciales();
        InsertarEstacionesDePrueba(); // Llenamos el catálogo estático
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

    // 1. Llenamos la tabla ESTACION con datos para que haya algo que buscar
    private void InsertarEstacionesDePrueba()
    {
        using (IDbConnection dbConnection = new SqliteConnection(rutaDB))
        {
            dbConnection.Open();
            using (IDbCommand dbCmd = dbConnection.CreateCommand())
            {
                dbCmd.CommandText = "SELECT COUNT(*) FROM ESTACION";
                int count = System.Convert.ToInt32(dbCmd.ExecuteScalar());

                if (count == 0)
                {
                    // Inserción para la Estación 1: Agua y Energía (Ciclo del agua)
                    dbCmd.CommandText = @"INSERT INTO ESTACION (id_estacion, nombre_estacion, codigo_qr, prefab) 
                                          VALUES (1, 'Agua y Energía', 'QR_AGUA_01', 'Modelos3D/Estacion1/CuboPrueba1')";
                    dbCmd.ExecuteNonQuery();
                    Debug.Log("Estación 1 de prueba insertada con éxito.");
                }
            }
            dbConnection.Close();
        }
    }

    // 2. ESTA ES LA FUNCIÓN QUE LLAMARÁS CUANDO VUFORIA DETECTE EL QR
    public void ProcesarEscaneoQR(string textoEscaneadoPorVuforia)
    {
        using (IDbConnection dbConnection = new SqliteConnection(rutaDB))
        {
            dbConnection.Open();
            using (IDbCommand dbCmd = dbConnection.CreateCommand())
            {
                // Buscamos el prefab asociado a ese código QR exacto
                dbCmd.CommandText = $"SELECT prefab FROM ESTACION WHERE codigo_qr = '{textoEscaneadoPorVuforia}'";
                
                using (IDataReader reader = dbCmd.ExecuteReader())
                {
                    if (reader.Read()) // Si encontró una coincidencia en la base de datos
                    {
                        string rutaPrefab = reader.GetString(0);
                        Debug.Log("¡QR Válido! Cargando modelo desde: " + rutaPrefab);
                        
                        // Unity busca el archivo en la carpeta especial 'Resources'
                        GameObject modeloCargado = Resources.Load<GameObject>(rutaPrefab);
                        
                        if (modeloCargado != null)
                        {
                            // 1. Instanciamos el modelo y lo guardamos en una variable
                            GameObject modelo = Instantiate(modeloCargado);

                            // 2. Reducimos su tamaño al 20%
                            modelo.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

                            // 3. Lo posicionamos 1.5 metros justo frente a la cámara, y 50 cm más abajo
                            modelo.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 1.5f + Vector3.down * 0.5f;

                            // 4. Hacemos que el modelo rote para mirar directamente hacia la cámara
                            modelo.transform.LookAt(Camera.main.transform);
                        }
                        else
                        {
                            Debug.LogError("No se encontró el archivo 3D en la carpeta Resources.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("QR Inválido o dañado. Mostrar alerta al usuario.");
                    }
                }
            }
            dbConnection.Close();
        }
    }
}