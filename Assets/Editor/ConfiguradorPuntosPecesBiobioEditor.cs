#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ConfiguradorPuntosPecesBiobioEditor
{
    private struct InfoPuntoBiobio
    {
        public string id;
        public string nombreComun;
        public string nombreCientifico;
        public string nombreLocal;
        public string habitat;
        public Vector3 posicion;

        public InfoPuntoBiobio(string id, string comun, string cientifico, string local, string hab, Vector3 pos)
        {
            this.id = id;
            this.nombreComun = comun;
            this.nombreCientifico = cientifico;
            this.nombreLocal = local;
            this.habitat = hab;
            this.posicion = pos;
        }
    }

    // Coordenadas calibradas con precisión milimétrica sobre el lomo dorsal de cada pez
    private static readonly InfoPuntoBiobio[] listaPeces = new InfoPuntoBiobio[]
    {
        new InfoPuntoBiobio(
            "Bagrecito",
            "BAGRECITO",
            "Bullockia maldonadoi",
            "Bagrecito",
            "Ríos andinos del centro y sur de Chile, en el caudal y la orilla.",
            new Vector3(-4.36f, 0.48f, -1.59f)
        ),
        new InfoPuntoBiobio(
            "PejerreyChileno",
            "PEJERREY CHILENO",
            "Basilichthys australis",
            "Cauque, pejerrey",
            "Desde el Aconcagua a la Región de Los Lagos, en aguas oxigenadas de baja velocidad.",
            new Vector3(5.08f, 0.55f, -1.85f)
        ),
        new InfoPuntoBiobio(
            "PercaTrucha",
            "PERCA TRUCHA",
            "Percichthys trucha",
            "Perca chilena, trucha chilena, trucha criolla",
            "Desde Valdivia a la Tierra del Fuego, en lagos o zonas bajas de los ríos.",
            new Vector3(1.26f, 0.58f, -1.92f)
        ),
        new InfoPuntoBiobio(
            "PuyeChico",
            "PUYE CHICO",
            "Galaxias maculatus",
            "Puye (mapudungun)",
            "En ríos y esteros de aguas poco profundas, donde abundan hojas y ramas.",
            new Vector3(-2.74f, 0.50f, -3.26f)
        ),
        new InfoPuntoBiobio(
            "TolloAguaDulce",
            "TOLLO DE AGUA DULCE",
            "Diplomystes nahuelbutaensis",
            "Tollo, bagre",
            "Ríos de la región del Maule a la Araucanía. Sectores caudalosos y profundos.",
            new Vector3(0.28f, 0.54f, -3.85f)
        ),
        new InfoPuntoBiobio(
            "TruchaArcoiris",
            "TRUCHA ARCOÍRIS (INTRODUCIDA)",
            "Oncorhynchus mykiss",
            "Cabeza de acero",
            "Ríos, arroyos y lagos de aguas frías y transparentes.",
            new Vector3(6.09f, 0.72f, -4.19f)
        ),
        new InfoPuntoBiobio(
            "BagreChico",
            "BAGRE CHICO",
            "Trichomycterus areolatus",
            "Pez gato, bagre pintado o bagre de centro",
            "De amplia distribución en Chile, bajo piedras en aguas calmas y litoral arenoso.",
            new Vector3(-2.70f, 0.46f, -0.88f)
        ),
        new InfoPuntoBiobio(
            "CarmelitaConcepcion",
            "CARMELITA DE CONCEPCIÓN",
            "Percilia irwini",
            "Carmelita, coloradita",
            "Ríos y lagos de la región del Biobío, donde se desplaza sobre piedras.",
            new Vector3(-0.96f, 0.50f, -1.85f)
        ),
        new InfoPuntoBiobio(
            "TruchaFario",
            "TRUCHA FARIO (INTRODUCIDA)",
            "Salmo trutta fario",
            "Trucha Café",
            "Zonas altas de los ríos, donde el agua es limpia y oxigenada.",
            new Vector3(0.39f, 0.70f, 0.59f)
        )
    };

    [MenuItem("Colbún/🐟 CALIBRAR PUNTOS DIRECTO SOBRE CADA PEZ (ESTACIÓN 4)")]
    public static void AplicarPuntosPrefab()
    {
        string rutaPrefab = "Assets/Resources/Modelos3D/Estacion4/BiobioPrefab.prefab";
        string rutaMaterial = "Assets/Resources/Modelos3D/Estacion3/MaterialPunto2D.mat";

        Material matPunto = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        if (matPunto == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró el material en: " + rutaMaterial, "OK");
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(rutaPrefab);
        if (prefabRoot == null)
        {
            EditorUtility.DisplayDialog("Error", "No se pudo abrir: " + rutaPrefab, "OK");
            return;
        }

        try
        {
            prefabRoot.transform.localPosition = Vector3.zero;

            if (prefabRoot.GetComponent<GestorPuntosPecesBiobioAR>() == null)
            {
                prefabRoot.AddComponent<GestorPuntosPecesBiobioAR>();
            }

            Transform previo = prefabRoot.transform.Find("PuntosInteractivos");
            if (previo != null)
            {
                Object.DestroyImmediate(previo.gameObject);
            }

            GameObject contenedor = new GameObject("PuntosInteractivos");
            contenedor.transform.SetParent(prefabRoot.transform, false);
            contenedor.transform.localPosition = Vector3.zero;
            contenedor.transform.localRotation = Quaternion.identity;
            contenedor.transform.localScale = Vector3.one;

            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Mesh quadMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tempQuad);

            float tamanoPunto = 0.50f;
            Vector3 escalaPunto = new Vector3(tamanoPunto, tamanoPunto, tamanoPunto);
            Quaternion rotacionVertical = Quaternion.identity;

            foreach (var p in listaPeces)
            {
                GameObject objPunto = new GameObject("Punto_" + p.id);
                objPunto.transform.SetParent(contenedor.transform, false);
                objPunto.transform.localPosition = p.posicion;
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
                col.radius = 1.0f;

                PuntoPezBiobioAR logica = objPunto.AddComponent<PuntoPezBiobioAR>();
                logica.nombreComun = p.nombreComun;
                logica.nombreCientifico = p.nombreCientifico;
                logica.nombreLocal = p.nombreLocal;
                logica.habitat = p.habitat;
                logica.colorNormal = new Color(0.22f, 0.74f, 0.97f, 0.95f);
                logica.colorSeleccionado = new Color(0.85f, 0.98f, 1f, 1f);
                logica.AsignarEscalaOriginal(escalaPunto);
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, rutaPrefab);
            Debug.Log("[ConfiguradorBiobio] ¡Los 9 puntos calibrados exactamente sobre el lomo dorsal de cada pez!");

            EditorUtility.DisplayDialog("¡Puntos Calibrados!",
                "Los 9 puntos 2D se han colocado exactamente sobre el lomo dorsal de cada pez:\n\n" +
                "1. Bagrecito -> Encima de su lomo\n" +
                "2. Pejerrey Chileno -> Encima de su lomo\n" +
                "3. Perca Trucha -> Encima de su lomo\n" +
                "4. Puye Chico -> Encima de su lomo\n" +
                "5. Tollo de Agua Dulce -> Encima de su lomo\n" +
                "6. Trucha Arcoíris -> Encima de su aleta dorsal\n" +
                "7. Bagre Chico -> Encima de su lomo\n" +
                "8. Carmelita de Concepción -> Encima de su lomo\n" +
                "9. Trucha Fario -> Encima de su lomo dorsal\n\n" +
                "¡Ahora cada punto está inequívocamente sobre su respectivo pez!",
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
