using UnityEngine;
using Vuforia; // Importamos la librería de Vuforia

public class LectorQR : MonoBehaviour
{
    public GestionSQLite gestorBD; // Aquí conectaremos tu base de datos
    private BarcodeBehaviour barcode;
    private bool yaLeido = false;

    void Start()
    {
        barcode = GetComponent<BarcodeBehaviour>();
    }

    void Update()
    {
        if (barcode == null) return;

        // Cuando el QR sale de la cámara, se permite leer otro (o el mismo de nuevo)
        if (barcode.InstanceData == null) { yaLeido = false; return; }

        if (yaLeido) return;

        string textoEscaneado = barcode.InstanceData.Text;
        if (!string.IsNullOrEmpty(textoEscaneado))
        {
            Debug.Log("¡QR detectado por Vuforia!: " + textoEscaneado);
            gestorBD.ProcesarEscaneoQR(textoEscaneado);
            yaLeido = true;   // evita que cree el modelo mil veces mientras el QR sigue a la vista
        }
    }
}