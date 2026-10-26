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
        // Si Vuforia detecta un código y aún no hemos sacado el modelo...
        if (!yaLeido && barcode != null && barcode.InstanceData != null)
        {
            string textoEscaneado = barcode.InstanceData.Text;
            
            if (!string.IsNullOrEmpty(textoEscaneado))
            {
                Debug.Log("¡QR detectado por Vuforia!: " + textoEscaneado);
                
                // Le enviamos el texto a tu script de SQLite
                gestorBD.ProcesarEscaneoQR(textoEscaneado);
                
                // Bloqueamos la lectura para que no instancie 100 cubos infinitos
                yaLeido = true; 
            }
        }
    }
}
