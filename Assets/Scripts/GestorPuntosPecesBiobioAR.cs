using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gestiona los 9 puntos interactivos 2D y la detección táctil de cada pez del Biobío (Estación 4).
/// Muestra una tarjeta flotante en UI Toolkit con exactamente las 3 informaciones oficiales:
/// 1. Nombre científico
/// 2. Nombre local
/// 3. Hábitat
/// Sigue el mismo patrón estético y de interacción que el esqueleto de la Estación 3.
/// </summary>
public class GestorPuntosPecesBiobioAR : MonoBehaviour
{
    private PuntoPezBiobioAR puntoSeleccionadoActual;
    private Camera camara;

    // Elementos UI Toolkit
    private VisualElement raizUI;
    private VisualElement tarjetaPunto;
    private Label lblNombreComun;
    private Label lblCientifico;
    private Label lblLocal;
    private Label lblHabitat;
    private Button btnCerrar;

    // Mapeo de colisionadores para responder tanto al punto como al cuerpo del pez
    private readonly Dictionary<Collider, PuntoPezBiobioAR> mapaColisionadores = new Dictionary<Collider, PuntoPezBiobioAR>();

    void Awake()
    {
        BuscarCamara();
    }

    void Start()
    {
        BuscarCamara();
        CrearTarjetaUI();
        AsegurarPuntosYColisionadores();
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

    /// <summary>
    /// Construye la tarjeta UI Toolkit siguiendo el diseño exacto de la Estación 3,
    /// adaptada a los 3 campos solicitados.
    /// </summary>
    private void CrearTarjetaUI()
    {
        if (raizUI != null && tarjetaPunto != null) return;

        var docUI = FindAnyObjectByType<UIDocument>();
        if (docUI == null || docUI.rootVisualElement == null) return;

        raizUI = docUI.rootVisualElement;

        // Limpiar tarjeta previa si ya existiera
        var previa = raizUI.Q<VisualElement>("tarjeta-punto-pez-biobio");
        if (previa != null) raizUI.Remove(previa);

        tarjetaPunto = new VisualElement();
        tarjetaPunto.name = "tarjeta-punto-pez-biobio";
        tarjetaPunto.style.position = Position.Absolute;
        tarjetaPunto.style.top = 110;
        tarjetaPunto.style.alignSelf = Align.Center;
        tarjetaPunto.style.backgroundColor = new Color(0.04f, 0.12f, 0.22f, 0.95f); // Azul oscuro institucional
        tarjetaPunto.style.borderTopLeftRadius = 24;
        tarjetaPunto.style.borderTopRightRadius = 24;
        tarjetaPunto.style.borderBottomLeftRadius = 24;
        tarjetaPunto.style.borderBottomRightRadius = 24;
        tarjetaPunto.style.borderLeftWidth = 2.5f;
        tarjetaPunto.style.borderRightWidth = 2.5f;
        tarjetaPunto.style.borderTopWidth = 2.5f;
        tarjetaPunto.style.borderBottomWidth = 2.5f;
        tarjetaPunto.style.borderLeftColor = new Color(0.22f, 0.74f, 0.97f, 0.92f); // Borde cian sutil
        tarjetaPunto.style.borderRightColor = new Color(0.22f, 0.74f, 0.97f, 0.92f);
        tarjetaPunto.style.borderTopColor = new Color(0.22f, 0.74f, 0.97f, 0.92f);
        tarjetaPunto.style.borderBottomColor = new Color(0.22f, 0.74f, 0.97f, 0.92f);
        tarjetaPunto.style.paddingLeft = 26;
        tarjetaPunto.style.paddingRight = 20;
        tarjetaPunto.style.paddingTop = 18;
        tarjetaPunto.style.paddingBottom = 20;
        tarjetaPunto.style.minWidth = 340;
        tarjetaPunto.style.maxWidth = 600;
        tarjetaPunto.style.display = DisplayStyle.None; // Oculta al inicio

        // --- FILA SUPERIOR: TÍTULO Y BOTÓN CERRAR ---
        var filaCabecera = new VisualElement();
        filaCabecera.style.flexDirection = FlexDirection.Row;
        filaCabecera.style.alignItems = Align.Center;
        filaCabecera.style.justifyContent = Justify.SpaceBetween;
        filaCabecera.style.marginBottom = 10;

        lblNombreComun = new Label("NOMBRE DEL PEZ");
        lblNombreComun.style.fontSize = 26;
        lblNombreComun.style.unityFontStyleAndWeight = FontStyle.Bold;
        lblNombreComun.style.color = Color.white;
        lblNombreComun.style.flexGrow = 1;
        lblNombreComun.style.whiteSpace = WhiteSpace.Normal;

        btnCerrar = new Button(() => OcultarTarjeta());
        btnCerrar.text = "✕";
        btnCerrar.style.width = 46;
        btnCerrar.style.height = 46;
        btnCerrar.style.fontSize = 22;
        btnCerrar.style.unityFontStyleAndWeight = FontStyle.Bold;
        btnCerrar.style.color = Color.white;
        btnCerrar.style.backgroundColor = new Color(0.20f, 0.35f, 0.55f, 0.95f);
        btnCerrar.style.borderTopLeftRadius = 23;
        btnCerrar.style.borderTopRightRadius = 23;
        btnCerrar.style.borderBottomLeftRadius = 23;
        btnCerrar.style.borderBottomRightRadius = 23;
        btnCerrar.style.borderLeftWidth = 0;
        btnCerrar.style.borderRightWidth = 0;
        btnCerrar.style.borderTopWidth = 0;
        btnCerrar.style.borderBottomWidth = 0;
        btnCerrar.style.marginLeft = 16;

        filaCabecera.Add(lblNombreComun);
        filaCabecera.Add(btnCerrar);
        tarjetaPunto.Add(filaCabecera);

        // Línea divisoria cian
        var lineaSeparador = new VisualElement();
        lineaSeparador.style.height = 1.5f;
        lineaSeparador.style.backgroundColor = new Color(0.22f, 0.74f, 0.97f, 0.40f);
        lineaSeparador.style.marginBottom = 12;
        tarjetaPunto.Add(lineaSeparador);

        // --- 1. NOMBRE CIENTÍFICO ---
        var contCientifico = new VisualElement();
        contCientifico.style.flexDirection = FlexDirection.Row;
        contCientifico.style.marginBottom = 6;
        contCientifico.style.flexWrap = Wrap.Wrap;

        var lblEtiquetaCientifico = new Label("Nombre científico: ");
        lblEtiquetaCientifico.style.fontSize = 17;
        lblEtiquetaCientifico.style.unityFontStyleAndWeight = FontStyle.Bold;
        lblEtiquetaCientifico.style.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Cian

        lblCientifico = new Label("");
        lblCientifico.style.fontSize = 17;
        lblCientifico.style.unityFontStyleAndWeight = FontStyle.Italic;
        lblCientifico.style.color = Color.white;
        lblCientifico.style.whiteSpace = WhiteSpace.Normal;

        contCientifico.Add(lblEtiquetaCientifico);
        contCientifico.Add(lblCientifico);
        tarjetaPunto.Add(contCientifico);

        // --- 2. NOMBRE LOCAL ---
        var contLocal = new VisualElement();
        contLocal.style.flexDirection = FlexDirection.Row;
        contLocal.style.marginBottom = 6;
        contLocal.style.flexWrap = Wrap.Wrap;

        var lblEtiquetaLocal = new Label("Nombre local: ");
        lblEtiquetaLocal.style.fontSize = 17;
        lblEtiquetaLocal.style.unityFontStyleAndWeight = FontStyle.Bold;
        lblEtiquetaLocal.style.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Cian

        lblLocal = new Label("");
        lblLocal.style.fontSize = 17;
        lblLocal.style.color = new Color(0.92f, 0.95f, 0.98f, 1f);
        lblLocal.style.whiteSpace = WhiteSpace.Normal;

        contLocal.Add(lblEtiquetaLocal);
        contLocal.Add(lblLocal);
        tarjetaPunto.Add(contLocal);

        // --- 3. HÁBITAT ---
        var contHabitat = new VisualElement();
        contHabitat.style.flexDirection = FlexDirection.Column;
        contHabitat.style.marginTop = 2;

        var lblEtiquetaHabitat = new Label("Hábitat: ");
        lblEtiquetaHabitat.style.fontSize = 17;
        lblEtiquetaHabitat.style.unityFontStyleAndWeight = FontStyle.Bold;
        lblEtiquetaHabitat.style.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Cian
        lblEtiquetaHabitat.style.marginBottom = 2;

        lblHabitat = new Label("");
        lblHabitat.style.fontSize = 16;
        lblHabitat.style.color = new Color(0.88f, 0.92f, 0.96f, 1f);
        lblHabitat.style.whiteSpace = WhiteSpace.Normal;

        contHabitat.Add(lblEtiquetaHabitat);
        contHabitat.Add(lblHabitat);
        tarjetaPunto.Add(contHabitat);

        raizUI.Add(tarjetaPunto);
    }

    /// <summary>
    /// Registra los 9 puntos interactivos y asegura que tocar el pez o su punto abra la ficha.
    /// </summary>
    public void AsegurarPuntosYColisionadores()
    {
        mapaColisionadores.Clear();

        // 1. Obtener todos los puntos interactivos
        PuntoPezBiobioAR[] puntos = GetComponentsInChildren<PuntoPezBiobioAR>(true);
        foreach (var p in puntos)
        {
            Collider col = p.GetComponent<Collider>();
            if (col != null && !mapaColisionadores.ContainsKey(col))
            {
                mapaColisionadores.Add(col, p);
            }
        }

        // 2. Asegurar que los cuerpos de los peces también respondan al toque
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform hijo = transform.GetChild(i);
            if (hijo.name.StartsWith("Puntos") || hijo.name.StartsWith("Punto_")) continue;

            Collider colPez = hijo.GetComponent<Collider>();
            if (colPez == null)
            {
                Renderer rend = hijo.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    BoxCollider box = hijo.gameObject.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    Bounds b = rend.localBounds;
                    if (b.size.sqrMagnitude > 0.001f)
                    {
                        box.center = b.center;
                        box.size = b.size * 1.15f;
                    }
                    colPez = box;
                }
            }

            if (colPez != null)
            {
                // Asociar al punto más cercano
                PuntoPezBiobioAR puntoCercano = EncontrarPuntoMasCercano(hijo.position, puntos);
                if (puntoCercano != null && !mapaColisionadores.ContainsKey(colPez))
                {
                    mapaColisionadores.Add(colPez, puntoCercano);
                }
            }
        }
    }

    private PuntoPezBiobioAR EncontrarPuntoMasCercano(Vector3 posicion, PuntoPezBiobioAR[] puntos)
    {
        if (puntos == null || puntos.Length == 0) return null;
        PuntoPezBiobioAR mejor = null;
        float minDist = float.MaxValue;
        foreach (var p in puntos)
        {
            float d = Vector3.Distance(posicion, p.transform.position);
            if (d < minDist)
            {
                minDist = d;
                mejor = p;
            }
        }
        return mejor;
    }

    private void ProcesarToque()
    {
        Vector2 posicionToque = Vector2.zero;
        bool huboToque = false;

#if ENABLE_INPUT_SYSTEM
        // Nuevo Input System (Táctil móvil y Mouse PC)
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

        // Ignorar toques en la franja inferior (área del botón de la ficha colapsada)
        if (posicionToque.y < Screen.height * 0.18f)
        {
            return;
        }

        if (camara == null) BuscarCamara();
        if (camara == null) return;

        Ray ray = camara.ScreenPointToRay(posicionToque);

        // Raycast compatible con triggers
        RaycastHit[] hits = Physics.RaycastAll(ray, 50f, ~0, QueryTriggerInteraction.Collide);
        if (hits != null && hits.Length > 0)
        {
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // 1. Búsqueda en el diccionario de mapeo
                if (mapaColisionadores.TryGetValue(hit.collider, out PuntoPezBiobioAR p))
                {
                    MostrarPunto(p);
                    return;
                }

                // 2. Búsqueda directa en el componente del objeto golpeado
                PuntoPezBiobioAR punto = hit.collider.GetComponent<PuntoPezBiobioAR>();
                if (punto == null) punto = hit.collider.GetComponentInChildren<PuntoPezBiobioAR>();
                if (punto == null) punto = hit.collider.GetComponentInParent<PuntoPezBiobioAR>();

                if (punto != null)
                {
                    MostrarPunto(punto);
                    return;
                }
            }
        }
    }

    public void MostrarPunto(PuntoPezBiobioAR punto)
    {
        if (punto == null) return;

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
            lblNombreComun.text = punto.nombreComun;
            lblCientifico.text = punto.nombreCientifico;
            lblLocal.text = punto.nombreLocal;
            lblHabitat.text = punto.habitat;

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
