using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ControladorCamaraAR : MonoBehaviour
{
    [Tooltip("Arrastra aquí el GameObject 'AR Session' de tu jerarquía")]
    public ARSession arSession;

    void Start()
    {
        // Apaga la cámara y el rastreo apenas inicia la app para evitar lag
        DesactivarCamaraAR();
    }

    // Llama a este método desde el botón flotante de escanear QR
    public void ActivarCamaraAR()
    {
        if (arSession != null)
        {
            arSession.enabled = true;
            Debug.Log("Cámara AR Encendida - Escáner activo");
        }
    }

    // Llama a este método desde los 4 botones de navegación inferiores
    public void DesactivarCamaraAR()
    {
        if (arSession != null)
        {
            arSession.enabled = false;
            Debug.Log("Cámara AR Apagada - Ahorrando batería");
        }
    }
}