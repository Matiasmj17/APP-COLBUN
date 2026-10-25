using System.Collections.Generic;            // Para usar List<>
using UnityEngine;                           // Clases base de Unity (PlayerPrefs, Mathf, etc.)
using UnityEngine.UIElements;                // UI Toolkit (UIDocument, Label, Button, VisualElement)

// Va en el mismo objeto que el componente UIDocument
[RequireComponent(typeof(UIDocument))]       // Obliga a que exista un UIDocument en este objeto
public class PasaporteController : MonoBehaviour
{
    enum Estado { Descubierta, Pendiente, Bloqueada }   // Los 3 estados del mockup

    class Estacion                                       // Datos de una estación
    {
        public int numero;                               // 1..5
        public string titulo;                            // Ej: "Flora Nativa"
        public string descripcion;                       // Texto corto bajo el título
        public string codigoQR;                          // Texto que debe contener el QR de esa estación
        public Estado estado;                            // Se calcula según el progreso guardado
    }

    [Tooltip("Arrastra aquí el AR Camera (el objeto que tiene QRScanner)")]
    [SerializeField] QRScanner qrScanner;                // Lector de QR (opcional: sin él, solo funciona el botón de prueba)

    // Catálogo provisional (después vendrá de la tabla ESTACION en SQLite).
    // Los estados NO se escriben aquí: se calculan con el progreso guardado.
    readonly List<Estacion> estaciones = new List<Estacion>
    {
        new Estacion { numero = 1, titulo = "Agua y Energía",        descripcion = "Transformación cinética y aprovechamiento hídrico", codigoQR = "ANGOSTURA-EST1" },
        new Estacion { numero = 2, titulo = "Flora Nativa",          descripcion = "Quillay, Peumo y Espino",                          codigoQR = "ANGOSTURA-EST2" },
        new Estacion { numero = 3, titulo = "Fauna del Maule",       descripcion = "Aproximándote al punto de observación",            codigoQR = "ANGOSTURA-EST3" },
        new Estacion { numero = 4, titulo = "Aves de Ribera",        descripcion = "Humedales y biodiversidad ribereña",               codigoQR = "ANGOSTURA-EST4" },
        new Estacion { numero = 5, titulo = "Historia & Patrimonio", descripcion = "Memorias y desarrollo del Maule",                  codigoQR = "ANGOSTURA-EST5" },
    };

    VisualElement raiz;                      // Raíz del árbol UXML (la guardamos para refrescar la UI)
    VisualElement pantallaPasaporte;         // Contenedor de la pantalla 1
    VisualElement pantallaEscaner;           // Contenedor de la pantalla 2 (cámara)
    Label lblMensaje;                        // Aviso sobre la cámara ("Código QR escaneado...")
    IVisualElementScheduledItem tareaOcultar;// Temporizador que oculta el aviso

    void OnEnable()                          // Se ejecuta cuando el objeto se activa (el UI ya existe)
    {
        raiz = GetComponent<UIDocument>().rootVisualElement;               // Raíz del árbol UXML

        pantallaPasaporte = raiz.Q<VisualElement>("pantalla-pasaporte");   // Busca por el atributo name del UXML
        pantallaEscaner   = raiz.Q<VisualElement>("pantalla-escaner");
        lblMensaje        = raiz.Q<Label>("lbl-mensaje");

        raiz.Q<Button>("btn-escanear").clicked   += IrAEscaner;            // Botón flotante "Escanear QR"
        raiz.Q<Button>("btn-nav-camara").clicked += IrAEscaner;            // Botón "Cámara AR" de la barra inferior
        raiz.Q<Button>("btn-volver").clicked     += IrAPasaporte;          // Botón "Volver" del visor
        raiz.Q<Button>("btn-simular").clicked    += SimularEscaneo;        // Botón de prueba (sin cámara)

        if (qrScanner != null) qrScanner.OnQRDetected += ProcesarQR;       // Nos avisan cuando se lee un QR

        RefrescarTodo();                     // Calcula estados y dibuja tarjetas
        IrAPasaporte();                      // Pantalla inicial
    }

    void OnDisable()                         // Al desactivarse el objeto
    {
        if (qrScanner != null) qrScanner.OnQRDetected -= ProcesarQR;       // Nos desuscribimos
    }

    // ---------- Navegación ----------

    void IrAPasaporte()
    {
        pantallaPasaporte.style.display = DisplayStyle.Flex;   // Muestra el pasaporte
        pantallaEscaner.style.display   = DisplayStyle.None;   // Oculta el visor
        if (qrScanner != null) qrScanner.escaneando = false;   // Apaga el escaneo (ahorra batería)
    }

    void IrAEscaner()
    {
        pantallaPasaporte.style.display = DisplayStyle.None;   // Oculta el pasaporte (si no, tapa la cámara)
        pantallaEscaner.style.display   = DisplayStyle.Flex;   // Muestra el visor transparente
        if (qrScanner != null) qrScanner.escaneando = true;    // Enciende el escaneo
    }

    // ---------- Lógica del QR ----------

    void ProcesarQR(string texto)
    {
        texto = (texto ?? "").Trim();                          // Limpia espacios; evita null
        if (texto.Length == 0 || texto.Length > 100)           // Validación básica de entrada
        {
            MostrarMensaje("QR no válido", true);
            return;
        }

        var estacion = estaciones.Find(x => x.codigoQR == texto);   // ¿Es una estación registrada?
        if (estacion == null)
        {
            MostrarMensaje("QR no válido: no pertenece al sendero", true);   // Nada cambia en el pasaporte
            return;
        }

        if (estacion.estado == Estado.Descubierta)             // Ya estaba hecha
        {
            MostrarMensaje("Ya descubriste: " + estacion.titulo, true);
            return;
        }

        if (estacion.estado == Estado.Bloqueada)               // Aún no le toca (hay que ir en orden)
        {
            MostrarMensaje("Estación bloqueada. Descubre primero las anteriores", true);
            return;
        }

        // Llegó aquí: es la estación pendiente y el QR es válido -> se marca como descubierta
        PlayerPrefs.SetInt("estacion_" + estacion.numero, 1);  // Guarda el progreso en el celular
        PlayerPrefs.Save();                                    // Fuerza el guardado a disco

        RefrescarTodo();                                       // Recalcula: la siguiente pasa a "Pendiente"
        MostrarMensaje("Código QR escaneado: " + estacion.titulo + " descubierta", false);

        raiz.schedule.Execute(IrAPasaporte).StartingIn(2000);  // A los 2 s vuelve al pasaporte para ver el cambio
    }

    void SimularEscaneo()                                      // Botón "Ingresar Código" (solo para pruebas)
    {
        var pendiente = estaciones.Find(x => x.estado == Estado.Pendiente);   // Estación que toca ahora
        ProcesarQR(pendiente != null ? pendiente.codigoQR : "ANGOSTURA-EST1"); // Simula leer su QR
    }

    [ContextMenu("Reiniciar progreso")]                        // Clic derecho sobre el componente en el Inspector
    void ReiniciarProgreso()
    {
        foreach (var e in estaciones) PlayerPrefs.DeleteKey("estacion_" + e.numero);  // Borra lo guardado
        PlayerPrefs.Save();
        if (raiz != null) RefrescarTodo();                     // Redibuja si ya está en ejecución
    }

    // ---------- Estados y dibujo ----------

    void CalcularEstados()
    {
        bool pendienteAsignada = false;                        // Solo UNA estación puede estar pendiente
        foreach (var e in estaciones)                          // Se recorre en orden 1..5
        {
            if (PlayerPrefs.GetInt("estacion_" + e.numero, 0) == 1)
                e.estado = Estado.Descubierta;                 // Guardada como hecha
            else if (!pendienteAsignada)
            {
                e.estado = Estado.Pendiente;                   // Primera no hecha = la que toca
                pendienteAsignada = true;
            }
            else
                e.estado = Estado.Bloqueada;                   // El resto sigue bloqueado
        }
    }

    void RefrescarTodo()
    {
        CalcularEstados();                                     // Estados según progreso guardado
        ConstruirEstaciones(raiz.Q<VisualElement>("estaciones"));   // Redibuja las tarjetas
        ActualizarProgreso();                                  // Redibuja progreso y "Próxima"
    }

    void MostrarMensaje(string texto, bool esError)
    {
        lblMensaje.text = texto;                               // Escribe el mensaje
        lblMensaje.EnableInClassList("escaner__mensaje--error", esError);   // Rojo si es error, verde si no
        lblMensaje.style.display = DisplayStyle.Flex;          // Lo muestra
        tareaOcultar?.Pause();                                 // Cancela el temporizador anterior (si había)
        tareaOcultar = lblMensaje.schedule.Execute(() => lblMensaje.style.display = DisplayStyle.None).StartingIn(2500);
    }

    void ConstruirEstaciones(VisualElement contenedor)
    {
        contenedor.Clear();                                    // Borra las tarjetas anteriores
        foreach (var e in estaciones)                          // Una tarjeta por estación
            contenedor.Add(CrearTarjeta(e));
    }

    VisualElement CrearTarjeta(Estacion e)
    {
        var tarjeta = new VisualElement();
        tarjeta.AddToClassList("estacion");
        if (e.estado == Estado.Pendiente) tarjeta.AddToClassList("estacion--pendiente");
        if (e.estado == Estado.Bloqueada) tarjeta.AddToClassList("estacion--bloqueada");

        var fila = new VisualElement();
        fila.AddToClassList("estacion__fila");

        // El círculo ahora es un contenedor vacío, no un Label de texto
        var circulo = new VisualElement();
        circulo.AddToClassList("estacion__circulo");

        // Inyectamos el ícono según el estado
        if (e.estado == Estado.Descubierta)
        {
            var icono = new VisualElement();
            icono.AddToClassList("icono-check");
            circulo.Add(icono);
        }
        else if (e.estado == Estado.Bloqueada)
        {
            var icono = new VisualElement();
            icono.AddToClassList("icono-candado");
            circulo.Add(icono);
        }
        else // Pendiente
        {
            var numero = new Label(e.numero.ToString());
            numero.style.fontSize = 56;
            numero.style.unityFontStyleAndWeight = FontStyle.Bold;
            circulo.Add(numero);
        }

        var textos = new VisualElement();
        textos.AddToClassList("estacion__textos");

        var etiqueta = new Label("Estación " + e.numero);
        etiqueta.AddToClassList("estacion__etiqueta");
        var titulo = new Label(e.titulo);
        titulo.AddToClassList("estacion__titulo");
        var descripcion = new Label(e.descripcion);
        descripcion.AddToClassList("estacion__descripcion");

        textos.Add(etiqueta);
        textos.Add(titulo);
        textos.Add(descripcion);

        string textoEstado = e.estado == Estado.Descubierta ? "Descubierto"
                           : e.estado == Estado.Pendiente   ? "Pendiente"
                           : "Bloqueada";
        var pildora = new Label(textoEstado);
        pildora.AddToClassList("estacion__pildora");

        fila.Add(circulo);
        fila.Add(textos);
        fila.Add(pildora);
        tarjeta.Add(fila);

        if (e.estado == Estado.Pendiente)
        {
            var pista = new Label("Apunta la cámara al tótem en el sendero");
            pista.AddToClassList("estacion__pista");
            tarjeta.Add(pista);
        }

        return tarjeta;
    }

    void ActualizarProgreso()
    {
        int hechas = estaciones.FindAll(x => x.estado == Estado.Descubierta).Count; // Cuántas están descubiertas
        float porcentaje = hechas * 100f / estaciones.Count;                         // Ej: 2*100/5 = 40

        raiz.Q<Label>("lbl-progreso").text   = hechas + " de " + estaciones.Count + " estaciones";
        raiz.Q<Label>("lbl-porcentaje").text = Mathf.RoundToInt(porcentaje) + "% completado";
        raiz.Q<VisualElement>("barra-relleno").style.width = Length.Percent(porcentaje); // Ancho de la barra azul

        var proxima = estaciones.Find(x => x.estado == Estado.Pendiente);            // Primera pendiente
        raiz.Q<Label>("lbl-proxima").text = proxima != null ? "Próxima: " + proxima.titulo : "¡Sendero completado!";
    }
}
