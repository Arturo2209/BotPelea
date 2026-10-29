using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controla la pelea: cuenta regresiva, barras de vida, quién gana y reinicio (R).
/// También crea los sonidos (por código).
/// </summary>
public class JuegoPelea : MonoBehaviour
{
    public static JuegoPelea Instancia;
    public enum Estado { Cuenta, Peleando, Fin }
    public static bool Peleando => Instancia != null && Instancia.estado == Estado.Peleando;

    public Luchador jugador, bot;
    public MonoBehaviour movimientoJugador; // MovimientoTerceraPersona

    public Estado estado = Estado.Cuenta;
    float inicio, finEn;
    Luchador perdedor;

    [HideInInspector] public AudioClip sGolpe, sGolpeFuerte, sBloqueo, sAire, sCampana;
    AudioSource fuente;

    void Awake()
    {
        Instancia = this;
        CrearSonidos();
    }

    void Start()
    {
        inicio = Time.time;
        movimientoJugador.enabled = false;
    }

    void Update()
    {
        if (estado == Estado.Cuenta && Time.time - inicio > 3f)
        {
            estado = Estado.Peleando;
            movimientoJugador.enabled = true;
            Sonar(sCampana);
        }
        if (Entrada.Reiniciar()) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Terminar(Luchador quienPerdio)
    {
        if (estado == Estado.Fin) return;
        estado = Estado.Fin;
        perdedor = quienPerdio;
        finEn = Time.time;
        movimientoJugador.enabled = false; // se acabó: el jugador ya no se mueve
        jugador.animator.SetFloat("Speed", 0f);
        Sonar(sCampana);
    }

    public void Sonar(AudioClip clip, float volumen = 1f)
    {
        fuente.pitch = Random.Range(0.9f, 1.1f);
        fuente.PlayOneShot(clip, volumen);
    }

    // ---------------- Interfaz ----------------

    void OnGUI()
    {
        float s = Screen.height / 1080f, W = Screen.width;
        Barra(new Rect(40 * s, 30 * s, 700 * s, 34 * s), jugador, false, new Color(0.3f, 0.6f, 1f));
        Barra(new Rect(W - 740 * s, 30 * s, 700 * s, 34 * s), bot, true, new Color(1f, 0.35f, 0.3f));

        var grande = Estilo(120 * s, TextAnchor.MiddleCenter);
        var chico = Estilo(24 * s, TextAnchor.MiddleCenter, false);
        if (estado == Estado.Cuenta)
        {
            int n = 3 - Mathf.FloorToInt(Time.time - inicio);
            Texto(new Rect(0, Screen.height * 0.3f, W, 150 * s), n > 0 ? n.ToString() : "", grande, Color.white);
        }
        else if (estado == Estado.Peleando && Time.time - inicio < 4f)
        {
            Texto(new Rect(0, Screen.height * 0.3f, W, 150 * s), "¡PELEA!", grande, new Color(1f, 0.8f, 0.2f));
        }
        else if (estado == Estado.Fin)
        {
            bool gane = perdedor == bot;
            string causa = perdedor.transform.position.y < -1f ? "¡FUERA DEL RING!" : "K.O.";
            Texto(new Rect(0, Screen.height * 0.22f, W, 80 * s), causa, Estilo(60 * s, TextAnchor.MiddleCenter), Color.white);
            Texto(new Rect(0, Screen.height * 0.32f, W, 150 * s), gane ? "¡GANASTE!" : "PERDISTE", grande, gane ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.3f));
            if (Time.time - finEn > 1f) Texto(new Rect(0, Screen.height * 0.52f, W, 40 * s), "Pulsa R para la revancha", chico, Color.white);
        }

        Texto(new Rect(0, Screen.height - 50 * s, W, 40 * s),
            "WASD mover · Shift correr · Espacio saltar · Clic izq / J golpe · Clic der / K golpe fuerte · F bloquear · R reiniciar", chico, new Color(1, 1, 1, 0.8f));
    }

    void Barra(Rect r, Luchador l, bool derecha, Color color)
    {
        GUI.color = new Color(0, 0, 0, 0.6f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        float v = l.vida / l.vidaMax;
        var relleno = new Rect(r.x + 4, r.y + 4, (r.width - 8) * v, r.height - 8);
        if (derecha) relleno.x = r.xMax - 4 - relleno.width;
        GUI.color = Color.Lerp(new Color(0.9f, 0.15f, 0.1f), color, v);
        GUI.DrawTexture(relleno, Texture2D.whiteTexture);
        GUI.color = Color.white;
        float s = Screen.height / 1080f;
        Texto(new Rect(r.x, r.yMax + 4 * s, r.width, 34 * s), l.nombre, Estilo(26 * s, derecha ? TextAnchor.UpperRight : TextAnchor.UpperLeft), Color.white);
    }

    static GUIStyle Estilo(float tam, TextAnchor ancla, bool negrita = true)
    {
        var e = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(8, Mathf.RoundToInt(tam)), alignment = ancla, fontStyle = negrita ? FontStyle.Bold : FontStyle.Normal };
        return e;
    }

    static void Texto(Rect r, string t, GUIStyle e, Color c)
    {
        e.normal.textColor = new Color(0, 0, 0, 0.7f * c.a);
        GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), t, e);
        e.normal.textColor = c;
        GUI.Label(r, t, e);
    }

    // ---------------- Sonidos ----------------

    const int Hz = 44100;

    void CrearSonidos()
    {
        fuente = gameObject.AddComponent<AudioSource>();
        var azar = new System.Random(3);
        float Ruido() => (float)azar.NextDouble() * 2f - 1f;

        sGolpe = Clip("Golpe", 0.15f, (t, y) => (y * 0.7f + Mathf.Sin(2 * Mathf.PI * 120 * t)) * Mathf.Exp(-t * 30), 0.15f, Ruido);
        sGolpeFuerte = Clip("GolpeFuerte", 0.3f, (t, y) => (y + Mathf.Sin(2 * Mathf.PI * 80 * t)) * Mathf.Exp(-t * 15), 0.1f, Ruido);
        sBloqueo = Clip("Bloqueo", 0.12f, (t, y) => (y * 0.5f + Mathf.Sin(2 * Mathf.PI * 600 * t) * 0.5f) * Mathf.Exp(-t * 40), 0.5f, Ruido);
        sAire = Clip("Aire", 0.18f, (t, y) => y * Mathf.Sin(Mathf.PI * t / 0.18f) * 0.4f, 0.6f, Ruido);
        sCampana = Clip("Campana", 1.5f, (t, y) =>
            (Mathf.Sin(2 * Mathf.PI * 880 * t) + 0.6f * Mathf.Sin(2 * Mathf.PI * 1320 * t) + 0.3f * Mathf.Sin(2 * Mathf.PI * 2210 * t)) * Mathf.Exp(-t * 3) * 0.35f, 1f, Ruido);
    }

    /// <summary>Crea un sonido: "f" recibe el tiempo y un ruido ya filtrado (más grave cuanto menor es "filtro").</summary>
    static AudioClip Clip(string nombre, float dur, System.Func<float, float, float> f, float filtro, System.Func<float> ruido)
    {
        var m = new float[(int)(dur * Hz)];
        float y = 0;
        for (int i = 0; i < m.Length; i++)
        {
            y += filtro * (ruido() - y);
            m[i] = Mathf.Clamp(f(i / (float)Hz, y), -1f, 1f);
        }
        var c = AudioClip.Create(nombre, m.Length, 1, Hz, false);
        c.SetData(m, 0);
        return c;
    }
}
