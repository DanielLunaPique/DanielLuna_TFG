using UnityEngine;
using Unity.Netcode;
using System.Collections; // Necesario para las corrutinas

public class Zombie : NetworkBehaviour
{
    [Header("Estadisticas")]
    public NetworkVariable<int> salud = new NetworkVariable<int>(18);

    public int puntosPorImpacto = 10;
    public int puntosPorMuerte = 70;

    [Header("Animaciones")]
    public Animator animator;

    public override void OnNetworkSpawn()
    {
        // Si tienes más código aquí arriba, déjalo.

        // Envolvemos la asignación de vida para que los clientes no la toquen
        if (IsServer)
        {
            NetworkVariable<int> rondaActual = GameManager.Instance.rondaActual;

            salud.Value = salud.Value * rondaActual.Value;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    // Añadimos el booleano al final. Le ponemos "= false" por defecto para que no dé errores
    // si lo llamas desde otro sitio (como el Orbe o el GameManager)
    public void TakeDamageServerRpc(int damage, ulong idAtacante, bool esTiroALaCabeza = false)
    {
        if (salud.Value <= 0) return;

        salud.Value -= damage;
        IngresarDineroEnBancoServidor(idAtacante, puntosPorImpacto);

        if (salud.Value <= 0)
        {
            // --- MAGIA DEL HEADSHOT ---
            int puntosFinales = esTiroALaCabeza ? 100 : puntosPorMuerte;
            IngresarDineroEnBancoServidor(idAtacante, puntosFinales);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ZombieEliminado();
            }

            UnityEngine.AI.NavMeshAgent agente = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agente != null)
            {
                agente.isStopped = true;
                agente.velocity = Vector3.zero;
                agente.speed = 0f;
                agente.enabled = false;
            }

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            MorirClientRpc();

            StartCoroutine(EsperarYDespawnear());
        }
    }

    // El servidor avisa a todos los jugadores
    [ClientRpc]
    private void MorirClientRpc()
    {
        // Disparamos la animación de muerte
        if (animator != null) animator.SetTrigger("Death");

        // Desactivamos la cápsula de colisión para que los jugadores puedan pisar el cadáver
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        ParteDelCuerpo[] hitboxes = GetComponentsInChildren<ParteDelCuerpo>();
        foreach (ParteDelCuerpo hitbox in hitboxes)
        {
            Collider hitboxCol = hitbox.GetComponent<Collider>();
            if (hitboxCol != null) hitboxCol.enabled = false;
        }

        // Desactivamos el script de IA para que deje de mirarnos o intentar moverse
        ZombieIA ia = GetComponent<ZombieIA>();
        if (ia != null) ia.enabled = false;
    }

    private IEnumerator EsperarYDespawnear()
    {
        // AJUSTA ESTE NÚMERO: Es el tiempo en segundos que dura tu animación de muerte
        yield return new WaitForSeconds(3f);

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
    }

    private void IngresarDineroEnBancoServidor(ulong idJugador, int cantidad)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(idJugador, out var cliente))
        {
            var bolsillo = cliente.PlayerObject.GetComponentInChildren<SistemaPuntosFPS>();
            if (bolsillo != null) bolsillo.puntos.Value += cantidad;
        }
    }
}