using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú  Personaje > Crear escena pelea
/// Ring abierto (cuerdas solo en dos lados), público alrededor, focos de luz,
/// el jugador (azul) contra un bot (rojo).
/// </summary>
public static class PeleaEditor
{
    const string Carpeta = ConfigurarPersonaje.Raiz + "/Pelea/Generado";
    public const string EscenaPelea = Carpeta + "/Escena_Pelea.unity";
    const float Medio = 5f; // el ring mide 10 x 10

    [MenuItem("Personaje/Crear escena pelea", priority = 2)]
    public static void Menu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Crear();
    }

    public static void Batch()
    {
        try { Crear(); EditorApplication.Exit(0); }
        catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static Material M(string n, Color c, Color? e = null) => ConfigurarPersonaje.Mat(Carpeta, n, c, null, e);

    static GameObject Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 esc, Material mat, Vector3 rot = default, bool colision = true)
    {
        var g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localEulerAngles = rot;
        g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!colision) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g;
    }

    static void Crear()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(Carpeta);
        if (!AssetDatabase.IsValidFolder(ConfigurarPersonaje.Raiz + "/Pelea")) AssetDatabase.CreateFolder(ConfigurarPersonaje.Raiz, "Pelea");
        AssetDatabase.CreateFolder(ConfigurarPersonaje.Raiz + "/Pelea", "Generado");

        var clips = ConfigurarPersonaje.ObtenerClips();
        var ctrl = ConfigurarPersonaje.CrearController(Carpeta + "/PersonajePelea.controller", clips);

        var mLona = M("Lona", new Color(0.2f, 0.35f, 0.75f));
        var mBorde = M("Borde", new Color(0.92f, 0.92f, 0.95f));
        var mBase = M("BaseRing", new Color(0.12f, 0.12f, 0.14f));
        var mPosteAzul = M("PosteAzul", new Color(0.2f, 0.4f, 1f));
        var mPosteRojo = M("PosteRojo", new Color(1f, 0.2f, 0.2f));
        var mMetal = M("Metal", new Color(0.7f, 0.7f, 0.75f));
        var mCuerda = M("Cuerda", new Color(0.95f, 0.95f, 0.95f));
        var mSuelo = M("Suelo", new Color(0.18f, 0.17f, 0.2f));
        var mGrada = M("Grada", new Color(0.3f, 0.3f, 0.35f));

        // ---- Luz: arena oscura con focos sobre el ring ----
        var sol = new GameObject("LuzAmbiente").AddComponent<Light>();
        sol.type = LightType.Directional;
        sol.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
        sol.intensity = 0.35f;
        sol.color = new Color(0.7f, 0.75f, 1f);
        sol.shadows = LightShadows.Soft;
        RenderSettings.sun = sol;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.28f);
        foreach (var (pos, color) in new[]
        {
            (new Vector3(-4, 8, -4), new Color(1f, 0.9f, 0.75f)), (new Vector3(4, 8, -4), new Color(1f, 0.9f, 0.75f)),
            (new Vector3(-4, 8, 4), new Color(0.75f, 0.85f, 1f)), (new Vector3(4, 8, 4), new Color(1f, 0.75f, 0.75f)),
        })
        {
            var foco = new GameObject("Foco").AddComponent<Light>();
            foco.type = LightType.Spot;
            foco.transform.position = pos;
            foco.transform.LookAt(Vector3.zero);
            foco.color = color;
            foco.intensity = 25f;
            foco.range = 18f;
            foco.spotAngle = 55f;
            foco.shadows = LightShadows.Soft;
        }

        // ---- Ring ----
        var ring = new GameObject("Ring").transform;
        Pieza(PrimitiveType.Cube, "Lona", ring, new Vector3(0, -0.15f, 0), new Vector3(Medio * 2, 0.3f, Medio * 2), mLona);
        Pieza(PrimitiveType.Cube, "Base", ring, new Vector3(0, -1.1f, 0), new Vector3(Medio * 2 + 0.4f, 1.6f, Medio * 2 + 0.4f), mBase);
        // Borde blanco alrededor de la lona
        foreach (var (p, e) in new[]
        {
            (new Vector3(0, 0.01f, Medio - 0.15f), new Vector3(Medio * 2, 0.02f, 0.3f)), (new Vector3(0, 0.01f, -Medio + 0.15f), new Vector3(Medio * 2, 0.02f, 0.3f)),
            (new Vector3(Medio - 0.15f, 0.01f, 0), new Vector3(0.3f, 0.02f, Medio * 2)), (new Vector3(-Medio + 0.15f, 0.01f, 0), new Vector3(0.3f, 0.02f, Medio * 2)),
        })
            Pieza(PrimitiveType.Cube, "Borde", ring, p, e, mBorde, default, false);
        // Logo en el centro
        Pieza(PrimitiveType.Cylinder, "Logo", ring, new Vector3(0, 0.01f, 0), new Vector3(3f, 0.01f, 3f), mBorde, default, false);
        Pieza(PrimitiveType.Cylinder, "Logo2", ring, new Vector3(0, 0.015f, 0), new Vector3(2.6f, 0.01f, 2.6f), mLona, default, false);

        // Postes en las 4 esquinas (azul en la esquina del jugador, rojo en la del bot)
        float esq = Medio - 0.2f;
        foreach (var (x, z) in new[] { (-esq, -esq), (esq, -esq), (-esq, esq), (esq, esq) })
        {
            Pieza(PrimitiveType.Cylinder, "Poste", ring, new Vector3(x, 0.8f, z), new Vector3(0.18f, 0.8f, 0.18f), mMetal);
            var colorPad = x < 0 ? mPosteAzul : mPosteRojo;
            for (int i = 0; i < 3; i++)
                Pieza(PrimitiveType.Cube, "Almohadilla", ring, new Vector3(x, 0.45f + i * 0.4f, z), new Vector3(0.3f, 0.3f, 0.3f), colorPad, new Vector3(0, 45, 0), false);
        }
        // Cuerdas SOLO en dos lados (fondo e izquierda): por el frente y la derecha se puede caer
        for (int i = 0; i < 3; i++)
        {
            float y = 0.45f + i * 0.4f;
            Pieza(PrimitiveType.Cylinder, "CuerdaFondo", ring, new Vector3(0, y, esq), new Vector3(0.05f, esq, 0.05f), mCuerda, new Vector3(0, 0, 90));
            Pieza(PrimitiveType.Cylinder, "CuerdaIzq", ring, new Vector3(-esq, y, 0), new Vector3(0.05f, esq, 0.05f), mCuerda, new Vector3(90, 0, 0));
        }
        // Escalones para volver a subir
        Pieza(PrimitiveType.Cube, "Escalon1", ring, new Vector3(Medio + 0.7f, -1.25f, 2f), new Vector3(1f, 1.3f, 2f), mBase);
        Pieza(PrimitiveType.Cube, "Escalon2", ring, new Vector3(Medio + 1.7f, -1.6f, 2f), new Vector3(1f, 0.6f, 2f), mBase);

        // ---- Suelo de la arena y público ----
        var arena = new GameObject("Arena").transform;
        Pieza(PrimitiveType.Cube, "Suelo", arena, new Vector3(0, -2.05f, 0), new Vector3(60, 0.1f, 60), mSuelo);
        var azar = new System.Random(5);
        float A(float a, float b) => a + (float)azar.NextDouble() * (b - a);
        var colores = Enumerable.Range(0, 6).Select(i => M("Publico" + i, Color.HSVToRGB(i / 6f, 0.6f, 0.85f))).ToArray();
        for (int lado = 0; lado < 3; lado++) // gradas en 3 lados (el frente queda para la cámara)
        {
            var g = new GameObject("Grada" + lado).transform;
            g.SetParent(arena, false);
            g.rotation = Quaternion.Euler(0, lado * 90 - 90, 0); // izquierda, fondo, derecha
            for (int fila = 0; fila < 3; fila++)
            {
                float d = 10f + fila * 1.6f, h = -2f + fila * 0.8f;
                Pieza(PrimitiveType.Cube, "Escalon", g, new Vector3(0, h + 0.4f, d), new Vector3(20, 0.8f + fila * 0.0f, 1.6f), mGrada);
                for (float x = -9f; x <= 9f; x += A(1.2f, 1.8f))
                {
                    var persona = new GameObject("Persona").transform;
                    persona.SetParent(g, false);
                    persona.localPosition = new Vector3(x, h + 0.8f, d);
                    var col = colores[azar.Next(colores.Length)];
                    Pieza(PrimitiveType.Capsule, "Cuerpo", persona, new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.45f, 0.4f), col, default, false);
                    Pieza(PrimitiveType.Sphere, "Cabeza", persona, new Vector3(0, 1.05f, 0), Vector3.one * 0.32f, mBorde, default, false);
                }
            }
        }

        // ---- Luchadores ----
        var jugadorGo = ConfigurarPersonaje.CrearPersonaje(ctrl, M("Jugador", new Color(0.55f, 0.75f, 1f)), out var animJugador);
        jugadorGo.name = "Jugador";
        jugadorGo.transform.SetPositionAndRotation(new Vector3(-2f, 0.05f, 0), Quaternion.Euler(0, 90, 0));
        var botGo = ConfigurarPersonaje.CrearPersonaje(ctrl, M("Bot", new Color(1f, 0.45f, 0.4f)), out var animBot);
        botGo.name = "Bot";
        botGo.layer = 0;
        botGo.transform.SetPositionAndRotation(new Vector3(2f, 0.05f, 0), Quaternion.Euler(0, -90, 0));

        var lJ = jugadorGo.AddComponent<Luchador>();
        lJ.nombre = "TÚ";
        lJ.animator = animJugador;
        lJ.cuerpo = jugadorGo.GetComponentsInChildren<Renderer>();
        var lB = botGo.AddComponent<Luchador>();
        lB.nombre = "BOT";
        lB.animator = animBot;
        lB.cuerpo = botGo.GetComponentsInChildren<Renderer>();
        lJ.rival = lB;
        lB.rival = lJ;
        jugadorGo.AddComponent<ControlJugadorPelea>();
        botGo.AddComponent<BotPelea>();

        // Cámara de pelea
        var camGo = new GameObject("Camara");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0, 4.5f, -8.5f);
        camGo.transform.LookAt(Vector3.up);
        var camPelea = camGo.AddComponent<CamaraPelea>();
        camPelea.a = jugadorGo.transform;
        camPelea.b = botGo.transform;

        // Movimiento del jugador (WASD relativo a la cámara de pelea)
        var mov = jugadorGo.AddComponent<MovimientoTerceraPersona>();
        mov.animator = animJugador;
        mov.camara = camGo.transform;

        // Control de la pelea
        var juego = new GameObject("JuegoPelea").AddComponent<JuegoPelea>();
        juego.jugador = lJ;
        juego.bot = lB;
        juego.movimientoJugador = mov;

        EditorSceneManager.SaveScene(escena, EscenaPelea);
        ConfigurarPersonaje.ActualizarBuild();
        AssetDatabase.SaveAssets();
        Debug.Log("Escena de pelea creada: " + EscenaPelea);
    }
}
