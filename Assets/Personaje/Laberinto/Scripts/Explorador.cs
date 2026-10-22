using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Hace que el personaje resuelva el laberinto solo.
/// Modo EXPLORAR: no conoce el mapa; avanza por caminos nuevos (prefiriendo los que se acercan a la salida)
/// y cuando llega a un callejón sin salida retrocede (búsqueda en profundidad).
/// Modo RUTA DIRECTA: calcula la ruta más corta y la sigue.
/// Teclas: R = laberinto nuevo, E = cambiar de modo, C = cambiar de cámara.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Explorador : MonoBehaviour
{
    public static bool explorar = true; // se mantiene al reiniciar

    public Laberinto laberinto;
    public Animator animator;
    public float velocidad = 3.2f;
    public Material matRastro, matCallejon, matRuta;

    CharacterController cc;
    Vector2Int actual, siguiente;
    readonly Stack<Vector2Int> pila = new Stack<Vector2Int>();
    readonly HashSet<Vector2Int> visitadas = new HashSet<Vector2Int>();
    List<Vector2Int> ruta;
    int indiceRuta;
    bool terminado;
    float inicio = -1f, tiempo;
    int pasos, retrocesos;
    float velY;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        actual = laberinto.Inicio;
        cc.enabled = false;
        transform.position = laberinto.Centro(actual);
        transform.rotation = Quaternion.LookRotation(Vector3.forward);
        cc.enabled = true;
        visitadas.Add(actual);
        pila.Push(actual);
        if (!explorar) ruta = laberinto.RutaMasCorta(laberinto.Inicio, laberinto.Afuera);
        siguiente = actual;
        Invoke(nameof(Empezar), 1.5f); // una pausa para ver el laberinto antes de arrancar
    }

    void Empezar()
    {
        inicio = Time.time;
        Decidir();
    }

    void Update()
    {
        if (Entrada.Reiniciar()) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        if (Entrada.CambiarModo()) { explorar = !explorar; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }

        float vel = 0f;
        if (inicio >= 0 && !terminado)
        {
            tiempo = Time.time - inicio;
            Vector3 hacia = laberinto.Centro(siguiente) - transform.position;
            hacia.y = 0;
            if (hacia.magnitude < 0.15f)
            {
                actual = siguiente;
                Decidir();
            }
            else
            {
                vel = velocidad;
                var dir = hacia.normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
                velY = cc.isGrounded ? -2f : velY - 20f * Time.deltaTime;
                cc.Move((dir * Mathf.Min(velocidad, hacia.magnitude / Time.deltaTime) + Vector3.up * velY) * Time.deltaTime);
            }
        }

        if (animator != null)
        {
            animator.SetFloat("Speed", vel, 0.1f, Time.deltaTime);
            animator.SetBool("IsGrounded", true);
        }
    }

    /// <summary>Elige a qué celda ir después.</summary>
    void Decidir()
    {
        pasos++;
        if (actual == laberinto.Afuera)
        {
            terminado = true;
            DibujarRuta();
            return;
        }

        if (!explorar)
        {
            Rastro(actual, matRastro);
            indiceRuta = Mathf.Min(indiceRuta + 1, ruta.Count - 1);
            siguiente = ruta[indiceRuta];
            return;
        }

        // ¿Hay caminos nuevos? Elegir el que más se acerque a la salida
        var opciones = laberinto.Vecinos(actual).FindAll(n => !visitadas.Contains(n));
        if (opciones.Count > 0)
        {
            Rastro(actual, matRastro);
            opciones.Sort((a, b) => Distancia(a).CompareTo(Distancia(b)));
            siguiente = opciones[0];
            visitadas.Add(siguiente);
            pila.Push(siguiente);
        }
        else
        {
            // Callejón sin salida: marcar en rojo y volver por donde vino
            Rastro(actual, matCallejon);
            retrocesos++;
            pila.Pop();
            siguiente = pila.Peek();
        }
    }

    float Distancia(Vector2Int c) => Vector2Int.Distance(c, laberinto.Afuera);

    readonly Dictionary<Vector2Int, Renderer> marcas = new Dictionary<Vector2Int, Renderer>();

    void Rastro(Vector2Int c, Material mat)
    {
        if (c == laberinto.Inicio) return;
        if (!marcas.TryGetValue(c, out var r))
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(m.GetComponent<Collider>());
            m.transform.position = laberinto.Centro(c) + Vector3.up * 0.02f;
            m.transform.localScale = new Vector3(laberinto.celda * 0.3f, 0.02f, laberinto.celda * 0.3f);
            r = m.GetComponent<Renderer>();
            marcas[c] = r;
        }
        r.sharedMaterial = mat;
    }

    void DibujarRuta()
    {
        var mejor = laberinto.RutaMasCorta(laberinto.Inicio, laberinto.Afuera);
        var linea = new GameObject("RutaMasCorta").AddComponent<LineRenderer>();
        linea.positionCount = mejor.Count;
        for (int i = 0; i < mejor.Count; i++) linea.SetPosition(i, laberinto.Centro(mejor[i]) + Vector3.up * 0.25f);
        linea.widthMultiplier = 0.35f;
        linea.sharedMaterial = matRuta;
        linea.numCornerVertices = 4;
        rutaMasCorta = mejor.Count - 1;
    }

    int rutaMasCorta;

    void OnGUI()
    {
        float s = Screen.height / 1080f;
        var estilo = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(28 * s), fontStyle = FontStyle.Bold };
        var chico = new GUIStyle(estilo) { fontSize = Mathf.RoundToInt(20 * s), fontStyle = FontStyle.Normal };
        GUI.color = new Color(0, 0, 0, 0.5f);
        GUI.DrawTexture(new Rect(20 * s, 20 * s, 560 * s, 170 * s), Texture2D.whiteTexture);
        GUI.color = Color.white;

        string modo = explorar ? "EXPLORANDO (sin conocer el mapa)" : "RUTA DIRECTA (conoce el mapa)";
        string estado = inicio < 0 ? "Preparándose..." : terminado ? "¡SALIDA ENCONTRADA!" : modo;
        estilo.normal.textColor = terminado ? new Color(0.4f, 1f, 0.5f) : Color.white;
        GUI.Label(new Rect(35 * s, 28 * s, 540 * s, 40 * s), estado, estilo);
        GUI.Label(new Rect(35 * s, 70 * s, 540 * s, 30 * s), $"Tiempo: {tiempo:0.0} s    Pasos: {pasos}    Retrocesos: {retrocesos}", chico);
        if (terminado) GUI.Label(new Rect(35 * s, 100 * s, 540 * s, 30 * s), $"Ruta más corta: {rutaMasCorta} celdas (línea verde)", chico);
        GUI.Label(new Rect(35 * s, 140 * s, 540 * s, 30 * s), "R = laberinto nuevo    E = cambiar modo    C = cámara", chico);
    }
}
