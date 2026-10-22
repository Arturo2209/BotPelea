using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera un laberinto aleatorio (algoritmo "backtracking recursivo") al empezar y lo construye con cubos.
/// La entrada es la celda (0,0) y la salida está en la pared norte de la última celda.
/// </summary>
public class Laberinto : MonoBehaviour
{
    public int ancho = 12, alto = 12;
    public float celda = 3f, altoMuro = 2.4f, grosor = 0.35f;
    public Material matMuro, matSuelo, matInicio, matSalida;

    // muroN[x,y]: pared al norte de la celda; muroE[x,y]: pared al este
    bool[,] muroN, muroE;

    public Vector2Int Inicio => new Vector2Int(0, 0);
    public Vector2Int Salida => new Vector2Int(ancho - 1, alto - 1);
    public Vector2Int Afuera => new Vector2Int(ancho - 1, alto); // la celda "de fuera" pasando la salida

    static readonly Vector2Int[] Dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

    void Awake()
    {
        Generar();
        Construir();
    }

    // ---------------- Generación ----------------

    void Generar()
    {
        muroN = new bool[ancho, alto];
        muroE = new bool[ancho, alto];
        for (int x = 0; x < ancho; x++)
            for (int y = 0; y < alto; y++) { muroN[x, y] = true; muroE[x, y] = true; }

        // Camino aleatorio que retrocede cuando se queda sin salida (crea un laberinto perfecto)
        var visitado = new bool[ancho, alto];
        var pila = new Stack<Vector2Int>();
        pila.Push(Inicio);
        visitado[0, 0] = true;
        while (pila.Count > 0)
        {
            var c = pila.Peek();
            var opciones = new List<Vector2Int>();
            foreach (var d in Dirs)
            {
                var n = c + d;
                if (Dentro(n) && !visitado[n.x, n.y]) opciones.Add(n);
            }
            if (opciones.Count == 0) { pila.Pop(); continue; }
            var sig = opciones[Random.Range(0, opciones.Count)];
            QuitarMuro(c, sig);
            visitado[sig.x, sig.y] = true;
            pila.Push(sig);
        }
        muroN[Salida.x, Salida.y] = false; // abrir la salida
    }

    bool Dentro(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < ancho && c.y < alto;

    void QuitarMuro(Vector2Int a, Vector2Int b)
    {
        if (b.y > a.y) muroN[a.x, a.y] = false;
        else if (b.y < a.y) muroN[b.x, b.y] = false;
        else if (b.x > a.x) muroE[a.x, a.y] = false;
        else muroE[b.x, b.y] = false;
    }

    /// <summary>¿Se puede pasar de la celda c a su vecina en dirección d?</summary>
    public bool Abierto(Vector2Int c, Vector2Int d)
    {
        var n = c + d;
        if (c == Salida && n == Afuera) return true;
        if (!Dentro(c) || !Dentro(n)) return false;
        if (d == Vector2Int.up) return !muroN[c.x, c.y];
        if (d == Vector2Int.down) return !muroN[n.x, n.y];
        if (d == Vector2Int.right) return !muroE[c.x, c.y];
        return !muroE[n.x, n.y];
    }

    public List<Vector2Int> Vecinos(Vector2Int c)
    {
        var lista = new List<Vector2Int>();
        foreach (var d in Dirs)
            if (Abierto(c, d)) lista.Add(c + d);
        return lista;
    }

    public Vector3 Centro(Vector2Int c) => transform.position + new Vector3((c.x + 0.5f) * celda, 0f, (c.y + 0.5f) * celda);

    /// <summary>Ruta más corta (búsqueda en anchura) desde a hasta b.</summary>
    public List<Vector2Int> RutaMasCorta(Vector2Int a, Vector2Int b)
    {
        var previo = new Dictionary<Vector2Int, Vector2Int>();
        var cola = new Queue<Vector2Int>();
        cola.Enqueue(a);
        previo[a] = a;
        while (cola.Count > 0)
        {
            var c = cola.Dequeue();
            if (c == b) break;
            foreach (var n in Vecinos(c))
                if (!previo.ContainsKey(n)) { previo[n] = c; cola.Enqueue(n); }
        }
        var ruta = new List<Vector2Int>();
        if (!previo.ContainsKey(b)) return ruta;
        for (var c = b; c != a; c = previo[c]) ruta.Add(c);
        ruta.Add(a);
        ruta.Reverse();
        return ruta;
    }

    // ---------------- Construcción ----------------

    void Construir()
    {
        float W = ancho * celda, H = alto * celda;
        var suelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        suelo.name = "Suelo";
        suelo.transform.SetParent(transform, false);
        suelo.transform.localPosition = new Vector3(W / 2, -0.05f, H / 2 + celda);
        suelo.transform.localScale = new Vector3(W + 8, 0.1f, H + 2 * celda + 8);
        suelo.GetComponent<Renderer>().sharedMaterial = matSuelo;

        var muros = new GameObject("Muros").transform;
        muros.SetParent(transform, false);
        for (int x = 0; x < ancho; x++)
        {
            Muro(muros, new Vector3((x + 0.5f) * celda, 0, 0), true); // borde sur
            for (int y = 0; y < alto; y++)
            {
                if (muroN[x, y]) Muro(muros, new Vector3((x + 0.5f) * celda, 0, (y + 1) * celda), true);
                if (muroE[x, y]) Muro(muros, new Vector3((x + 1) * celda, 0, (y + 0.5f) * celda), false);
            }
        }
        for (int y = 0; y < alto; y++) Muro(muros, new Vector3(0, 0, (y + 0.5f) * celda), false); // borde oeste

        // Columnas en las esquinas para que se vea más prolijo
        for (int x = 0; x <= ancho; x++)
            for (int y = 0; y <= alto; y++)
            {
                var col = GameObject.CreatePrimitive(PrimitiveType.Cube);
                col.transform.SetParent(muros, false);
                col.transform.localPosition = new Vector3(x * celda, altoMuro / 2 + 0.05f, y * celda);
                col.transform.localScale = new Vector3(grosor * 1.3f, altoMuro + 0.1f, grosor * 1.3f);
                col.GetComponent<Renderer>().sharedMaterial = matMuro;
            }

        Placa("Inicio", Inicio, matInicio, new Color(0.3f, 0.6f, 1f));
        Placa("Salida", Afuera, matSalida, new Color(0.3f, 1f, 0.4f));
    }

    void Muro(Transform padre, Vector3 pos, bool horizontal)
    {
        var m = GameObject.CreatePrimitive(PrimitiveType.Cube);
        m.name = "Muro";
        m.transform.SetParent(padre, false);
        m.transform.localPosition = pos + Vector3.up * altoMuro / 2;
        m.transform.localScale = horizontal ? new Vector3(celda, altoMuro, grosor) : new Vector3(grosor, altoMuro, celda);
        m.GetComponent<Renderer>().sharedMaterial = matMuro;
    }

    void Placa(string nombre, Vector2Int c, Material mat, Color luz)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p.name = nombre;
        Destroy(p.GetComponent<Collider>());
        p.transform.position = Centro(c) + Vector3.up * 0.03f;
        p.transform.localScale = new Vector3(celda * 0.7f, 0.03f, celda * 0.7f);
        p.GetComponent<Renderer>().sharedMaterial = mat;
        var l = new GameObject("Luz" + nombre).AddComponent<Light>();
        l.type = LightType.Point;
        l.color = luz;
        l.range = 6f;
        l.intensity = 3f;
        l.transform.position = Centro(c) + Vector3.up * 1.5f;
    }
}
