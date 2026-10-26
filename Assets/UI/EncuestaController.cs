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

    readonly string[] origenes = { "Colbún", "Otra comuna del Maule", "Otra región de Chile", "Otro país" };
    readonly string[] edades   = { "Menor de 18", "18 a 29", "30 a 59", "60 o más" };
    readonly string[] notas    = { "1", "2", "3", "4", "5" };

    readonly List<VisualElement> contenedores = new List<VisualElement>();
    string origen, edad;
    int nota;
    TextField comentario;
    Label mensaje;

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
        raiz.Q<Button>("btn-enviar-encuesta").clicked += Enviar;
    }

    void CrearChips(VisualElement contenedor, string[] opciones, Action<string> alElegir)
    {
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
        if (origen == null || edad == null || nota == 0)
        {
            mensaje.text = "Por favor responde las 3 primeras preguntas.";
            return;
        }

        var r = new Respuesta
        {
            id_sesion_anonima = ObtenerIdAnonimo(),
            fecha = DateTime.Now.ToString("s"),
            origen = origen,
            edad = edad,
            nota = nota,
            comentario = comentario.value
        };

        string ruta = Path.Combine(Application.persistentDataPath, "encuestas.jsonl");
        File.AppendAllText(ruta, JsonUtility.ToJson(r) + "\n");
        Debug.Log("Encuesta guardada en: " + ruta);

        mensaje.text = "¡Gracias por responder!";
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