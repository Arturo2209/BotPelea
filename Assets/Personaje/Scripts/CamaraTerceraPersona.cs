using UnityEngine;

/// <summary>Cámara que orbita alrededor del personaje con el mouse. Rueda = acercar/alejar.</summary>
public class CamaraTerceraPersona : MonoBehaviour
{
    public Transform objetivo;
    public float distancia = 4.5f;
    public float altura = 1.5f;
    public float sensibilidad = 2.5f;

    float yaw, pitch = 15f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (objetivo != null) yaw = objetivo.eulerAngles.y;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;
        Vector2 m = Entrada.Mouse();
        yaw += m.x * sensibilidad;
        pitch = Mathf.Clamp(pitch - m.y * sensibilidad, -20f, 70f);
        distancia = Mathf.Clamp(distancia - Entrada.Rueda(), 2f, 9f);

        Vector3 foco = objetivo.position + Vector3.up * altura;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 deseada = foco - rot * Vector3.forward * distancia;

        // Si hay una pared entre el personaje y la cámara, acercarla
        // (el personaje está en la capa "Ignore Raycast", así no se choca consigo mismo)
        if (Physics.SphereCast(foco, 0.2f, deseada - foco, out var hit, distancia, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            deseada = foco + (deseada - foco).normalized * (hit.distance - 0.1f);

        transform.SetPositionAndRotation(deseada, rot);
    }
}
