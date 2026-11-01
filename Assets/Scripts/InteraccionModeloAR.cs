using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Controles táctiles para el modelo que aparece después de escanear un QR.
/// Un dedo gira el modelo; dos dedos lo mueven en el plano de la pantalla y
/// permiten acercarlo o alejarlo con el gesto de pinza.
/// </summary>
public class InteraccionModeloAR : MonoBehaviour
{
    [SerializeField] float velocidadGiro = 0.25f;
    [SerializeField] float velocidadMovimiento = 0.002f;
    [SerializeField] float escalaMinima = 0.001f;
    [SerializeField] float escalaMaxima = 0.02f;
    [SerializeField] float sensibilidadZoom = 0.005f;

    Camera camara;
    void Start()
    {
        EnhancedTouchSupport.Enable();
        camara = Camera.main;
        if (camara == null) camara = FindAnyObjectByType<Camera>();
    }

    void Update()
    {
        var toques = Touch.activeTouches;
        if (camara == null || toques.Count == 0) return;

        if (toques.Count == 1)
        {
            var toque = toques[0];
            if (toque.phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                // Arrastrar con un dedo gira el objeto alrededor de sus ejes.
                transform.Rotate(camara.transform.up, -toque.delta.x * velocidadGiro, Space.World);
                transform.Rotate(camara.transform.right, toque.delta.y * velocidadGiro, Space.World);
            }
        }
        else if (toques.Count >= 2)
        {
            var a = toques[0];
            var b = toques[1];
            Vector2 anteriorA = a.screenPosition - a.delta;
            Vector2 anteriorB = b.screenPosition - b.delta;
            float distanciaAnterior = Vector2.Distance(anteriorA, anteriorB);
            float distanciaActual = Vector2.Distance(a.screenPosition, b.screenPosition);

            // Pinza: abrir amplía el modelo y cerrar lo reduce.
            float cambioPinza = distanciaActual - distanciaAnterior;
            float factor = 1f + cambioPinza * sensibilidadZoom;
            float escala = Mathf.Clamp(transform.localScale.x * factor, escalaMinima, escalaMaxima);
            transform.localScale = Vector3.one * escala;

            // Desplazar ambos dedos mueve el modelo sobre el plano frente a cámara.
            Vector2 deltaCentro = (a.delta + b.delta) * 0.5f;
            transform.position += camara.transform.right * (deltaCentro.x * velocidadMovimiento)
                                + camara.transform.up * (deltaCentro.y * velocidadMovimiento);
        }
    }
}
