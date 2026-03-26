using UnityEngine;
using Unity.Netcode;

public class Zombie : NetworkBehaviour
{
    [Header("Estadisticas")]
    public NetworkVariable<int> salud = new NetworkVariable<int>(100);

    public int puntosPorImpacto = 10;
    public int puntosPorMuerte = 70;

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ulong idAtacante)
    {
        if (salud.Value <= 0) return;

        salud.Value -= damage;

        // 1. El Servidor suma el dinero REAL. La UI del cliente se actualizará sola.
        IngresarDineroEnBancoServidor(idAtacante, puntosPorImpacto);

        if (salud.Value <= 0)
        {
            // 2. Ingresamos el dinero de la muerte
            IngresarDineroEnBancoServidor(idAtacante, puntosPorMuerte);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ZombieEliminado();
            }

            // 3. Destruimos al zombie
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }
    }

    // Función segura que solo ejecuta el Servidor para modificar la NetworkVariable
    private void IngresarDineroEnBancoServidor(ulong idJugador, int cantidad)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(idJugador, out var cliente))
        {
            var bolsillo = cliente.PlayerObject.GetComponentInChildren<SistemaPuntosFPS>();
            if (bolsillo != null)
            {
                bolsillo.puntos.Value += cantidad; // ESTO ES EL DINERO REAL
            }
        }
    }
}