using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class EncuestaController : MonoBehaviour
{
    [Serializable]
    class Respuesta
    {
        public string id_sesion_anonima;
        public string fecha;
        public string origen;
        public string edad;
        public int nota;
        public string comentario;
    }

    [Serializable]
    class PayloadServidor
    {
        public string id_sesion;
        public string origen;
        public string edad;
        public int nota;
        public string comentario;
        public string fecha;
    }

    readonly string[] origenes = { "Santa Bárbara", "Quilaco", "Los Ángeles", "Otra comuna del Biobío", "Otra región de Chile", "Otro país" };
    readonly string[] edades   = { "Menor de 18", "18 a 29", "30 a 59", "60 o más" };
    readonly string[] notas    = { "1", "2", "3", "4", "5" };

    readonly List<VisualElement> contenedores = new List<VisualElement>();
    string origen, edad;
    int nota;
    TextField comentario;
    GestionSQLite gestorBD;
    Label mensaje;
    Button btnEnviar;

    void OnEnable()
    {
        var raiz = GetComponent<UIDocument>().rootVisualElement;

        CrearChips(raiz.Q<VisualElement>("ch-origen"), origenes, v => origen = v);
        CrearChips(raiz.Q<VisualElement>("ch-edad"), edades, v => edad = v);
        CrearChips(raiz.Q<VisualElement>("ch-nota"), notas, v => nota = int.Parse(v));

        comentario = raiz.Q<TextField>("tf-comentario");
        var input = comentario.Q(className: "unity-base-text-field__input");
        if (input != null) { input.style.fontSize = 34; input.style.minHeight = 180; }

        mensaje = raiz.Q<Label>("lbl-encuesta-mensaje");
        btnEnviar = raiz.Q<Button>("btn-enviar-encuesta");
        btnEnviar.clicked += Enviar;

        gestorBD = FindAnyObjectByType<GestionSQLite>();

        // Si ya respondió la encuesta en esta visita, deshabilitar botón
        if (PlayerPrefs.GetInt("encuesta_enviada", 0) == 1)
        {
            mensaje.text = "¡Ya has respondido esta encuesta! Muchas gracias por tu opinión.";
            btnEnviar.text = "Encuesta enviada";
            btnEnviar.SetEnabled(false);
        }
    }

    void CrearChips(VisualElement contenedor, string[] opciones, Action<string> alElegir)
    {
        if (contenedor == null) return;
        contenedor.Clear();
        foreach (var texto in opciones)
        {
            var chip = new Button { text = texto };
            chip.AddToClassList("chip");
            chip.clicked += () =>
            {
                foreach (var otro in contenedor.Children()) otro.RemoveFromClassList("chip--sel");
                chip.AddToClassList("chip--sel");
                alElegir(texto);
            };
            contenedor.Add(chip);
        }
        contenedores.Add(contenedor);
    }

    void Enviar()
    {
        if (PlayerPrefs.GetInt("encuesta_enviada", 0) == 1)
        {
            mensaje.text = "Ya has enviado tu respuesta anteriormente.";
            return;
        }

        if (origen == null || edad == null || nota == 0)
        {
            mensaje.text = "Por favor responde las 3 primeras preguntas.";
            return;
        }

        string idSesion = ObtenerIdAnonimo();
        string comentariosTexto = comentario != null ? (comentario.value ?? "") : "";

        // 1. Guardar en la base de datos local SQLite (Tabla ENCUESTA_VISITANTE)
        if (gestorBD == null) gestorBD = FindAnyObjectByType<GestionSQLite>();
        if (gestorBD != null)
        {
            gestorBD.GuardarEncuesta(idSesion, origen, edad, nota, comentariosTexto);
        }
        else
        {
            Debug.LogWarning("[ENCUESTA] No se encontró GestionSQLite para registrar en base de datos.");
        }

        // 2. Guardar también respaldo en archivo JSON local
        try
        {
            var r = new Respuesta
            {
                id_sesion_anonima = idSesion,
                fecha = DateTime.Now.ToString("s"),
                origen = origen,
                edad = edad,
                nota = nota,
                comentario = comentariosTexto
            };

            string ruta = Path.Combine(Application.persistentDataPath, "encuestas.jsonl");
            File.AppendAllText(ruta, JsonUtility.ToJson(r) + "\n");
            Debug.Log("[ENCUESTA] Guardada también en archivo JSON: " + ruta);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ENCUESTA] Error en respaldo JSON: " + ex.Message);
        }

        // 3. Marcar como completada
        PlayerPrefs.SetInt("encuesta_enviada", 1);
        PlayerPrefs.Save();

        // 4. Intentar enviar en segundo plano a la laptop si está conectado al Hotspot (Offline LAN)
        StartCoroutine(EnviarAlServidorDashboard(idSesion, origen, edad, nota, comentariosTexto));

        mensaje.text = "¡Gracias por responder! Tu opinión ha sido guardada en la base de datos.";
        if (btnEnviar != null)
        {
            btnEnviar.text = "Encuesta enviada";
            btnEnviar.SetEnabled(false);
        }
        Limpiar();
    }

    IEnumerator EnviarAlServidorDashboard(string idSesion, string origen, string edad, int nota, string comentarios)
    {
#if UNITY_EDITOR
        string ipServidor = "127.0.0.1";
#else
        string ipServidor = PlayerPrefs.GetString("ip_servidor_dashboard", "192.168.137.1");
#endif
        string url = $"http://{ipServidor}:8000/api/encuesta";

        var payload = new PayloadServidor
        {
            id_sesion = idSesion ?? "sesion_android",
            origen = string.IsNullOrEmpty(origen) ? "Santa Bárbara" : origen,
            edad = string.IsNullOrEmpty(edad) ? "18 a 29" : edad,
            nota = nota,
            comentario = comentarios ?? "",
            fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string json = JsonUtility.ToJson(payload);
        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = 3; // Timeout corto (3 seg) para no gastar batería si no hay señal
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[ENCUESTA] ¡Enviada exitosamente al Dashboard de la laptop!");
            }
            else
            {
                Debug.Log("[ENCUESTA] Servidor de laptop no alcanzable (" + req.error + "). Queda seguro en SQLite local.");
            }
        }
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Colbún/Habilitar Conexiones Locales HTTP")]
    public static void HabilitarHTTP()
    {
        UnityEditor.PlayerSettings.insecureHttpOption = UnityEditor.InsecureHttpOption.AlwaysAllowed;
        Debug.Log(">> [Colbún] Conexiones locales HTTP habilitadas (AlwaysAllowed).");
    }

    [UnityEditor.MenuItem("Colbún/Probar Envío de Encuesta a Streamlit")]
    public static void ProbarEnvioEditor()
    {
        UnityEditor.PlayerSettings.insecureHttpOption = UnityEditor.InsecureHttpOption.AlwaysAllowed;
        Debug.Log(">> [Colbún] Enviando encuesta de prueba a Streamlit (127.0.0.1:8000)...");

        var payload = new PayloadServidor
        {
            id_sesion = "test_unity_" + System.DateTime.Now.Ticks,
            origen = "Santa Bárbara",
            edad = "18 a 29",
            nota = 5,
            comentario = "¡Prueba de conexión exitosa desde Unity a Streamlit!",
            fecha = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        string json = JsonUtility.ToJson(payload);
        var req = new UnityWebRequest("http://127.0.0.1:8000/api/encuesta", "POST");
        req.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        var op = req.SendWebRequest();
        op.completed += _ =>
        {
            if (req.result == UnityWebRequest.Result.Success)
            {
                Debug.Log(">> [Colbún] ¡CONEXIÓN EXITOSA! Streamlit respondió: " + req.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning(">> [Colbún] No se pudo conectar con Streamlit: " + req.error);
            }
            req.Dispose();
        };
    }
#endif

    string ObtenerIdAnonimo()
    {
        string id = PlayerPrefs.GetString("id_sesion_anonima", "");
        if (id == "")
        {
            id = Guid.NewGuid().ToString();
            PlayerPrefs.SetString("id_sesion_anonima", id);
        }
        return id;
    }

    void Limpiar()
    {
        origen = null; edad = null; nota = 0;
        comentario.value = "";
        foreach (var c in contenedores)
            foreach (var chip in c.Children()) chip.RemoveFromClassList("chip--sel");
    }
}