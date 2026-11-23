using UnityEngine;
using UnityEngine.UIElements;

// Va en el objeto "Interfaz" (el que tiene el UIDocument).
// Muestra la ficha de la estación escaneada, reproduce el audio y cierra todo con "Continuar recorrido".
[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(AudioSource))]
public class DetalleEstacionController : MonoBehaviour
{
    VisualElement pantallaDetalle, pantallaEscaner, pantallaPasaporte, pantallaEncuesta, filaAudio;
    VisualElement iconoFlechaDetalle;
    Label txtAviso, txtTitulo, txtSubtitulo, txtDescripcion, txtTiempo;
    Button btnAudio, btnContinuar, btnToggleDetalle;

    AudioSource audioSource;
    GameObject modeloActual;
    string codigoEstacionActual;
    bool pausado;

    public GameObject ModeloActual { get { return modeloActual; } }   // NUEVO: lo usa QuibarController

    void OnEnable()
    {
        var raiz = GetComponent<UIDocument>().rootVisualElement;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        pantallaDetalle   = raiz.Q<VisualElement>("pantalla-detalle");
        pantallaEscaner   = raiz.Q<VisualElement>("pantalla-escaner");
        pantallaPasaporte = raiz.Q<VisualElement>("pantalla-pasaporte");
        pantallaEncuesta  = raiz.Q<VisualElement>("pantalla-encuesta");
        filaAudio         = raiz.Q<VisualElement>("audio-fila");

        txtAviso       = raiz.Q<Label>("TXT_Aviso");
        txtTitulo      = raiz.Q<Label>("TXT_Titulo");
        txtSubtitulo   = raiz.Q<Label>("TXT_Subtitulo");
        txtDescripcion = raiz.Q<Label>("TXT_Descripcion");
        txtTiempo      = raiz.Q<Label>("TXT_AudioTiempo");

        btnAudio     = raiz.Q<Button>("btn-audio");
        btnContinuar = raiz.Q<Button>("btn-continuar");
        btnToggleDetalle = raiz.Q<Button>("btn-toggle-detalle");
        iconoFlechaDetalle = raiz.Q<VisualElement>("icono-flecha-detalle");

        pantallaDetalle.style.display = DisplayStyle.None;
        btnAudio.clicked     += AlternarAudio;
        btnContinuar.clicked += Cerrar;
        btnToggleDetalle.clicked += AlternarFicha;

        raiz.schedule.Execute(ActualizarTiempo).Every(250);
    }

    // Lo llama GestionSQLite cuando el QR es válido
    public void Mostrar(string titulo, string subtitulo, string ficha, string rutaAudio, GameObject modelo, string codigoQR)
    {
        DestruirModelo();
        modeloActual = modelo;
        codigoEstacionActual = codigoQR;

        // 1. PRIMERO SIEMPRE: Colocar los textos y mostrar la ficha informativa de inmediato
        if (string.IsNullOrWhiteSpace(ficha))
        {
            if (codigoQR == "QR_ENERGIA_05" || (titulo != null && titulo.Contains("5")))
            {
                ficha = "El agua de un río no solo mueve turbinas: también sostiene la vida a su alrededor. Un río limpio y con caudal estable alimenta a peces, aves acuáticas y mamíferos como el zorro, que baja a beber a sus orillas.\n\nLa energía hidroeléctrica aprovecha el flujo del agua para generar electricidad sin quemar combustibles, por eso es una fuente renovable. Pero su desarrollo debe cuidar el río: mantener un caudal ecológico y permitir que la fauna se siga moviendo y reproduciendo.\n\nEsta estación muestra ese equilibrio: producir energía y proteger la vida que depende del río.";
            }
            else if (codigoQR == "QR_BIOBIO_04" || (titulo != null && titulo.Contains("4")))
            {
                ficha = "La cuenca del Biobío es hogar de peces nativos, varios exclusivos de Chile y de tamaño muy pequeño. Muchos están amenazados.\n\nPeces nativos:\n• Bagrecito (Bullockia maldonadoi): 7 cm. En peligro de extinción.\n• Tollo de agua dulce (Diplomystes nahuelbutaensis): 7 cm. En peligro de extinción.\n• Carmelita de Concepción (Percilia irwini): 7 cm. En peligro de extinción.\n• Bagre chico (Trichomycterus areolatus): 15 cm. Vulnerable.\n• Pejerrey chileno (Basilichthys australis): 30 cm. Casi amenazado.\n• Perca trucha (Percichthys trucha): 35 cm. Preocupación menor.\n• Puye (Galaxias maculatus): 5 cm. Preocupación menor.\n\nPeces introducidos:\n• Trucha arcoíris (Oncorhynchus mykiss).\n• Trucha fario (Salmo trutta fario).";
            }
        }

        Debug.Log($"[DETALLE] Mostrar - Titulo: '{titulo}', Ficha length: {ficha?.Length ?? 0}");

        if (txtTitulo != null) txtTitulo.text = titulo ?? "";
        if (txtSubtitulo != null) txtSubtitulo.text = subtitulo ?? "";
        if (txtDescripcion != null) txtDescripcion.text = ficha ?? "";

        if (pantallaDetalle != null)
        {
            pantallaDetalle.RemoveFromClassList("detalle--colapsado");
            pantallaDetalle.style.display = DisplayStyle.Flex;
        }
        if (iconoFlechaDetalle != null) iconoFlechaDetalle.RemoveFromClassList("detalle__flecha--arriba");

        // Ocultar visor de cámara y pasaporte para que se vea la ficha
        if (pantallaPasaporte != null) pantallaPasaporte.style.display = DisplayStyle.None;
        if (pantallaEscaner != null) pantallaEscaner.style.display = DisplayStyle.None;

        // Mensaje de aviso "estación escaneada"
        if (txtAviso != null)
        {
            txtAviso.style.display = DisplayStyle.Flex;
            txtAviso.schedule.Execute(() => txtAviso.style.display = DisplayStyle.None).StartingIn(3000);
        }

        // Marcar de inmediato la estación como descubierta en el Pasaporte
        if (PasaporteController.Instancia != null)
        {
            PasaporteController.Instancia.ConfirmarEstacionDescubierta(codigoQR);
        }

        // 2. Audio protegido: si falla o no existe, JAMÁS bloquea la información
        try
        {
            if (audioSource != null)
            {
                audioSource.Stop();
                pausado = false;
                audioSource.clip = string.IsNullOrEmpty(rutaAudio) ? null : Resources.Load<AudioClip>(rutaAudio);
                if (!string.IsNullOrEmpty(rutaAudio) && audioSource.clip == null)
                    Debug.LogWarning("No se encontró el audio en Resources: " + rutaAudio);

                if (filaAudio != null)
                {
                    filaAudio.style.display = audioSource.clip != null ? DisplayStyle.Flex : DisplayStyle.None;
                }
                if (btnAudio != null) btnAudio.text = "Escuchar";
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[DETALLE] Error cargando audio: " + ex.Message);
            if (filaAudio != null) filaAudio.style.display = DisplayStyle.None;
        }

        // 3. Quibar protegido: jamás interrumpe la visualización de la ficha
        try
        {
            var quibar = GetComponent<QuibarController>();
            if (quibar != null)
            {
                var m = System.Text.RegularExpressions.Regex.Match(titulo ?? "", @"Estaci[oó]n\s+(\d+)");
                if (m.Success) quibar.AlMostrarEstacion(int.Parse(m.Groups[1].Value));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[DETALLE] Error en Quibar: " + ex.Message);
        }
    }

    void AlternarAudio()
    {
        if (audioSource.clip == null) return;
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
            pausado = true;
            btnAudio.text = "Escuchar";
        }
        else
        {
            if (pausado) audioSource.UnPause(); else audioSource.Play();
            pausado = false;
            btnAudio.text = "Pausar";
        }
    }

    void AlternarFicha()
    {
        Debug.Log("[DETALLE] AlternarFicha pulsado");
        bool estabaColapsada = pantallaDetalle.ClassListContains("detalle--colapsado");
        if (estabaColapsada)
        {
            pantallaDetalle.RemoveFromClassList("detalle--colapsado");
            iconoFlechaDetalle.RemoveFromClassList("detalle__flecha--arriba");
        }
        else
        {
            pantallaDetalle.AddToClassList("detalle--colapsado");
            iconoFlechaDetalle.AddToClassList("detalle__flecha--arriba");
        }
    }

    void ActualizarTiempo()
    {
        try
        {
            if (audioSource == null || audioSource.clip == null || txtTiempo == null) return;
            txtTiempo.text = Formato(audioSource.time) + " / " + Formato(audioSource.clip.length);
            if (!audioSource.isPlaying && !pausado && btnAudio != null) btnAudio.text = "Escuchar";   // el audio terminó
        }
        catch
        {
            // Protegido contra estados intermedios del clip de audio en móviles
        }
    }

    static string Formato(float segundos)
    {
        int s = Mathf.FloorToInt(segundos);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    // Lo llama GestionSQLite cuando el ancla en el piso queda lista y con el modelo cargado
    public void AsignarModeloActual(GameObject modelo)
    {
        if (modeloActual != null && modeloActual != modelo)
        {
            Destroy(modeloActual);
        }
        modeloActual = modelo;
    }

    public void Cerrar()
    {
        Debug.Log("[DETALLE] Cerrar pulsado. codigoEstacionActual: " + codigoEstacionActual);
        try
        {
            if (audioSource != null) audioSource.Stop();
            pausado = false;
            DestruirModelo();
            if (GestionSQLite.Instancia != null) GestionSQLite.Instancia.LimpiarModeloColocado();
            if (PasaporteController.Instancia != null)
                PasaporteController.Instancia.ConfirmarEstacionDescubierta(codigoEstacionActual);
            if (ControladorCamaraAR.Instancia != null) ControladorCamaraAR.Instancia.DesactivarCamaraAR();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[DETALLE] Excepción durante Cerrar: " + ex);
        }
        finally
        {
            if (pantallaDetalle != null) pantallaDetalle.style.display = DisplayStyle.None;

            // Si ya descubrió las 5 estaciones y aún no ha respondido la encuesta, invitarlo abriendo la encuesta
            bool todasCompletadas = true;
            for (int i = 1; i <= 5; i++)
            {
                if (PlayerPrefs.GetInt("estacion_" + i, 0) == 0)
                {
                    todasCompletadas = false;
                    break;
                }
            }

            if (todasCompletadas && PlayerPrefs.GetInt("encuesta_enviada", 0) == 0 && pantallaEncuesta != null)
            {
                if (pantallaPasaporte != null) pantallaPasaporte.style.display = DisplayStyle.None;
                pantallaEncuesta.style.display = DisplayStyle.Flex;
            }
            else
            {
                if (pantallaPasaporte != null) pantallaPasaporte.style.display = DisplayStyle.Flex;
            }

            codigoEstacionActual = null;
        }
    }

    void DestruirModelo()
    {
        if (modeloActual != null) Destroy(modeloActual);
        modeloActual = null;
    }
}
