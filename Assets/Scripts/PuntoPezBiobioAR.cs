using UnityEngine;

/// <summary>
/// Punto interactivo para cada pez del Biobío en Realidad Aumentada (Estación 4).
/// Almacena las 3 informaciones oficiales del panel del sendero:
/// 1. Nombre científico
/// 2. Nombre local
/// 3. Hábitat
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class PuntoPezBiobioAR : MonoBehaviour
{
    [Header("Identificación")]
    public string nombreComun;

    [Header("3 Informaciones Oficiales del Panel")]
    public string nombreCientifico;
    public string nombreLocal;
    [TextArea(2, 4)]
    public string habitat;

    [Header("Apariencia Visual")]
    public Color colorNormal = new Color(0.22f, 0.74f, 0.97f, 0.95f);      // Cian institucional
    public Color colorSeleccionado = new Color(0.85f, 0.98f, 1f, 1f);        // Blanco brillante

    private Renderer renderVisual;
    private MaterialPropertyBlock propBlock;
    private Vector3 escalaOriginal = Vector3.one;
    private bool estaSeleccionado;
    private Camera camara;

    void Awake()
    {
        renderVisual = GetComponent<Renderer>();
        if (renderVisual == null) renderVisual = GetComponentInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
        if (transform.localScale != Vector3.zero)
        {
            escalaOriginal = transform.localScale;
        }
        camara = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
    }

    void Start()
    {
        if (escalaOriginal == Vector3.zero || escalaOriginal == Vector3.one)
        {
            if (transform.localScale != Vector3.zero)
            {
                escalaOriginal = transform.localScale;
            }
        }
        AplicarEstadoVisual();
    }

    public void AsignarEscalaOriginal(Vector3 escala)
    {
        escalaOriginal = escala;
        transform.localScale = escala;
    }

    void LateUpdate()
    {
        // Billboard: hace que el punto mire perpendicularmente a la pantalla en AR
        if (camara == null) camara = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (camara != null)
        {
            transform.rotation = camara.transform.rotation;
        }
    }

    public void Seleccionar()
    {
        estaSeleccionado = true;
        AplicarEstadoVisual();
    }

    public void Deseleccionar()
    {
        estaSeleccionado = false;
        AplicarEstadoVisual();
    }

    private void AplicarEstadoVisual()
    {
        if (renderVisual != null)
        {
            Color c = estaSeleccionado ? colorSeleccionado : colorNormal;
            if (propBlock == null) propBlock = new MaterialPropertyBlock();
            renderVisual.GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", c);
            propBlock.SetColor("_Color", c);
            renderVisual.SetPropertyBlock(propBlock);
        }

        // Crecimiento táctil al seleccionarlo
        transform.localScale = estaSeleccionado ? (escalaOriginal * 1.35f) : escalaOriginal;
    }
}
