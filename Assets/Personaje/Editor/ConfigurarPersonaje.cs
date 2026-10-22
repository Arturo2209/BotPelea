using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú  Personaje > Configurar escena
/// 1) Si pusiste tus animaciones de Mixamo en Assets/Personaje/Mixamo (Idle, Walking, Running, Jumping),
///    las configura como Humanoid y las usa. Si no, usa las animaciones incluidas.
/// 2) Crea el Animator Controller (Blend Tree Idle/Walk/Run + Jump).
/// 3) Arma la escena de prueba con el personaje, la cámara y obstáculos para saltar.
/// </summary>
public static class ConfigurarPersonaje
{
    public const string Raiz = "Assets/Personaje";
    const string Mixamo = Raiz + "/Mixamo";
    const string Generado = Raiz + "/Generado";
    public const string EscenaMovimiento = Generado + "/Escena_Movimiento.unity";

    public class Clips
    {
        public AnimationClip idle, caminar, correr, salto;
        public string origen;
    }

    [MenuItem("Personaje/Configurar escena", priority = 0)]
    public static void Menu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Configurar();
    }

    public static void Batch()
    {
        try { Configurar(); EditorApplication.Exit(0); }
        catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static void Configurar()
    {
        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        AssetDatabase.DeleteAsset(Generado);
        AssetDatabase.CreateFolder(Raiz, "Generado");

        var clips = ObtenerClips();
        var ctrl = CrearController(Generado + "/Personaje.controller", clips);

        CrearEscena(ctrl);
        EditorSceneManager.SaveScene(escena, EscenaMovimiento);
        ActualizarBuild();
        AssetDatabase.SaveAssets();
        Debug.Log($"Personaje configurado con animaciones {clips.origen}. Escena: {EscenaMovimiento}");
    }

    /// <summary>Pone en Build Settings las escenas que existan (movimiento y laberinto).</summary>
    public static void ActualizarBuild()
    {
        var lista = new List<EditorBuildSettingsScene>();
        foreach (var ruta in new[] { EscenaMovimiento, LaberintoEditor.EscenaLaberinto })
            if (File.Exists(ruta)) lista.Add(new EditorBuildSettingsScene(ruta, true));
        EditorBuildSettings.scenes = lista.ToArray();
    }

    // ---------- Animaciones ----------

    public static Clips ObtenerClips()
    {
        if (File.Exists(Mixamo + "/Idle.fbx") && File.Exists(Mixamo + "/Walking.fbx") &&
            File.Exists(Mixamo + "/Running.fbx") && File.Exists(Mixamo + "/Jumping.fbx"))
            return new Clips
            {
                idle = ClipMixamo(Mixamo + "/Idle.fbx", true),
                caminar = ClipMixamo(Mixamo + "/Walking.fbx", true),
                correr = ClipMixamo(Mixamo + "/Running.fbx", true),
                salto = ClipMixamo(Mixamo + "/Jumping.fbx", false),
                origen = "Mixamo",
            };
        return new Clips
        {
            idle = Clip(Raiz + "/Animaciones/Idle.fbx"),
            caminar = Clip(Raiz + "/Animaciones/Walk.fbx"),
            correr = Clip(Raiz + "/Animaciones/Run.fbx"),
            salto = Clip(Raiz + "/Animaciones/JumpInAir.fbx"),
            origen = "incluidas",
        };
    }

    static AnimationClip Clip(string ruta) =>
        AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));

    /// <summary>Configura un FBX de Mixamo como Humanoid y devuelve su animación.</summary>
    static AnimationClip ClipMixamo(string ruta, bool bucle)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(ruta);
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.SaveAndReimport();

        var lista = imp.defaultClipAnimations;
        foreach (var c in lista)
        {
            c.loopTime = bucle;
            c.lockRootRotation = true;      // "Bake Into Pose": que no gire solo
            c.lockRootHeightY = true;       // ni suba/baje
            c.lockRootPositionXZ = true;    // ni avance (ya es "In Place")
            c.keepOriginalOrientation = true;
            c.keepOriginalPositionY = true;
            c.keepOriginalPositionXZ = true;
        }
        imp.clipAnimations = lista;
        imp.SaveAndReimport();
        return Clip(ruta);
    }

    // ---------- Animator Controller ----------

    public static AnimatorController CrearController(string ruta, Clips clips)
    {
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        ctrl.parameters = ctrl.parameters.Select(p => { if (p.name == "IsGrounded") p.defaultBool = true; return p; }).ToArray();
        var sm = ctrl.layers[0].stateMachine;

        // Blend Tree 1D por velocidad (m/s): 0 = Idle, 2 = Walk, 5.5 = Run
        var locomocion = ctrl.CreateBlendTreeInController("Locomocion", out var arbol, 0);
        arbol.blendType = BlendTreeType.Simple1D;
        arbol.blendParameter = "Speed";
        arbol.useAutomaticThresholds = false;
        arbol.AddChild(clips.idle, 0f);
        arbol.AddChild(clips.caminar, 2f);
        arbol.AddChild(clips.correr, 5.5f);
        sm.defaultState = locomocion;

        var estadoSalto = sm.AddState("Jump");
        estadoSalto.motion = clips.salto;

        // Locomoción -> Salto: apenas deja el suelo (sin esperar a que termine la animación)
        var aSalto = locomocion.AddTransition(estadoSalto);
        aSalto.hasExitTime = false;
        aSalto.duration = 0.1f;
        aSalto.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");

        // Salto -> Locomoción: apenas toca el suelo
        var aSuelo = estadoSalto.AddTransition(locomocion);
        aSuelo.hasExitTime = false;
        aSuelo.duration = 0.15f;
        aSuelo.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        AssetDatabase.SaveAssets();
        return ctrl;
    }

    // ---------- Utilidades compartidas ----------

    public static Material Mat(string carpeta, string nombre, Color color, Texture2D tex = null, Color? emision = null)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh) { color = color };
        if (tex != null) { m.mainTexture = tex; m.mainTextureScale = new Vector2(30, 30); }
        if (emision.HasValue)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emision.Value);
        }
        AssetDatabase.CreateAsset(m, $"{carpeta}/{nombre}.mat");
        return m;
    }

    /// <summary>Crea el personaje (CharacterController + modelo con Animator) y lo devuelve.</summary>
    public static GameObject CrearPersonaje(AnimatorController ctrl, Material material, out Animator animator)
    {
        var jugador = new GameObject("Jugador");
        jugador.layer = 2; // Ignore Raycast: la cámara no choca con él
        var cc = jugador.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0, 0.93f, 0);
        cc.stepOffset = 0.3f;

        var modeloAsset = AssetDatabase.LoadAssetAtPath<GameObject>(Raiz + "/Modelo/Armature.fbx");
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(modeloAsset, jugador.transform);
        modelo.name = "Modelo";
        foreach (var r in modelo.GetComponentsInChildren<Renderer>())
            r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
        animator = modelo.GetComponent<Animator>();
        if (animator == null) animator = modelo.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;
        animator.applyRootMotion = false;
        if (animator.avatar == null)
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(Raiz + "/Modelo/Armature.fbx").OfType<Avatar>().FirstOrDefault();
        return jugador;
    }

    public static void Luz()
    {
        var sol = new GameObject("Sol").AddComponent<Light>();
        sol.type = LightType.Directional;
        sol.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        sol.intensity = 1.2f;
        sol.shadows = LightShadows.Soft;
        RenderSettings.sun = sol;
        RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
    }

    // ---------- Escena de movimiento ----------

    static Texture2D Cuadricula()
    {
        const int s = 128;
        var t = new Texture2D(s, s);
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool linea = x < 3 || y < 3;
                bool cuadro = (x < s / 2) ^ (y < s / 2);
                t.SetPixel(x, y, linea ? new Color(0.35f, 0.4f, 0.45f) : cuadro ? new Color(0.62f, 0.68f, 0.72f) : new Color(0.7f, 0.75f, 0.8f));
            }
        t.Apply();
        string ruta = Generado + "/Cuadricula.png";
        File.WriteAllBytes(ruta, t.EncodeToPNG());
        AssetDatabase.ImportAsset(ruta);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    static GameObject Caja(string nombre, Vector3 pos, Vector3 esc, Material m, Vector3 rot = default)
    {
        var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nombre;
        g.transform.position = pos;
        g.transform.eulerAngles = rot;
        g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static void CrearEscena(AnimatorController ctrl)
    {
        Luz();

        // Suelo y obstáculos para practicar
        var mSuelo = Mat(Generado, "Suelo", Color.white, Cuadricula());
        var mBloque = Mat(Generado, "Bloque", new Color(0.95f, 0.6f, 0.25f));
        var mBloque2 = Mat(Generado, "Bloque2", new Color(0.3f, 0.6f, 0.9f));
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Plane);
        suelo.name = "Suelo";
        suelo.transform.localScale = new Vector3(6, 1, 6);
        suelo.GetComponent<Renderer>().sharedMaterial = mSuelo;

        var obst = new GameObject("Obstaculos").transform;
        for (int i = 0; i < 5; i++) // escalones para saltar
            Caja("Escalon" + i, new Vector3(6 + i * 2.2f, 0.25f + i * 0.45f, 6), new Vector3(2, 0.5f + i * 0.9f, 2), i % 2 == 0 ? mBloque : mBloque2).transform.SetParent(obst);
        Caja("Rampa", new Vector3(-6, 0.9f, 6), new Vector3(3, 0.3f, 7), mBloque2, new Vector3(-15, 0, 0)).transform.SetParent(obst);
        Caja("Plataforma", new Vector3(-6, 1.8f, 11.2f), new Vector3(4, 0.3f, 4), mBloque).transform.SetParent(obst);
        for (int i = 0; i < 6; i++)
            Caja("Caja" + i, new Vector3(-10 + i * 4f, 0.4f, -7), Vector3.one * 0.8f, i % 2 == 0 ? mBloque : mBloque2, new Vector3(0, i * 25, 0)).transform.SetParent(obst);
        Caja("Muro", new Vector3(0, 1.5f, 14), new Vector3(16, 3, 0.5f), mBloque2).transform.SetParent(obst);

        // Personaje y cámara
        var jugador = CrearPersonaje(ctrl, Mat(Generado, "Personaje", new Color(0.85f, 0.85f, 0.9f)), out var animator);
        var camGo = new GameObject("Camara");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0, 2.5f, -4.5f);
        camGo.AddComponent<CamaraTerceraPersona>().objetivo = jugador.transform;

        var mov = jugador.AddComponent<MovimientoTerceraPersona>();
        mov.animator = animator;
        mov.camara = camGo.transform;
    }
}
