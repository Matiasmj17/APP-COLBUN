using System;                       // Para usar Action
using UnityEngine;                  // Para MonoBehaviour

public class QRScanner : MonoBehaviour   // Versión vacía, solo para que compile el Pasaporte
{
    public event Action<string> OnQRDetected;   // Evento que usa PasaporteController (aquí nadie lo dispara)
    public bool escaneando;                     // Interruptor que usa PasaporteController
}