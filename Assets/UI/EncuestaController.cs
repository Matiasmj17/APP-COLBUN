using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
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

        mensaje.text = "¡Gracias por responder! Tu opinión ha sido guardada en la base de datos.";
        if (btnEnviar != null)
        {
            btnEnviar.text = "Encuesta enviada";
            btnEnviar.SetEnabled(false);
        }
        Limpiar();
    }

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