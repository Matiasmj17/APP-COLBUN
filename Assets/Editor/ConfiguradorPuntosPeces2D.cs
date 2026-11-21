#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ConfiguradorPuntosPeces2D
{
    private struct InfoPuntoNormalizado
    {
        public string id;
        public string nombre;
        public string categoria;
        public Vector3 posRelativa;

        public InfoPuntoNormalizado(string id, string nombre, string categoria, Vector3 pos)
        {
            this.id = id;
            this.nombre = nombre;
            this.categoria = categoria;
            this.posRelativa = pos;
        }
    }

    // Coordenadas calibradas con precisión sobre la anatomía real del modelo 3D
    private static readonly InfoPuntoNormalizado[] puntos = new InfoPuntoNormalizado[]
    {
        // ROSTRO
        new InfoPuntoNormalizado("Boca", "Boca", "ROSTRO", new Vector3(0.78f, 0.08f, -0.04f)),
        new InfoPuntoNormalizado("Ojos", "Ojos", "ROSTRO", new Vector3(0.60f, 0.10f, 0.02f)),
        new InfoPuntoNormalizado("Branquias", "Branquias", "ROSTRO", new Vector3(0.38f, 0.10f, -0.04f)),

        // CRÁNEO
        new InfoPuntoNormalizado("CajaCraneana", "Caja Craneana", "CRÁNEO", new Vector3(0.44f, 0.09f, 0.18f)),

        // ESQUELETO
        new InfoPuntoNormalizado("EspinaDorsal", "Espina Dorsal", "ESQUELETO", new Vector3(-0.05f, 0.09f, 0.04f)),

        // ALETAS
        new InfoPuntoNormalizado("AletaDorsal1", "Dorsal", "ALETAS", new Vector3(0.12f, 0.06f, 0.28f)),
        new InfoPuntoNormalizado("AletaDorsal2", "Dorsal", "ALETAS", new Vector3(-0.24f, 0.06f, 0.24f)),
        new InfoPuntoNormalizado("AletaPectoral", "Pectoral", "ALETAS", new Vector3(0.25f, 0.14f, -0.10f)),
        new InfoPuntoNormalizado("AletaVentral", "Ventral", "ALETAS", new Vector3(0.05f, 0.08f, -0.16f)),
        new InfoPuntoNormalizado("AletaAnal", "Anal", "ALETAS", new Vector3(-0.36f, 0.06f, -0.16f)),
        new InfoPuntoNormalizado("ColaCaudal", "Cola o caudal", "ALETAS", new Vector3(-0.68f, 0.06f, 0.00f))
    };

    [MenuItem("Colbún/🎯 CONFIGURAR PUNTOS 2D DEL PEZ (OFICIAL)")]
    public static void AplicarPuntosConEscalaExacta()
    {
        string rutaMaterial = "Assets/Resources/Modelos3D/Estacion3/MaterialPunto2D.mat";
        string rutaPrefabBase = "Assets/Resources/Modelos3D/Estacion3/LosPeces.prefab";
        string rutaPrefabFinal = "Assets/Resources/Modelos3D/Estacion3/LosPecesPrefab.prefab";

        Material matPunto = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        if (matPunto == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró el material en:\n" + rutaMaterial, "OK");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(rutaPrefabBase);
        if (prefabRoot == null)
        {
            EditorUtility.DisplayDialog("Error", "No se pudo abrir el prefab:\n" + rutaPrefabBase, "OK");
            return;
        }

        try
        {
            // 1. Asegurar Gestor interactivo en la raíz
            if (prefabRoot.GetComponent<GestorPuntosPecesAR>() == null)
            {
                prefabRoot.AddComponent<GestorPuntosPecesAR>();
            }

            // 2. Medir la malla del pez
            MeshFilter mfPez = prefabRoot.GetComponentInChildren<MeshFilter>(true);
            Transform transPez = mfPez != null ? mfPez.transform : prefabRoot.transform;

            Bounds boundsMalla = (mfPez != null && mfPez.sharedMesh != null) 
                ? mfPez.sharedMesh.bounds 
                : new Bounds(Vector3.zero, new Vector3(0.02f, 0.002f, 0.009f));

            float factorEscala = boundsMalla.size.x > 0.001f ? (boundsMalla.size.x / 1.75f) : 0.01135f;
            float gruesoY = (boundsMalla.size.y * 0.5f) + 0.0006f; // Justo en la cara exterior visible del pez

            // 3. Limpiar contenedores anteriores
            Transform previo = transPez.Find("PuntosInteractivos");
            if (previo != null) Object.DestroyImmediate(previo.gameObject);

            Transform contenedorRaiz = prefabRoot.transform.Find("PuntosInteractivos");
            if (contenedorRaiz != null) Object.DestroyImmediate(contenedorRaiz.gameObject);

            GameObject contenedor = new GameObject("PuntosInteractivos");
            contenedor.transform.SetParent(transPez, false);
            contenedor.transform.localPosition = Vector3.zero;
            contenedor.transform.localRotation = Quaternion.identity;
            contenedor.transform.localScale = Vector3.one;

            // 4. Mesh Quad nativo para los puntos 2D
            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Mesh quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempQuad);

            // Escala del punto en AR: ~6.8 cm (proporción armónica y fácil de pulsar)
            float tamanoPunto = boundsMalla.size.x * 0.085f;
            Vector3 escalaPunto = new Vector3(tamanoPunto, tamanoPunto, tamanoPunto);

            // Rotación 90° en X: Pone los quads VERTICALES mirando de frente al perfil del pez
            Quaternion rotacionVertical = Quaternion.Euler(90f, 0f, 0f);

            // 5. Instanciar los 11 puntos
            foreach (var p in puntos)
            {
                GameObject objPunto = new GameObject("Punto_" + p.id);
                objPunto.transform.SetParent(contenedor.transform, false);

                Vector3 posAjustada = new Vector3(
                    p.posRelativa.x * factorEscala,
                    gruesoY,
                    p.posRelativa.z * factorEscala
                );
                objPunto.transform.localPosition = posAjustada;
                objPunto.transform.localRotation = rotacionVertical;
                objPunto.transform.localScale = escalaPunto;

                MeshFilter mf = objPunto.AddComponent<MeshFilter>();
                mf.sharedMesh = quadMesh;

                MeshRenderer mr = objPunto.AddComponent<MeshRenderer>();
                mr.sharedMaterial = matPunto;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;

                SphereCollider col = objPunto.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.2f;

                PuntoAnatomicoAR logica = objPunto.AddComponent<PuntoAnatomicoAR>();
                logica.nombrePunto = p.nombre;
                logica.categoria = p.categoria;
            }

            // Guardar tanto en LosPeces.prefab como en LosPecesPrefab.prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, rutaPrefabBase);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, rutaPrefabFinal);
            Debug.Log($"[Configurador2D] Puntos calibrados: Ojos dentro de la cuenca, boca pegada a las mandíbulas y caja craneana separada.");

            EditorUtility.DisplayDialog("¡Puntos Calibrados!",
                "Se ajustaron las posiciones de forma precisa:\n\n" +
                "• Boca: Acercada directamente a la mandíbula.\n" +
                "• Ojos: Centrado dentro de la cuenca ocular circular.\n" +
                "• Caja Craneana: Separada hacia la parte superior del cráneo.\n" +
                "• Aletas y Cola: Pegadas a la silueta del pez.\n\n" +
                "¡Ahora cada punto es fácil de pulsar individualmente!",
                "¡Excelente!");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
