using UnityEngine;
using UnityEngine.UIElements;

public class ControladorUI : MonoBehaviour
{
    [Tooltip("Arrastra aquí el objeto que tiene tu UIDocument")]
    public UIDocument documentoUI;
    
    [Tooltip("Arrastra aquí el script de la cámara que creamos antes")]
    public ControladorCamaraAR controladorCamara;

    void OnEnable()
    {
        // Obtenemos la raíz de tu diseño UI Toolkit
        var root = documentoUI.rootVisualElement;

        // 1. Buscamos los botones por su nombre exacto en el UI Builder
        Button btnQR = root.Q<Button>("BotonQR");
        Button btnNav1 = root.Q<Button>("BotonNav1");
        Button btnNav2 = root.Q<Button>("BotonNav2");
        Button btnNav3 = root.Q<Button>("BotonNav3");
        Button btnNav4 = root.Q<Button>("BotonNav4");

        // 2. Le decimos qué hacer a cada botón cuando le hagan clic
        if (btnQR != null)
        {
            btnQR.clicked += () => controladorCamara.ActivarCamaraAR();
        }

        // A los 4 botones inferiores les asignamos apagar la cámara
        if (btnNav1 != null) btnNav1.clicked += () => controladorCamara.DesactivarCamaraAR();
        if (btnNav2 != null) btnNav2.clicked += () => controladorCamara.DesactivarCamaraAR();
        if (btnNav3 != null) btnNav3.clicked += () => controladorCamara.DesactivarCamaraAR();
        if (btnNav4 != null) btnNav4.clicked += () => controladorCamara.DesactivarCamaraAR();
    }
}