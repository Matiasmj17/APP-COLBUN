using UnityEngine;
using UnityEngine.UIElements;

// Va en el objeto "Interfaz" (el que tiene el UIDocument).
// Muestra la ficha de la estación escaneada, reproduce el audio y cierra todo con "Continuar recorrido".
[RequireComponent(typeof(UIDocument))]
[RequireComponent(typeof(AudioSource))]
public class DetalleEstacionController : MonoBehaviour
{
    VisualElement pantallaDetalle, pantallaEscaner, pantallaPasaporte, filaAudio;
    Label txtAviso, txtTitulo, txtSubtitulo, txtDescripcion, txtTiempo;
    Button btnAudio, btnContinuar;

    AudioSource audioSource;
    GameObject modeloActual;
    bool pausado;

    void OnEnable()
    {
        var raiz = GetComponent<UIDocument>().rootVisualElement;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        pantallaDetalle   = raiz.Q<VisualElement>("pantalla-detalle");
        pantallaEscaner   = raiz.Q<VisualElement>("pantalla-escaner");
        pantallaPasaporte = raiz.Q<VisualElement>("pantalla-pasaporte");
        filaAudio         = raiz.Q<VisualElement>("audio-fila");

        txtAviso       = raiz.Q<Label>("TXT_Aviso");
        txtTitulo      = raiz.Q<Label>("TXT_Titulo");
        txtSubtitulo   = raiz.Q<Label>("TXT_Subtitulo");
        txtDescripcion = raiz.Q<Label>("TXT_Descripcion");
        txtTiempo      = raiz.Q<Label>("TXT_AudioTiempo");

        btnAudio     = raiz.Q<Button>("btn-audio");
        btnContinuar = raiz.Q<Button>("btn-continuar");

        pantallaDetalle.style.display = DisplayStyle.None;
        btnAudio.clicked     += AlternarAudio;
        btnContinuar.clicked += Cerrar;

        raiz.schedule.Execute(ActualizarTiempo).Every(250);
    }

    // Lo llama GestionSQLite cuando el QR es válido
    public void Mostrar(string titulo, string subtitulo, string ficha, string rutaAudio, GameObject modelo)
    {
        DestruirModelo();
        modeloActual = modelo;

        txtTitulo.text      = titulo;
        txtSubtitulo.text   = subtitulo;
        txtDescripcion.text = ficha;

        // Audio (si la ruta no existe, simplemente se oculta la fila del audio)
        audioSource.Stop();
        pausado = false;
        audioSource.clip = string.IsNullOrEmpty(rutaAudio) ? null : Resources.Load<AudioClip>(rutaAudio);
        if (!string.IsNullOrEmpty(rutaAudio) && audioSource.clip == null)
            Debug.LogWarning("No se encontró el audio en Resources: " + rutaAudio);
        filaAudio.style.display = audioSource.clip != null ? DisplayStyle.Flex : DisplayStyle.None;
        btnAudio.text = "Escuchar";

        // Cambio de pantalla
        pantallaEscaner.style.display = DisplayStyle.None;
        pantallaDetalle.style.display = DisplayStyle.Flex;

        // Mensaje de "estación escaneada" que se esconde a los 3 segundos
        txtAviso.style.display = DisplayStyle.Flex;
        txtAviso.schedule.Execute(() => txtAviso.style.display = DisplayStyle.None).StartingIn(3000);
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

    void ActualizarTiempo()
    {
        if (audioSource == null || audioSource.clip == null) return;
        txtTiempo.text = Formato(audioSource.time) + " / " + Formato(audioSource.clip.length);
        if (!audioSource.isPlaying && !pausado) btnAudio.text = "Escuchar";   // el audio terminó
    }

    static string Formato(float segundos)
    {
        int s = Mathf.FloorToInt(segundos);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    void Cerrar()
    {
        audioSource.Stop();
        pausado = false;
        DestruirModelo();
        pantallaDetalle.style.display   = DisplayStyle.None;
        pantallaPasaporte.style.display = DisplayStyle.Flex;
    }

    void DestruirModelo()
    {
        if (modeloActual != null) Destroy(modeloActual);
        modeloActual = null;
    }
}
