using UnityEngine;
using Vuforia; // Importamos la librería de Vuforia

public class LectorQR : MonoBehaviour
{
    public GestionSQLite gestorBD; // Aquí conectaremos tu base de datos
    private BarcodeBehaviour barcode;
    private bool yaLeido = false;
    private ControladorCamaraAR controladorCamara;
    private float tiempoReintento = 0f;
    private string ultimoTextoRechazado = "";

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
            ultimoTextoRechazado = "";
            return;
        }

        // Cuando el QR sale de la cámara, se permite leer otro (o el mismo de nuevo)
        if (barcode.InstanceData == null) { yaLeido = false; return; }

        if (yaLeido) return;

        string textoEscaneado = barcode.InstanceData.Text;
        if (!string.IsNullOrEmpty(textoEscaneado))
        {
            // Si este texto fue rechazado recientemente (por ejemplo estación bloqueada), esperar antes de volver a procesarlo
            if (textoEscaneado == ultimoTextoRechazado && Time.time < tiempoReintento)
            {
                return;
            }

            Debug.Log("¡QR detectado por Vuforia!: " + textoEscaneado);
            if (gestorBD == null) gestorBD = FindAnyObjectByType<GestionSQLite>();

            bool procesado = (gestorBD != null) && gestorBD.ProcesarEscaneoQR(textoEscaneado);
            if (procesado)
            {
                controladorCamara.DetenerLecturaQR();
                yaLeido = true;   // evita que cree el modelo mil veces mientras el QR sigue a la vista
                ultimoTextoRechazado = "";
            }
            else
            {
                // Si el QR no fue reconocido o fue bloqueado, esperar 2.5s antes de reintentar el mismo QR
                yaLeido = false;
                ultimoTextoRechazado = textoEscaneado;
                tiempoReintento = Time.time + 2.5f;
            }
        }
    }
}
