using UnityEngine;

/// <summary>
/// Controles de pelea del jugador (el movimiento lo hace MovimientoTerceraPersona):
/// Clic izquierdo o J = golpe rápido, Clic derecho o K = golpe fuerte, mantener F o L = bloquear.
/// </summary>
[RequireComponent(typeof(Luchador))]
public class ControlJugadorPelea : MonoBehaviour
{
    Luchador yo;

    void Start() => yo = GetComponent<Luchador>();

    void Update()
    {
        if (!JuegoPelea.Peleando || yo.ko) { yo.bloqueando = false; return; }
        yo.bloqueando = Entrada.Bloquear();
        if (Entrada.GolpeRapido()) yo.Atacar(false);
        if (Entrada.GolpeFuerte()) yo.Atacar(true);
    }
}
