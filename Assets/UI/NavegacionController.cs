using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class NavegacionController : MonoBehaviour
{
    VisualElement barra, pantallaPasaporte, pantallaEscaner, pantallaEncuesta, pantallaKiba;
    Button btnPasaporte, btnCamara, btnKiba, btnEncuesta;

    void OnEnable()
    {
        var raiz = GetComponent<UIDocument>().rootVisualElement;

        barra            = raiz.Q<VisualElement>("barra-nav");
        pantallaPasaporte = raiz.Q<VisualElement>("pantalla-pasaporte");
        pantallaEscaner   = raiz.Q<VisualElement>("pantalla-escaner");
        pantallaEncuesta  = raiz.Q<VisualElement>("pantalla-encuesta");
        pantallaKiba      = raiz.Q<VisualElement>("pantalla-kiba");

        btnPasaporte = raiz.Q<Button>("btn-nav-pasaporte");
        btnCamara    = raiz.Q<Button>("btn-nav-camara");
        btnKiba      = raiz.Q<Button>("btn-nav-kiba");
        btnEncuesta  = raiz.Q<Button>("btn-nav-encuesta");

        pantallaEncuesta.style.display = DisplayStyle.None;
        pantallaKiba.style.display     = DisplayStyle.None;

        btnPasaporte.clicked += () => Mostrar(pantallaPasaporte);
        btnKiba.clicked      += () => Mostrar(pantallaKiba);
        btnEncuesta.clicked  += () => Mostrar(pantallaEncuesta);
        // El visor lo abre PasaporteController; aquí solo tapamos lo que haya debajo
        btnCamara.clicked    += () =>
        {
            pantallaEncuesta.style.display = DisplayStyle.None;
            pantallaKiba.style.display     = DisplayStyle.None;
        };

        raiz.schedule.Execute(Actualizar).Every(100);   // revisa la pantalla actual
    }

    void Mostrar(VisualElement destino)
    {
        pantallaPasaporte.style.display = DisplayStyle.None;
        pantallaEncuesta.style.display  = DisplayStyle.None;
        pantallaKiba.style.display      = DisplayStyle.None;
        destino.style.display           = DisplayStyle.Flex;
    }

    void Actualizar()
    {
        bool visorAbierto = pantallaEscaner.resolvedStyle.display != DisplayStyle.None;
        barra.style.display = visorAbierto ? DisplayStyle.None : DisplayStyle.Flex;   // sin barra en el visor

        Marcar(btnPasaporte, pantallaPasaporte);
        Marcar(btnKiba, pantallaKiba);
        Marcar(btnEncuesta, pantallaEncuesta);
    }

    void Marcar(Button boton, VisualElement pantalla)
    {
        boton.EnableInClassList("nav-btn--activo", pantalla.resolvedStyle.display != DisplayStyle.None);
    }
}