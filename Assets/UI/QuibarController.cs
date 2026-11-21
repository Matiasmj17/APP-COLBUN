using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Va en el objeto "Interfaz" (junto a NavegacionController y DetalleEstacionController).
// Quibar se esconde en una estación al azar. Al escanear esa estación aparece y se puede "atrapar":
// entonces se desbloquea la pestaña Quibar, donde queda su trofeo.
[RequireComponent(typeof(UIDocument))]
public class QuibarController : MonoBehaviour
{
    [Header("Arrastra aquí la imagen de Quibar (quibar2, con fondo transparente)")]
    public Texture2D imagenQuibar;

    [Header("Estaciones que ya tienen contenido (1 a 5). Quibar se esconde en una de ellas")]
    public int[] estacionesConContenido = { 1 };

    [Header("Quibar en AR: alto en metros (0 = automático según el tamaño del modelo)")]
    public float alturaQuibar = 0f;

    [Header("Separación entre el modelo y Quibar, en metros")]
    public float separacion = 0.1f;

    const string ClaveEstacion   = "quibar_estacion";    // estación donde está escondido (se sortea una vez)
    const string ClaveEncontrado = "quibar_encontrado";  // 0 = escondido, 1 = ya lo atrapó
    const string ClaveHallazgo   = "quibar_hallazgo";    // estación donde lo encontró

    VisualElement raiz, capa, candado;
    VisualElement pantallaPasaporte, pantallaEscaner, pantallaDetalle, pantallaEncuesta, pantallaQuibar;
    Button btnQuibar;
    DetalleEstacionController detalle;

    // --- Quibar flotando en AR ---
    const float AnchoPanelAR = 560f;
    GameObject quibarAR, modeloAnclaje;
    Sprite spriteAR;
    Renderer[] renderersModelo;
    Camera cam;
    VisualElement panelAR;
    float altoAR;
    bool teniaModelo, primerPosicionado;

    public bool EstaBloqueado() { return PlayerPrefs.GetInt(ClaveEncontrado, 0) == 0; }

    void OnEnable()
    {
        raiz = GetComponent<UIDocument>().rootVisualElement;
        detalle = GetComponent<DetalleEstacionController>();

        pantallaPasaporte = raiz.Q<VisualElement>("pantalla-pasaporte");
        pantallaEscaner   = raiz.Q<VisualElement>("pantalla-escaner");
        pantallaDetalle   = raiz.Q<VisualElement>("pantalla-detalle");
        pantallaEncuesta  = raiz.Q<VisualElement>("pantalla-encuesta");
        pantallaQuibar    = raiz.Q<VisualElement>("pantalla-kiba");   // es la pantalla provisoria que ya existe
        btnQuibar         = raiz.Q<Button>("btn-nav-kiba");

        ElegirEscondite();
        ActualizarBoton();
        ConstruirTrofeo();
    }

    void OnDisable() { LimpiarAR(); }

    // ---------- Escondite ----------
    void ElegirEscondite()
    {
        if (PlayerPrefs.GetInt(ClaveEstacion, 0) != 0) return;               // ya se sorteó
        if (estacionesConContenido == null || estacionesConContenido.Length == 0) return;
        int elegida = estacionesConContenido[UnityEngine.Random.Range(0, estacionesConContenido.Length)];
        PlayerPrefs.SetInt(ClaveEstacion, elegida);
        PlayerPrefs.Save();
    }

    // Lo llama DetalleEstacionController cada vez que se abre la ficha de una estación
    public void AlMostrarEstacion(int numero)
    {
        if (!EstaBloqueado()) return;                                         // ya lo atrapó
        if (numero != PlayerPrefs.GetInt(ClaveEstacion, 0)) return;           // no está escondido aquí
        MostrarAparicion(numero);
    }

    // ---------- Botón de la barra (nombre y candado) ----------
    void ActualizarBoton()
    {
        if (btnQuibar == null) return;
        var etiqueta = btnQuibar.Q<Label>();
        if (etiqueta != null) etiqueta.text = "Quibar";

        bool bloqueado = EstaBloqueado();
        btnQuibar.style.opacity = bloqueado ? 0.45f : 1f;

        if (bloqueado && candado == null)
        {
            candado = CrearCandado();
            btnQuibar.Add(candado);
        }
        else if (!bloqueado && candado != null)
        {
            candado.RemoveFromHierarchy();
            candado = null;
        }
    }

    // Se llama desde NavegacionController cuando tocan la pestaña estando bloqueada
    public void MostrarPista()
    {
        CerrarCapa();
        capa = CrearCapa();
        var t = CrearTarjeta(capa);
        t.Add(Texto("Quibar está escondido", 56, "#0B2540", true));
        t.Add(Imagen(360, 0.35f));
        t.Add(Texto("Se esconde en una de las estaciones del túnel. ¡Escanea los QR para encontrarlo!", 36, "#475569", false));
        t.Add(Boton("Entendido", CerrarCapa));
    }

    // ---------- Aparición y captura ----------
    void MostrarAparicion(int numero)
    {
        LimpiarAR();
        if (imagenQuibar == null) { Debug.LogWarning("QuibarController: falta arrastrar la imagen de Quibar."); return; }

        cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
        if (cam == null) return;

        modeloAnclaje = detalle != null ? detalle.ModeloActual : null;
        teniaModelo = modeloAnclaje != null;
        renderersModelo = teniaModelo ? modeloAnclaje.GetComponentsInChildren<Renderer>() : null;

        // Tamaño de Quibar: el parámetro, o ~60% del alto del modelo
        Bounds b;
        if (alturaQuibar > 0f) altoAR = alturaQuibar;
        else if (TieneBounds(out b)) altoAR = Mathf.Clamp(b.size.y * 0.6f, 0.15f, 0.6f);
        else altoAR = 0.3f;

        // El "modelo" de Quibar: un sprite plano (la foto) que siempre mira a la cámara
        quibarAR = new GameObject("QuibarAR");
        spriteAR = Sprite.Create(imagenQuibar, new Rect(0, 0, imagenQuibar.width, imagenQuibar.height),
                                 new Vector2(0.5f, 0.5f), imagenQuibar.height);
        quibarAR.AddComponent<SpriteRenderer>().sprite = spriteAR;
        quibarAR.transform.localScale = Vector3.one * altoAR;
        primerPosicionado = false;

        // Botones flotantes que siguen a Quibar en pantalla
        panelAR = new VisualElement();
        panelAR.pickingMode = PickingMode.Ignore;                             // solo los botones reciben toques
        panelAR.style.position = Position.Absolute;
        panelAR.style.width = AnchoPanelAR;
        panelAR.style.alignItems = Align.Center;
        raiz.Add(panelAR);

        panelAR.Add(Etiqueta("¡Quibar está aquí!"));
        panelAR.Add(Boton("¡Atrapar!", () => Atrapar(numero)));
    }

    void Atrapar(int numero)
    {
        PlayerPrefs.SetInt(ClaveEncontrado, 1);
        PlayerPrefs.SetInt(ClaveHallazgo, numero);
        PlayerPrefs.Save();
        ActualizarBoton();
        ConstruirTrofeo();

        if (panelAR == null) return;
        panelAR.Clear();
        panelAR.Add(Etiqueta("¡Atrapaste a Quibar!"));
        panelAR.Add(Boton("Ver trofeo", IrATrofeo));
        panelAR.Add(BotonSecundario("Seguir en la estación", LimpiarAR));
    }

    void IrATrofeo()
    {
        LimpiarAR();
        CerrarCapa();
        if (detalle != null) detalle.Cerrar();                                // cierra la ficha y borra el modelo 3D
        Ocultar(pantallaPasaporte); Ocultar(pantallaEscaner); Ocultar(pantallaDetalle); Ocultar(pantallaEncuesta);
        if (pantallaQuibar != null) pantallaQuibar.style.display = DisplayStyle.Flex;
    }

    // ---------- Quibar en AR: posición y seguimiento ----------
    void LateUpdate()
    {
        if (quibarAR == null) return;

        // Se limpia solo si el modelo se destruyó o se cerró la ficha
        bool fichaCerrada = pantallaDetalle != null && pantallaDetalle.style.display.value == DisplayStyle.None;
        if ((teniaModelo && modeloAnclaje == null) || fichaCerrada || cam == null) { LimpiarAR(); return; }

        // Dirección "derecha" de la cámara, sin inclinación
        Vector3 derecha = cam.transform.right; derecha.y = 0f;
        derecha = derecha.sqrMagnitude < 0.001f ? Vector3.right : derecha.normalized;
        Vector3 adelante = cam.transform.forward; adelante.y = 0f;
        adelante = adelante.sqrMagnitude < 0.001f ? Vector3.forward : adelante.normalized;

        // Posición objetivo: al costado del modelo (o frente a la cámara si no hay modelo)
        Vector3 objetivo;
        Bounds b;
        if (TieneBounds(out b))
        {
            float radio = Mathf.Max(b.extents.x, b.extents.z);
            objetivo = b.center + derecha * (radio + separacion + altoAR * 0.4f);
            objetivo.y = b.min.y + altoAR * 0.5f;
        }
        else if (teniaModelo)
        {
            objetivo = modeloAnclaje.transform.position + derecha * (0.3f + separacion);
            objetivo.y += altoAR * 0.5f;
        }
        else
        {
            objetivo = cam.transform.position + adelante * 1.5f + derecha * 0.4f;
            objetivo.y = cam.transform.position.y - 0.4f;
        }
        objetivo.y += Mathf.Sin(Time.time * 2f) * altoAR * 0.03f;             // flotadito suave

        var tr = quibarAR.transform;
        tr.position = primerPosicionado ? Vector3.Lerp(tr.position, objetivo, 1f - Mathf.Exp(-8f * Time.deltaTime)) : objetivo;
        primerPosicionado = true;
        tr.rotation = Quaternion.LookRotation(adelante, Vector3.up);          // siempre mirando a la cámara

        PosicionarPanelAR();
    }

    void PosicionarPanelAR()
    {
        if (panelAR == null) return;

        Vector3 cabeza = quibarAR.transform.position + Vector3.up * (altoAR * 0.55f);
        bool visible = cam.WorldToViewportPoint(cabeza).z > 0f;
        panelAR.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        if (!visible) return;

        float W = raiz.resolvedStyle.width, H = raiz.resolvedStyle.height;
        if (float.IsNaN(W) || float.IsNaN(H) || W <= 0f || H <= 0f) return;

        Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(raiz.panel, cabeza, cam);
        panelAR.style.left   = Mathf.Clamp(p.x - AnchoPanelAR * 0.5f, 20f, Mathf.Max(20f, W - AnchoPanelAR - 20f));
        panelAR.style.bottom = Mathf.Clamp(H - p.y + 20f, 20f, Mathf.Max(20f, H - 420f));   // los botones quedan encima de su cabeza
    }

    bool TieneBounds(out Bounds b)
    {
        b = default(Bounds);
        if (renderersModelo == null) return false;
        bool primero = true;
        foreach (var r in renderersModelo)
        {
            if (r == null) continue;
            if (primero) { b = r.bounds; primero = false; }
            else b.Encapsulate(r.bounds);
        }
        return !primero;
    }

    void LimpiarAR()
    {
        if (panelAR != null) panelAR.RemoveFromHierarchy();
        panelAR = null;
        if (quibarAR != null) Destroy(quibarAR);
        quibarAR = null;
        if (spriteAR != null) Destroy(spriteAR);
        spriteAR = null;
        modeloAnclaje = null; renderersModelo = null; teniaModelo = false;
    }

    // ---------- Pantalla del trofeo (pestaña Quibar) ----------
    void ConstruirTrofeo()
    {
        if (pantallaQuibar == null) return;
        pantallaQuibar.Clear();
        if (EstaBloqueado()) return;

        var cont = new VisualElement();
        cont.style.flexGrow = 1;
        cont.style.alignItems = Align.Center;
        cont.style.justifyContent = Justify.Center;
        cont.style.paddingBottom = 200;                                       // deja libre la barra inferior
        cont.style.paddingLeft = 60; cont.style.paddingRight = 60;

        var etiqueta = Texto("TROFEO DESBLOQUEADO", 34, "#78350F", true);
        etiqueta.style.backgroundColor = Hex("#FDE68A");
        Radio(etiqueta, 40);
        etiqueta.style.paddingTop = 14; etiqueta.style.paddingBottom = 14;
        etiqueta.style.paddingLeft = 36; etiqueta.style.paddingRight = 36;
        etiqueta.style.marginBottom = 40;
        cont.Add(etiqueta);

        var marco = new VisualElement();                                      // círculo dorado tipo trofeo
        marco.style.width = 560; marco.style.height = 560;
        Radio(marco, 280);
        Borde(marco, 14, Hex("#F5B301"));
        marco.style.backgroundColor = Hex("#FFF7E0");
        marco.style.alignItems = Align.Center;
        marco.style.justifyContent = Justify.Center;
        marco.Add(Imagen(470, 1f));
        cont.Add(marco);

        var titulo = Texto("¡Encontraste a Quibar!", 60, "#0B2540", true);
        titulo.style.marginTop = 50;
        cont.Add(titulo);

        int estacion = PlayerPrefs.GetInt(ClaveHallazgo, 0);
        string donde = estacion > 0 ? "Lo encontraste escondido en la Estación " + estacion + "." : "Lo encontraste escondido en el túnel.";
        cont.Add(Texto(donde, 38, "#475569", false));
        cont.Add(Texto("Sigue recorriendo el túnel para descubrir más estaciones.", 34, "#64748B", false));

        pantallaQuibar.Add(cont);
    }

    // ---------- Pruebas desde el Inspector (clic derecho sobre el componente) ----------
    [ContextMenu("Probar aparición en estación 1")]
    void ProbarAparicion()
    {
        if (pantallaDetalle != null) pantallaDetalle.style.display = DisplayStyle.Flex;
        MostrarAparicion(1);
    }

    [ContextMenu("Reiniciar Quibar")]
    void Reiniciar()
    {
        PlayerPrefs.DeleteKey(ClaveEstacion);
        PlayerPrefs.DeleteKey(ClaveEncontrado);
        PlayerPrefs.DeleteKey(ClaveHallazgo);
        PlayerPrefs.Save();
        if (raiz == null) return;
        ElegirEscondite();
        ActualizarBoton();
        ConstruirTrofeo();
    }

    // ---------- Piezas de interfaz (todo se arma por código, sin tocar el UXML) ----------
    void CerrarCapa()
    {
        if (capa != null) capa.RemoveFromHierarchy();
        capa = null;
    }

    static void Ocultar(VisualElement e) { if (e != null) e.style.display = DisplayStyle.None; }

    VisualElement CrearCapa()
    {
        var c = new VisualElement();
        c.style.position = Position.Absolute;
        c.style.left = 0; c.style.right = 0; c.style.top = 0; c.style.bottom = 0;
        c.style.backgroundColor = new Color(0f, 0f, 0f, 0.72f);
        c.style.alignItems = Align.Center;
        c.style.justifyContent = Justify.Center;
        raiz.Add(c);                                                          // al final = se dibuja encima de todo
        return c;
    }

    VisualElement CrearTarjeta(VisualElement padre)
    {
        var t = new VisualElement();
        t.style.width = Length.Percent(86);
        t.style.backgroundColor = Color.white;
        Radio(t, 60);
        t.style.paddingTop = 50; t.style.paddingBottom = 50;
        t.style.paddingLeft = 50; t.style.paddingRight = 50;
        t.style.alignItems = Align.Center;
        padre.Add(t);
        return t;
    }

    Label Texto(string texto, float tam, string color, bool negrita)
    {
        var l = new Label(texto);
        l.style.fontSize = tam;
        l.style.color = Hex(color);
        l.style.whiteSpace = WhiteSpace.Normal;
        l.style.unityTextAlign = TextAnchor.MiddleCenter;
        l.style.marginTop = 12; l.style.marginBottom = 12;
        if (negrita) l.style.unityFontStyleAndWeight = FontStyle.Bold;
        return l;
    }

    Image Imagen(float lado, float opacidad)
    {
        var i = new Image { image = imagenQuibar, scaleMode = ScaleMode.ScaleToFit };
        i.style.width = lado; i.style.height = lado;
        i.style.opacity = opacidad;
        return i;
    }

    Button Boton(string texto, Action clic, float fuente = 44)
    {
        var b = new Button(clic) { text = texto };
        b.style.marginTop = 30;
        b.style.height = 120;
        b.style.alignSelf = Align.Stretch;
        b.style.backgroundColor = Hex("#0A5CAD");
        b.style.color = Color.white;
        b.style.fontSize = fuente;
        b.style.unityFontStyleAndWeight = FontStyle.Bold;
        Radio(b, 60);
        Borde(b, 0, Color.clear);
        return b;
    }

    Button BotonSecundario(string texto, Action clic)
    {
        var b = Boton(texto, clic, 38);
        b.style.backgroundColor = Color.white;
        b.style.color = Hex("#0A5CAD");
        Borde(b, 4, Hex("#0A5CAD"));
        return b;
    }

    Label Etiqueta(string texto)
    {
        var l = Texto(texto, 40, "#0B2540", true);
        l.pickingMode = PickingMode.Ignore;
        l.style.backgroundColor = Color.white;
        Radio(l, 40);
        l.style.paddingTop = 14; l.style.paddingBottom = 14;
        l.style.paddingLeft = 36; l.style.paddingRight = 36;
        return l;
    }

    VisualElement CrearCandado()
    {
        var cont = new VisualElement();
        cont.pickingMode = PickingMode.Ignore;
        cont.style.position = Position.Absolute;
        cont.style.left = Length.Percent(54);
        cont.style.top = 18;
        cont.style.width = 36; cont.style.height = 42;
        cont.style.alignItems = Align.Center;

        var arco = new VisualElement();                                       // la argolla del candado
        arco.pickingMode = PickingMode.Ignore;
        arco.style.width = 20; arco.style.height = 16;
        arco.style.borderTopLeftRadius = 10; arco.style.borderTopRightRadius = 10;
        arco.style.borderTopWidth = 4; arco.style.borderLeftWidth = 4; arco.style.borderRightWidth = 4;
        arco.style.borderTopColor = Hex("#0B2540"); arco.style.borderLeftColor = Hex("#0B2540"); arco.style.borderRightColor = Hex("#0B2540");

        var cuerpo = new VisualElement();                                     // el cuerpo del candado
        cuerpo.pickingMode = PickingMode.Ignore;
        cuerpo.style.width = 36; cuerpo.style.height = 26;
        cuerpo.style.backgroundColor = Hex("#0B2540");
        Radio(cuerpo, 6);

        cont.Add(arco); cont.Add(cuerpo);
        return cont;
    }

    static void Radio(VisualElement e, float r)
    {
        e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r;
        e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r;
    }

    static void Borde(VisualElement e, float ancho, Color color)
    {
        e.style.borderTopWidth = ancho; e.style.borderBottomWidth = ancho;
        e.style.borderLeftWidth = ancho; e.style.borderRightWidth = ancho;
        e.style.borderTopColor = color; e.style.borderBottomColor = color;
        e.style.borderLeftColor = color; e.style.borderRightColor = color;
    }

    static Color Hex(string h) { Color c; ColorUtility.TryParseHtmlString(h, out c); return c; }
}
