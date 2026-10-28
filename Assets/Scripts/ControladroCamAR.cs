using UnityEngine;
using Vuforia;

/// <summary>
/// Controla cuándo Vuforia usa la cámara. La cámara permanece activa durante
/// el visor y la ficha AR, y se pausa en las pantallas normales de la app.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class ControladorCamaraAR : MonoBehaviour
{
    public static ControladorCamaraAR Instancia { get; private set; }

    VuforiaBehaviour vuforia;
    public bool EscaneoQRActivo { get; private set; }

    void Awake()
    {
        Instancia = this;
        ConfigurarFrecuenciaObjetivo();
        vuforia = FindAnyObjectByType<VuforiaBehaviour>();
        PausarVuforia();
    }

    void OnApplicationFocus(bool tieneFoco)
    {
        // Android puede cambiar la frecuencia disponible al volver a la app
        // (por ejemplo, al salir del ahorro de batería).
        if (tieneFoco) ConfigurarFrecuenciaObjetivo();
    }

    void ConfigurarFrecuenciaObjetivo()
    {
        float hzPantalla = (float)Screen.currentResolution.refreshRateRatio.value;
        int objetivo = hzPantalla > 0f ? Mathf.RoundToInt(hzPantalla) : 60;

        // Mantiene un límite razonable si otro dispositivo informa más de 120 Hz.
        Application.targetFrameRate = Mathf.Clamp(objetivo, 30, 120);
        Debug.Log("Objetivo de FPS: " + Application.targetFrameRate +
                  " (frecuencia detectada: " + hzPantalla.ToString("F1") + " Hz)");
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    // Se llama al entrar en la pantalla del visor.
    public void ActivarCamaraAR()
    {
        if (vuforia == null) vuforia = FindAnyObjectByType<VuforiaBehaviour>();
        if (vuforia != null) vuforia.enabled = true;
        EscaneoQRActivo = true;
        Debug.Log("Vuforia activa: buscando un código QR.");
    }

    // Deja de buscar códigos, pero conserva la cámara para mostrar el modelo AR.
    public void DetenerLecturaQR()
    {
        EscaneoQRActivo = false;
    }

    // Se llama al volver a una pantalla sin AR.
    public void DesactivarCamaraAR()
    {
        EscaneoQRActivo = false;
        PausarVuforia();
    }

    void PausarVuforia()
    {
        if (vuforia == null) vuforia = FindAnyObjectByType<VuforiaBehaviour>();
        if (vuforia != null) vuforia.enabled = false;
    }
}
