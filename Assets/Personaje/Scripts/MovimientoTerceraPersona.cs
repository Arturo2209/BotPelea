using UnityEngine;

/// <summary>
/// Movimiento básico en tercera persona con Character Controller:
/// WASD / flechas para moverse (relativo a la cámara), Shift para correr,
/// Espacio para saltar, gravedad y rotación hacia donde camina.
/// Le pasa al Animator: "Speed" (float, m/s) e "IsGrounded" (bool).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class MovimientoTerceraPersona : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public Transform camara;

    [Header("Movimiento")]
    public float velocidadCaminar = 2f;
    public float velocidadCorrer = 5.5f;
    public float suavizadoGiro = 12f;

    [Header("Salto y gravedad")]
    public float alturaSalto = 1.2f;
    public float gravedad = -20f;

    CharacterController cc;
    float velY;
    float ultimoEnSuelo;

    static readonly int ParamSpeed = Animator.StringToHash("Speed");
    static readonly int ParamGrounded = Animator.StringToHash("IsGrounded");

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (camara == null && Camera.main != null) camara = Camera.main.transform;
    }

    void Update()
    {
        // 1) Leer ejes (WASD / flechas)
        Vector2 eje = Entrada.Ejes();
        Vector3 dir = new Vector3(eje.x, 0f, eje.y);
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        // Moverse relativo a hacia dónde mira la cámara
        if (camara != null)
        {
            Vector3 adelante = camara.forward; adelante.y = 0; adelante.Normalize();
            Vector3 derecha = camara.right; derecha.y = 0; derecha.Normalize();
            dir = adelante * dir.z + derecha * dir.x;
        }

        // 2) Caminar o correr (Left Shift)
        float velocidad = Entrada.Correr() ? velocidadCorrer : velocidadCaminar;
        Vector3 mover = dir * velocidad;

        // 3) Girar el personaje hacia donde camina
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion objetivo = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, suavizadoGiro * Time.deltaTime);
        }

        // 4) Salto y gravedad
        if (cc.isGrounded)
        {
            ultimoEnSuelo = Time.time;
            if (velY < 0) velY = -2f; // lo mantiene pegado al suelo
        }
        bool enSuelo = Time.time - ultimoEnSuelo < 0.1f; // pequeño margen para que no "parpadee"
        if (enSuelo && Entrada.Saltar())
        {
            velY = Mathf.Sqrt(alturaSalto * -2f * gravedad);
            ultimoEnSuelo = -1f;
            enSuelo = false;
        }
        velY += gravedad * Time.deltaTime;
        mover.y = velY;
        cc.Move(mover * Time.deltaTime);

        // 5) Animator
        if (animator != null)
        {
            Vector3 horizontal = cc.velocity; horizontal.y = 0;
            animator.SetFloat(ParamSpeed, horizontal.magnitude, 0.1f, Time.deltaTime);
            animator.SetBool(ParamGrounded, enSuelo);
        }
    }
}
