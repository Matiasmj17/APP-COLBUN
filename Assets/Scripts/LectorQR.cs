using UnityEngine;
using Vuforia; // Importamos la librería de Vuforia

public class LectorQR : MonoBehaviour
{
    public GestionSQLite gestorBD; // Aquí conectaremos tu base de datos
    private BarcodeBehaviour barcode;
    private bool yaLeido = false;
    private ControladorCamaraAR controladorCamara;

    void Start()
    {
        barcode = GetComponent<BarcodeBehaviour>();
        controladorCamara = ControladorCamaraAR.Instancia;
    }

    void Update()
    {
        if (barcode == null || controladorCamara == null) return;

        // Al estar fuera del modo escáner, permite volver a leer el mismo QR
        // cuando el usuario abra el visor otra vez.
        if (!controladorCamara.EscaneoQRActivo)
        {
            yaLeido = false;
            return;
        }

        // Cuando el QR sale de la cámara, se permite leer otro (o el mismo de nuevo)
        if (barcode.InstanceData == null) { yaLeido = false; return; }

        if (yaLeido) return;

        string textoEscaneado = barcode.InstanceData.Text;
        if (!string.IsNullOrEmpty(textoEscaneado))
        {
            Debug.Log("¡QR detectado por Vuforia!: " + textoEscaneado);
            controladorCamara.DetenerLecturaQR();
            gestorBD.ProcesarEscaneoQR(textoEscaneado);
            yaLeido = true;   // evita que cree el modelo mil veces mientras el QR sigue a la vista
        }
    }
}
