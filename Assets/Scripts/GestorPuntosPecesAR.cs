using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gestiona la detección táctil de los puntos 2D del pez y muestra
/// la tarjeta con la categoría y nombre según el panel oficial de la estación.
/// Compatible con el Nuevo Input System de Unity (activeInputHandler: 1).
/// </summary>
public class GestorPuntosPecesAR : MonoBehaviour
{
    private PuntoAnatomicoAR puntoSeleccionadoActual;
    private Camera camara;

    // Elementos de interfaz (UI Toolkit)
    private VisualElement raizUI;
    private VisualElement tarjetaPunto;
    private Label lblCategoria;
    private Label lblNombre;
    private Button btnCerrar;

    void Start()
    {
        BuscarCamara();
        CrearTarjetaUI();
    }

    void OnDisable()
    {
        OcultarTarjeta();
    }

    void Update()
    {
        ProcesarToque();
    }

    private void BuscarCamara()
    {
        camara = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
    }

    private void CrearTarjetaUI()
    {
        if (raizUI != null && tarjetaPunto != null) return;

        var docUI = FindAnyObjectByType<UIDocument>();
        if (docUI == null || docUI.rootVisualElement == null) return;

        raizUI = docUI.rootVisualElement;

        // Limpiar tarjeta previa si existiera
        var previa = raizUI.Q<VisualElement>("tarjeta-punto-anatomia");
        if (previa != null) raizUI.Remove(previa);

        // Tarjeta flotante en la parte superior-central de la pantalla
        tarjetaPunto = new VisualElement();
        tarjetaPunto.name = "tarjeta-punto-anatomia";
        tarjetaPunto.style.position = Position.Absolute;
        tarjetaPunto.style.top = 130;
        tarjetaPunto.style.alignSelf = Align.Center;
        tarjetaPunto.style.backgroundColor = new Color(0.04f, 0.12f, 0.22f, 0.94f); // Azul oscuro institucional
        tarjetaPunto.style.borderTopLeftRadius = 26;
        tarjetaPunto.style.borderTopRightRadius = 26;
        tarjetaPunto.style.borderBottomLeftRadius = 26;
        tarjetaPunto.style.borderBottomRightRadius = 26;
        tarjetaPunto.style.borderLeftWidth = 2.5f;
        tarjetaPunto.style.borderRightWidth = 2.5f;
        tarjetaPunto.style.borderTopWidth = 2.5f;
        tarjetaPunto.style.borderBottomWidth = 2.5f;
        tarjetaPunto.style.borderLeftColor = new Color(0.22f, 0.74f, 0.97f, 0.9f); // Borde cian sutil
        tarjetaPunto.style.borderRightColor = new Color(0.22f, 0.74f, 0.97f, 0.9f);
        tarjetaPunto.style.borderTopColor = new Color(0.22f, 0.74f, 0.97f, 0.9f);
        tarjetaPunto.style.borderBottomColor = new Color(0.22f, 0.74f, 0.97f, 0.9f);
        tarjetaPunto.style.paddingLeft = 32;
        tarjetaPunto.style.paddingRight = 24;
        tarjetaPunto.style.paddingTop = 20;
        tarjetaPunto.style.paddingBottom = 20;
        tarjetaPunto.style.flexDirection = FlexDirection.Row;
        tarjetaPunto.style.alignItems = Align.Center;
        tarjetaPunto.style.minWidth = 360;
        tarjetaPunto.style.maxWidth = 600;
        tarjetaPunto.style.display = DisplayStyle.None; // Oculta al inicio

        // Contenedor de textos
        var contenedorTextos = new VisualElement();
        contenedorTextos.style.flexGrow = 1;

        lblCategoria = new Label("CATEGORÍA");
        lblCategoria.style.fontSize = 20;
        lblCategoria.style.unityFontStyleAndWeight = FontStyle.Bold;
        lblCategoria.style.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Cian suave
        lblCategoria.style.marginBottom = 4;

        lblNombre = new Label("Nombre del Punto");
        lblNombre.style.fontSize = 36;
        lblNombre.style.unityFontStyleAndWeight = FontStyle.Bold;
        lblNombre.style.color = Color.white;

        contenedorTextos.Add(lblCategoria);
        contenedorTextos.Add(lblNombre);

        // Botón cerrar (X)
        btnCerrar = new Button(() => OcultarTarjeta());
        btnCerrar.text = "✕";
        btnCerrar.style.width = 54;
        btnCerrar.style.height = 54;
        btnCerrar.style.fontSize = 26;
        btnCerrar.style.unityFontStyleAndWeight = FontStyle.Bold;
        btnCerrar.style.color = Color.white;
        btnCerrar.style.backgroundColor = new Color(0.20f, 0.35f, 0.55f, 0.95f);
        btnCerrar.style.borderTopLeftRadius = 27;
        btnCerrar.style.borderTopRightRadius = 27;
        btnCerrar.style.borderBottomLeftRadius = 27;
        btnCerrar.style.borderBottomRightRadius = 27;
        btnCerrar.style.borderLeftWidth = 0;
        btnCerrar.style.borderRightWidth = 0;
        btnCerrar.style.borderTopWidth = 0;
        btnCerrar.style.borderBottomWidth = 0;
        btnCerrar.style.marginLeft = 20;

        tarjetaPunto.Add(contenedorTextos);
        tarjetaPunto.Add(btnCerrar);

        raizUI.Add(tarjetaPunto);
    }

    private void ProcesarToque()
    {
        Vector2 posicionToque = Vector2.zero;
        bool huboToque = false;

#if ENABLE_INPUT_SYSTEM
        // Nuevo Input System (Universal Pointer: funciona para touch en móvil y click en PC)
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            posicionToque = Pointer.current.position.ReadValue();
            huboToque = true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (!huboToque)
        {
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                posicionToque = Input.GetTouch(0).position;
                huboToque = true;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                posicionToque = Input.mousePosition;
                huboToque = true;
            }
        }
#endif

        if (!huboToque) return;

        // Ignorar toques en la parte inferior (zona del botón de la ficha colapsada)
        if (posicionToque.y < Screen.height * 0.18f)
        {
            return;
        }

        if (camara == null) BuscarCamara();
        if (camara == null) return;

        Ray ray = camara.ScreenPointToRay(posicionToque);

        // RaycastAll forzando la detección de triggers (QueryTriggerInteraction.Collide)
        RaycastHit[] hits = Physics.RaycastAll(ray, 50f, ~0, QueryTriggerInteraction.Collide);
        if (hits != null && hits.Length > 0)
        {
            // Ordenar por distancia para tomar el collider más cercano a la vista
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                PuntoAnatomicoAR punto = hit.collider.GetComponent<PuntoAnatomicoAR>();
                if (punto != null)
                {
                    MostrarPunto(punto);
                    return;
                }
            }
        }
    }

    public void MostrarPunto(PuntoAnatomicoAR punto)
    {
        if (puntoSeleccionadoActual != null)
        {
            puntoSeleccionadoActual.Deseleccionar();
        }

        puntoSeleccionadoActual = punto;
        puntoSeleccionadoActual.Seleccionar();

        if (tarjetaPunto == null || raizUI == null)
        {
            CrearTarjetaUI();
        }

        if (tarjetaPunto != null)
        {
            lblCategoria.text = punto.categoria.ToUpper();
            lblNombre.text = punto.nombrePunto;
            tarjetaPunto.style.display = DisplayStyle.Flex;
        }
    }

    public void OcultarTarjeta()
    {
        if (puntoSeleccionadoActual != null)
        {
            puntoSeleccionadoActual.Deseleccionar();
            puntoSeleccionadoActual = null;
        }

        if (tarjetaPunto != null)
        {
            tarjetaPunto.style.display = DisplayStyle.None;
        }
    }

    void OnDestroy()
    {
        if (tarjetaPunto != null && raizUI != null)
        {
            raizUI.Remove(tarjetaPunto);
        }
    }
}
