using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú  Personaje > Crear escena laberinto
/// Crea una escena donde el personaje resuelve solo un laberinto aleatorio.
/// </summary>
public static class LaberintoEditor
{
    const string Carpeta = ConfigurarPersonaje.Raiz + "/Laberinto/Generado";
    public const string EscenaLaberinto = Carpeta + "/Escena_Laberinto.unity";

    [MenuItem("Personaje/Crear escena laberinto", priority = 1)]
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

    static void Crear()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(Carpeta);
        AssetDatabase.CreateFolder(ConfigurarPersonaje.Raiz + "/Laberinto", "Generado");

        // Animaciones y controller propios de esta escena
        var clips = ConfigurarPersonaje.ObtenerClips();
        var ctrl = ConfigurarPersonaje.CrearController(Carpeta + "/PersonajeLaberinto.controller", clips);

        // Materiales
        Material M(string n, Color c, Color? e = null) => ConfigurarPersonaje.Mat(Carpeta, n, c, null, e);
        var mMuro = M("Muro", new Color(0.55f, 0.6f, 0.75f));
        var mSuelo = M("Suelo", new Color(0.82f, 0.8f, 0.72f));
        var mInicio = M("Inicio", new Color(0.3f, 0.6f, 1f), new Color(0.2f, 0.5f, 1f) * 1.5f);
        var mSalida = M("Salida", new Color(0.3f, 1f, 0.4f), new Color(0.2f, 1f, 0.3f) * 1.5f);
        var mRastro = M("Rastro", new Color(1f, 0.85f, 0.3f), new Color(0.6f, 0.45f, 0.05f));
        var mCallejon = M("Callejon", new Color(0.9f, 0.25f, 0.2f), new Color(0.5f, 0.05f, 0.03f));
        var mRuta = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.2f, 1f, 0.35f) };
        AssetDatabase.CreateAsset(mRuta, Carpeta + "/RutaMasCorta.mat");

        ConfigurarPersonaje.Luz();

        // Laberinto (se genera distinto en cada Play)
        var lab = new GameObject("Laberinto").AddComponent<Laberinto>();
        lab.matMuro = mMuro;
        lab.matSuelo = mSuelo;
        lab.matInicio = mInicio;
        lab.matSalida = mSalida;

        // Personaje que lo resuelve solo
        var jugador = ConfigurarPersonaje.CrearPersonaje(ctrl, M("Personaje", new Color(0.85f, 0.85f, 0.9f)), out var animator);
        jugador.transform.position = new Vector3(lab.celda / 2, 0, lab.celda / 2);
        var exp = jugador.AddComponent<Explorador>();
        exp.laberinto = lab;
        exp.animator = animator;
        exp.matRastro = mRastro;
        exp.matCallejon = mCallejon;
        exp.matRuta = mRuta;

        // Cámara
        var camGo = new GameObject("Camara");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = jugador.transform.position + new Vector3(0, 11, -7);
        camGo.transform.LookAt(jugador.transform);
        var cam = camGo.AddComponent<CamaraLaberinto>();
        cam.objetivo = jugador.transform;
        cam.laberinto = lab;

        EditorSceneManager.SaveScene(escena, EscenaLaberinto);
        ConfigurarPersonaje.ActualizarBuild();
        AssetDatabase.SaveAssets();
        Debug.Log("Escena del laberinto creada: " + EscenaLaberinto);
    }
}
