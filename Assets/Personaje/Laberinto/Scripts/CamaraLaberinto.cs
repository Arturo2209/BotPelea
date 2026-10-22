using UnityEngine;

/// <summary>Cámara del laberinto: sigue al personaje desde arriba o muestra el laberinto completo (tecla C).</summary>
public class CamaraLaberinto : MonoBehaviour
{
    public Transform objetivo;
    public Laberinto laberinto;
    public bool vistaCompleta;

    void LateUpdate()
    {
        if (Entrada.CambiarCamara()) vistaCompleta = !vistaCompleta;

        Vector3 pos;
        Vector3 mirar;
        if (vistaCompleta)
        {
            float w = laberinto.ancho * laberinto.celda, h = laberinto.alto * laberinto.celda;
            mirar = laberinto.transform.position + new Vector3(w / 2, 0, h / 2);
            pos = mirar + new Vector3(0, Mathf.Max(w, h) * 1.15f, -h * 0.35f);
        }
        else
        {
            mirar = objetivo.position;
            pos = objetivo.position + new Vector3(0, 11f, -7f);
        }
        transform.position = Vector3.Lerp(transform.position, pos, 4f * Time.deltaTime);
        var rot = Quaternion.LookRotation(mirar - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, 6f * Time.deltaTime);
    }
}
