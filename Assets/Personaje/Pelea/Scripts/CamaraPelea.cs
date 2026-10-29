using UnityEngine;

/// <summary>Cámara de pelea: mira el punto medio entre los dos luchadores y se aleja si se separan.</summary>
public class CamaraPelea : MonoBehaviour
{
    public Transform a, b;

    void LateUpdate()
    {
        Vector3 medio = (a.position + b.position) / 2f;
        float separacion = Vector3.Distance(a.position, b.position);
        Vector3 deseada = medio + new Vector3(0f, 2.8f + separacion * 0.3f, -(5.5f + separacion * 0.7f));
        transform.position = Vector3.Lerp(transform.position, deseada, 4f * Time.deltaTime);
        var rot = Quaternion.LookRotation(medio + Vector3.up * 1.1f - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, 6f * Time.deltaTime);
    }
}
