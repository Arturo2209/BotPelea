using UnityEngine;

/// <summary>
/// Inteligencia simple del bot: se acerca, golpea cada cierto tiempo, a veces bloquea
/// cuando lo atacan, a veces retrocede, y evita caerse por el borde del ring.
/// </summary>
[RequireComponent(typeof(Luchador))]
public class BotPelea : MonoBehaviour
{
    [Range(0f, 1f)] public float dificultad = 0.5f;
    public float velocidad = 3f;
    public float bordeRing = 3.8f; // a partir de aquí intenta volver al centro

    Luchador yo;
    CharacterController cc;
    float proximoAtaque, retrocederHasta, bloquearHasta, velY;
    bool yaDecidioBloqueo;

    void Start()
    {
        yo = GetComponent<Luchador>();
        cc = GetComponent<CharacterController>();
        proximoAtaque = Time.time + 1.5f;
    }

    void Update()
    {
        float vel = 0f;
        Vector3 mover = Vector3.zero;

        if (JuegoPelea.Peleando && !yo.ko && !yo.rival.ko)
        {
            var rival = yo.rival;
            Vector3 hacia = rival.transform.position - transform.position;
            hacia.y = 0;
            float dist = hacia.magnitude;
            Vector3 dir = dist > 0.01f ? hacia / dist : transform.forward;

            // Mirar siempre al rival
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);

            // ¿Bloquear? Se decide una vez por cada ataque del rival
            if (rival.Atacando && dist < 2.3f)
            {
                if (!yaDecidioBloqueo)
                {
                    yaDecidioBloqueo = true;
                    if (Random.value < 0.25f + dificultad * 0.4f) bloquearHasta = Time.time + 0.5f;
                }
            }
            else yaDecidioBloqueo = false;
            yo.bloqueando = Time.time < bloquearHasta;

            // Moverse: acercarse, o retroceder un momento
            if (Time.time < retrocederHasta) mover = -dir;
            else if (dist > 1.45f) mover = dir;

            // No caerse del ring
            var p = transform.position;
            if (Mathf.Abs(p.x) > bordeRing || Mathf.Abs(p.z) > bordeRing)
                mover += new Vector3(-p.x, 0, -p.z).normalized * 1.5f;

            // Atacar
            if (dist < 1.7f && !yo.bloqueando && Time.time > proximoAtaque)
            {
                yo.Atacar(Random.value < 0.3f);
                proximoAtaque = Time.time + Random.Range(0.5f, 1.3f) * (1.4f - dificultad);
                if (Random.value < 0.2f) retrocederHasta = Time.time + 0.6f;
            }

            if (mover.sqrMagnitude > 0.01f)
            {
                mover = mover.normalized * velocidad * (yo.bloqueando ? 0.4f : 1f);
                vel = mover.magnitude;
            }
        }

        // Gravedad
        velY = cc.isGrounded ? -2f : velY - 20f * Time.deltaTime;
        mover.y = velY;
        if (!yo.ko) cc.Move(mover * Time.deltaTime); // en K.O. la caída la maneja Luchador

        yo.animator.SetFloat("Speed", vel, 0.1f, Time.deltaTime);
        yo.animator.SetBool("IsGrounded", true);
    }
}
