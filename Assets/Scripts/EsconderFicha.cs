using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Agrega una flecha (como la ^ de la bandeja de Windows) pegada al borde superior de la ficha.
/// Al tocarla, la ficha baja y se esconde dejando solo la flecha; al tocarla otra vez, vuelve a subir.
/// Va en el mismo objeto que el UIDocument (Interfaz).
/// </summary>
public class FichaDesplegable : MonoBehaviour
{
    [SerializeField] private UIDocument documento;
    [Tooltip("Nombre (name) del elemento de la tarjeta blanca en el UXML")]
    [SerializeField] private string nombreFicha = "ficha";
    [SerializeField] private float duracionSegundos = 0.3f;

    private VisualElement ficha;
    private Button boton;
    private VisualElement chevron;
    private bool oculta = false;

    void OnEnable()
    {
        if (documento == null) documento = GetComponent<UIDocument>();
        var raiz = documento.rootVisualElement;

        ficha = raiz.Q<VisualElement>(nombreFicha);
        if (ficha == null)
        {
            Debug.LogWarning("[FICHA] No se encontró un elemento llamado '" + nombreFicha + "'. Estos son los nombres que existen:");
            raiz.Query<VisualElement>().ForEach(e =>
            {
                if (!string.IsNullOrEmpty(e.name)) Debug.Log("[FICHA] elemento: " + e.name);
            });
            return;
        }

        // Animación suave al mover la ficha
        var duracion = new List<TimeValue> { new TimeValue(duracionSegundos, TimeUnit.Second) };
        ficha.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("translate") };
        ficha.style.transitionDuration = duracion;

        ConstruirFlecha(duracion);
    }

    void OnDisable()
    {
        if (boton != null) boton.RemoveFromHierarchy();
    }

    private void ConstruirFlecha(List<TimeValue> duracion)
    {
        // Botón pegado arriba de la ficha, centrado
        boton = new Button(Alternar);
        boton.style.position = Position.Absolute;
        boton.style.top = -48;
        boton.style.left = Length.Percent(50);
        boton.style.width = 120;
        boton.style.height = 48;
        boton.style.marginLeft = -60;
        boton.style.marginTop = 0;
        boton.style.marginBottom = 0;
        boton.style.marginRight = 0;
        boton.style.paddingTop = 0;
        boton.style.paddingBottom = 0;
        boton.style.backgroundColor = Color.white;
        boton.style.borderTopLeftRadius = 16;
        boton.style.borderTopRightRadius = 16;
        boton.style.borderBottomLeftRadius = 0;
        boton.style.borderBottomRightRadius = 0;
        boton.style.borderTopWidth = 0;
        boton.style.borderBottomWidth = 0;
        boton.style.borderLeftWidth = 0;
        boton.style.borderRightWidth = 0;
        boton.style.alignItems = Align.Center;
        boton.style.justifyContent = Justify.Center;

        // Flecha dibujada con dos bordes de un cuadrado girado (no depende de la fuente)
        chevron = new VisualElement();
        chevron.pickingMode = PickingMode.Ignore;
        chevron.style.width = 18;
        chevron.style.height = 18;
        chevron.style.marginBottom = 4;
        chevron.style.borderRightWidth = 4;
        chevron.style.borderBottomWidth = 4;
        var azul = new Color(0.05f, 0.35f, 0.7f);
        chevron.style.borderRightColor = azul;
        chevron.style.borderBottomColor = azul;
        chevron.style.rotate = new Rotate(45f);   // 45° = apunta hacia abajo (ficha visible)
        chevron.style.transitionProperty = new List<StylePropertyName> { new StylePropertyName("rotate") };
        chevron.style.transitionDuration = duracion;

        boton.Add(chevron);
        ficha.Add(boton);
    }

    private void Alternar()
    {
        oculta = !oculta;

        // La ficha baja su propio alto: queda fuera de pantalla y solo asoma la flecha
        float altura = ficha.resolvedStyle.height;
        ficha.style.translate = new Translate(0, oculta ? altura : 0);

        // 225° = apunta hacia arriba (para volver a abrir)
        chevron.style.rotate = new Rotate(oculta ? 225f : 45f);
    }
}