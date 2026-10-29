using UnityEngine;

/// <summary>
/// Lo que comparten el jugador y el bot: vida, golpes (rápido y fuerte), bloqueo, empuje y K.O.
/// Los golpes y la guardia se animan moviendo los huesos de los brazos por código (el personaje es Humanoid).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Luchador : MonoBehaviour
{
    public string nombre = "Jugador";
    public float vidaMax = 100f;
    public float vida = 100f;
    public Animator animator;
    public Luchador rival;
    public Renderer[] cuerpo;

    [HideInInspector] public bool bloqueando;
    [HideInInspector] public bool ko;
    [HideInInspector] public Vector3 empuje;

    CharacterController cc;
    float ataqueInicio = -10f, ataqueDuracion;
    bool fuerte, golpeAplicado, brazoDerecho;
    float rojoHasta;
    MaterialPropertyBlock bloque;

    public bool Atacando => Time.time - ataqueInicio < ataqueDuracion;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        vida = vidaMax;
        bloque = new MaterialPropertyBlock();
    }

    public void Atacar(bool esFuerte)
    {
        if (ko || Atacando || bloqueando) return;
        fuerte = esFuerte;
        ataqueDuracion = fuerte ? 0.6f : 0.35f;
        ataqueInicio = Time.time;
        golpeAplicado = false;
        brazoDerecho = fuerte || !brazoDerecho; // los rápidos alternan brazo
        // Girar hacia el rival para no fallar por estar de lado
        Vector3 d = rival.transform.position - transform.position;
        d.y = 0;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        JuegoPelea.Instancia.Sonar(JuegoPelea.Instancia.sAire, 0.5f);
    }

    float caidaKO;

    void Update()
    {
        if (ko)
        {
            // Ya no lo controla nadie: solo cae por gravedad (por si lo sacaron del ring)
            caidaKO = cc.isGrounded ? -2f : caidaKO - 20f * Time.deltaTime;
            cc.Move((empuje * 0.5f + Vector3.up * caidaKO) * Time.deltaTime);
            empuje = Vector3.MoveTowards(empuje, Vector3.zero, 10f * Time.deltaTime);
            return;
        }

        // El golpe "conecta" a mitad de la animación
        if (Atacando && !golpeAplicado && Time.time - ataqueInicio > ataqueDuracion * 0.45f)
        {
            golpeAplicado = true;
            Vector3 d = rival.transform.position - transform.position;
            d.y = 0;
            float alcance = fuerte ? 1.9f : 1.7f;
            if (!rival.ko && d.magnitude < alcance && Vector3.Dot(transform.forward, d.normalized) > 0.4f)
                rival.RecibirGolpe(fuerte ? 18f : 9f, d.normalized * (fuerte ? 10f : 5f));
        }

        // Empuje por los golpes (se va frenando)
        if (empuje.sqrMagnitude > 0.01f)
        {
            cc.Move(empuje * Time.deltaTime);
            empuje = Vector3.MoveTowards(empuje, Vector3.zero, 18f * Time.deltaTime);
        }

        // Si se cae del ring: K.O.
        if (transform.position.y < -1.2f) KO();
    }

    public void RecibirGolpe(float danio, Vector3 empujon)
    {
        if (ko) return;
        var juego = JuegoPelea.Instancia;
        bool deFrente = Vector3.Dot(transform.forward, -empujon.normalized) > 0.3f;
        if (bloqueando && deFrente)
        {
            danio *= 0.2f;
            empujon *= 0.4f;
            juego.Sonar(juego.sBloqueo);
        }
        else
        {
            juego.Sonar(danio > 10 ? juego.sGolpeFuerte : juego.sGolpe);
            rojoHasta = Time.time + 0.15f;
        }
        vida = Mathf.Max(0f, vida - danio);
        empuje += empujon;
        if (vida <= 0f) KO();
    }

    void KO()
    {
        if (ko) return;
        ko = true;
        bloqueando = false;
        JuegoPelea.Instancia.Terminar(this);
    }

    // ---------------- Animación de brazos (después del Animator) ----------------

    void LateUpdate()
    {
        // Parpadeo rojo al recibir un golpe
        Color c = Time.time < rojoHasta ? new Color(1f, 0.3f, 0.3f) : Color.white;
        foreach (var r in cuerpo)
        {
            r.GetPropertyBlock(bloque);
            bloque.SetColor("_BaseColor", c * r.sharedMaterial.color);
            bloque.SetColor("_Color", c * r.sharedMaterial.color);
            r.SetPropertyBlock(bloque);
        }

        var modelo = animator.transform;
        if (ko)
        {
            // Cae de espaldas
            modelo.localRotation = Quaternion.Slerp(modelo.localRotation, Quaternion.Euler(-85f, 0, 0), 6f * Time.deltaTime);
            return;
        }

        Vector3 f = transform.forward, u = Vector3.up, r2 = transform.right;
        if (bloqueando)
        {
            // Guardia: los dos brazos delante de la cara
            Brazo(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, f * 0.6f - u * 0.5f - r2 * 0.2f, u + f * 0.2f - r2 * 0.6f, 1f);
            Brazo(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, f * 0.6f - u * 0.5f + r2 * 0.2f, u + f * 0.2f + r2 * 0.6f, 1f);
        }
        else if (Atacando)
        {
            float t = (Time.time - ataqueInicio) / ataqueDuracion;
            float peso = t < 0.45f ? t / 0.45f : 1f - (t - 0.45f) / 0.55f;
            Vector3 lado = brazoDerecho ? -r2 * 0.15f : r2 * 0.15f;
            Vector3 dir = f + lado + u * (fuerte ? 0.15f : 0.05f);
            if (brazoDerecho)
                Brazo(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, dir, dir, peso);
            else
                Brazo(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, dir, dir, peso);
            if (fuerte)
            {
                // El golpe fuerte gira un poco el torso
                var pecho = animator.GetBoneTransform(HumanBodyBones.Spine);
                if (pecho != null) pecho.rotation = Quaternion.AngleAxis(-25f * peso, Vector3.up) * pecho.rotation;
            }
        }
    }

    /// <summary>Apunta el brazo y el antebrazo en las direcciones dadas (peso 0 = pose de la animación, 1 = pose completa).</summary>
    void Brazo(HumanBodyBones superior, HumanBodyBones inferior, HumanBodyBones mano, Vector3 dirBrazo, Vector3 dirAntebrazo, float peso)
    {
        var s = animator.GetBoneTransform(superior);
        var i = animator.GetBoneTransform(inferior);
        var m = animator.GetBoneTransform(mano);
        if (s == null || i == null || m == null) return;
        var q = Quaternion.FromToRotation(i.position - s.position, dirBrazo);
        s.rotation = Quaternion.Slerp(Quaternion.identity, q, peso) * s.rotation;
        var q2 = Quaternion.FromToRotation(m.position - i.position, dirAntebrazo);
        i.rotation = Quaternion.Slerp(Quaternion.identity, q2, peso) * i.rotation;
    }
}
