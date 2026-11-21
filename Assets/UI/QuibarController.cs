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
    [Header("Quibar escondido (quibar_new): sale junto al modelo AR y en la pista del candado")]
    public Texture2D imagenQuibar;

    [Header("Quibar con el trofeo (Zorro_y_cuadrado_celebran_juntos): pantalla del trofeo")]
    public Texture2D imagenTrofeo;

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
    SpriteRenderer srAR;
    float altoAR, inicioEspera;
    bool teniaModelo, colocado, atrapado;
    Vector3 posLocalBase;
    const float EsperaModelo = 2.5f;      // segundos esperando que Vuforia coloque el modelo en el piso

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
        srAR = quibarAR.AddComponent<SpriteRenderer>();
        srAR.sprite = spriteAR;
        inicioEspera = Time.time;
        quibarAR.transform.localScale = Vector3.one * altoAR;
        colocado = false; atrapado = false;

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

        atrapado = true;
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

    // ---------- Quibar en AR: posición fija y botones ----------
    void LateUpdate()
    {
        if (quibarAR == null) return;

        // Se limpia solo si el modelo se destruyó o se cerró la ficha
        bool fichaCerrada = pantallaDetalle != null && pantallaDetalle.style.display.value == DisplayStyle.None;
        if ((teniaModelo && modeloAnclaje == null) || fichaCerrada || cam == null) { LimpiarAR(); return; }

        // El modelo se coloca en el piso un poco después de mostrar la ficha: cuando aparezca, Quibar queda fijo a su lado
        if (!teniaModelo && detalle != null && detalle.ModeloActual != null) AdoptarModelo(detalle.ModeloActual);

        // Mientras se espera el modelo, Quibar no se muestra (si tarda mucho, queda fijo frente a la cámara)
        bool esperando = !teniaModelo && Time.time - inicioEspera < EsperaModelo;
        bool visible = !esperando && (!teniaModelo || ModeloVisible());
        if (srAR != null) srAR.enabled = visible;

        if (!esperando && !colocado) ColocarSprite();                          // se calcula UNA sola vez

        if (colocado)
        {
            // La posición no cambia (va pegado al ancla del piso, igual que el modelo). Solo gira para mirar a la cámara.
            var tr = quibarAR.transform;
            tr.localPosition = posLocalBase + Vector3.up * (Mathf.Sin(Time.time * 2f) * altoAR * 0.03f);   // flotadito suave
            Vector3 adelante = cam.transform.forward; adelante.y = 0f;
            if (adelante.sqrMagnitude > 0.001f) tr.rotation = Quaternion.LookRotation(adelante.normalized, Vector3.up);
        }

        PosicionarPanelAR(visible && colocado);
    }

    // Calcula el lugar de Quibar una sola vez: al costado del modelo (o frente a la cámara si no hay modelo)
    void ColocarSprite()
    {
        Vector3 derecha = cam.transform.right; derecha.y = 0f;
        derecha = derecha.sqrMagnitude < 0.001f ? Vector3.right : derecha.normalized;
        Vector3 adelante = cam.transform.forward; adelante.y = 0f;
        adelante = adelante.sqrMagnitude < 0.001f ? Vector3.forward : adelante.normalized;

        Vector3 pos;
        Bounds b;
        if (teniaModelo && TieneBounds(out b))
        {
            float radio = Mathf.Max(b.extents.x, b.extents.z);
            pos = b.center + derecha * (radio + separacion + altoAR * 0.4f);
            pos.y = b.min.y + altoAR * 0.5f;
        }
        else if (teniaModelo)
        {
            pos = modeloAnclaje.transform.position + derecha * (0.3f + separacion);
            pos.y += altoAR * 0.5f;
        }
        else
        {
            pos = cam.transform.position + adelante * 1.5f + derecha * 0.4f;
            pos.y = cam.transform.position.y - 0.4f;
        }

        var tr = quibarAR.transform;
        tr.SetParent(null, true);
        tr.position = pos;
        if (teniaModelo) tr.SetParent(modeloAnclaje.transform, true);          // pegado al ancla: se mueve con el mundo, no con la cámara
        posLocalBase = tr.localPosition;
        colocado = true;
    }

    void PosicionarPanelAR(bool spriteVisible)
    {
        if (panelAR == null) return;

        float W = raiz.resolvedStyle.width, H = raiz.resolvedStyle.height;
        if (float.IsNaN(W) || float.IsNaN(H) || W <= 0f || H <= 0f) return;

        Vector3 cabeza = quibarAR.transform.position + Vector3.up * (altoAR * 0.55f);
        Vector3 vp = cam.WorldToViewportPoint(cabeza);
        bool enPantalla = spriteVisible && vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;

        if (!enPantalla && !atrapado)
        {
            panelAR.style.display = DisplayStyle.None;                         // hay que mover la cámara para encontrarlo
            return;
        }
        panelAR.style.display = DisplayStyle.Flex;

        if (enPantalla)
        {
            Vector2 p = RuntimePanelUtils.CameraTransformWorldToPanel(raiz.panel, cabeza, cam);
            panelAR.style.left   = Mathf.Clamp(p.x - AnchoPanelAR * 0.5f, 20f, Mathf.Max(20f, W - AnchoPanelAR - 20f));
            panelAR.style.bottom = Mathf.Clamp(H - p.y + 20f, 20f, Mathf.Max(20f, H - 420f));
        }
        else
        {
            // Ya lo atrapó pero Quibar salió de cuadro: los botones se quedan fijos en pantalla para poder continuar
            panelAR.style.left   = (W - AnchoPanelAR) * 0.5f;
            panelAR.style.bottom = H * 0.5f;
        }
    }

    void AdoptarModelo(GameObject modelo)
    {
        modeloAnclaje = modelo;
        teniaModelo = true;
        renderersModelo = modelo.GetComponentsInChildren<Renderer>();
        Bounds b;
        if (alturaQuibar <= 0f && TieneBounds(out b))
        {
            altoAR = Mathf.Clamp(b.size.y * 0.6f, 0.15f, 0.6f);
            quibarAR.transform.SetParent(null, true);
            quibarAR.transform.localScale = Vector3.one * altoAR;
        }
        colocado = false;                                                      // se recoloca junto al modelo
    }

    // Si Vuforia pierde el seguimiento oculta el modelo; Quibar se oculta con él
    bool ModeloVisible()
    {
        if (renderersModelo == null) return true;
        foreach (var r in renderersModelo)
            if (r != null && r.enabled) return true;
        return false;
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
        modeloAnclaje = null; renderersModelo = null; teniaModelo = false; srAR = null;
        colocado = false; atrapado = false;
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

        var marco = new VisualElement();                                      // tarjeta con borde dorado tipo trofeo
        marco.style.width = Length.Percent(100);
        marco.style.height = 640;
        Radio(marco, 70);
        Borde(marco, 14, Hex("#F5B301"));
        marco.style.backgroundColor = Hex("#FFF7E0");
        marco.style.overflow = Overflow.Hidden;
        marco.style.alignItems = Align.Center;
        marco.style.justifyContent = Justify.Center;
        var fotoTrofeo = new Image { image = imagenTrofeo != null ? imagenTrofeo : imagenQuibar, scaleMode = ScaleMode.ScaleToFit };
        fotoTrofeo.style.width = Length.Percent(94);
        fotoTrofeo.style.height = Length.Percent(94);
        marco.Add(fotoTrofeo);
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
