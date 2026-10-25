# 🌲 Pasaporte Digital - Aplicación RA Colbún

¡Hola! Este repositorio contiene la versión actualizada del Pasaporte Digital con Realidad Aumentada para el Sendero Botánico Colbún.

## 🎨 Actualizaciones de Interfaz (UI/UX)
Se finalizó la integración del diseño visual utilizando **UI Toolkit**. Los estilos se gestionan desde el archivo `Assets/UI/Pasaporte.uss`.
*   **Logos e Íconos:** Se integró el logo oficial de Colbún (`logo_colbun.jpg`) y se corrigieron las proporciones mediante CSS (`flex-shrink: 0`, `min-width`, `min-height`) para evitar que Unity aplaste los vectores (Ampolleta, Candados, Trofeo y Cámara AR).
*   **Botón de Escáner:** Se ajustó la clase `.flotante` para anclar correctamente el botón de "Escanear QR" e integrar su respectivo ícono junto al texto.
*   **Limpieza de Jerarquía:** Se eliminó el objeto `UI` de la plantilla base de Unity que generaba un cuadro negro sobre la cámara AR.

## ⚠️ ADVERTENCIA CRÍTICA DE COMPILACIÓN (LEER ANTES DE ABRIR)
El compilador de Android (NDK/CMake) falla catastróficamente si encuentra tildes o espacios en las rutas del sistema. Para poder compilar el `.apk` en tu equipo, **debes seguir estas reglas**:

1.  **Ruta del Proyecto:** Clona este repositorio en una ruta limpia, sin espacios ni caracteres especiales. 
    *   ✅ Correcto: `C:\PROYECTOS\AplicacionRaColbun`
    *   ❌ Incorrecto: `C:\Users\José\Mi Proyecto Colbún`
2.  **Caché de Gradle (Variable de Entorno):** Si tu nombre de usuario de Windows tiene tildes (ej. "matías"), Gradle colapsará. Debes crear una Variable de Entorno en Windows llamada `GRADLE_USER_HOME` y asignarle una ruta limpia (ej. `C:\PROYECTOS\.gradle_cache`).
3.  **Arquitectura:** En `Project Settings > Player > Other Settings`, asegúrate de que el *Scripting Backend* sea **IL2CPP** y la arquitectura objetivo (*Target Architectures*) sea **únicamente ARM64**.

## 📱 Estado Actual
*   El diseño está alineado con el Mockup original.
*   Se generó exitosamente la compilación a `.apk`.
*   *(En progreso)* Pruebas físicas en dispositivo Samsung (Android) para validar el escaneo de los códigos QR de las estaciones y el desbloqueo progresivo del pasaporte.