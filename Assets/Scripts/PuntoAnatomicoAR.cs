using UnityEngine;

/// <summary>
/// Componente para cada punto anatómico interactivo del pez en 2D.
/// Muestra la información oficial del panel al ser seleccionado.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class PuntoAnatomicoAR : MonoBehaviour
{
    [Header("Información oficial del panel")]
    public string categoria = "ROSTRO";
    public string nombrePunto = "Boca";

    [Header("Apariencia 2D")]
    public Color colorNormal = new Color(0.22f, 0.74f, 0.97f, 0.95f);
    public Color colorSeleccionado = new Color(0.85f, 0.98f, 1f, 1f);

    private Renderer renderVisual;
    private MaterialPropertyBlock propBlock;
    private Vector3 escalaOriginal;
    private bool estaSeleccionado;

    void Awake()
    {
        renderVisual = GetComponent<Renderer>();
        if (renderVisual == null)
        {
            renderVisual = GetComponentInChildren<Renderer>();
        }
        propBlock = new MaterialPropertyBlock();
        escalaOriginal = transform.localScale;
    }

    void Start()
    {
        AplicarEstadoVisual();
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

        // Crecimiento sutil al seleccionarlo para dar respuesta táctil
        transform.localScale = estaSeleccionado ? (escalaOriginal * 1.35f) : escalaOriginal;
    }
}
