using Unity.Netcode;
using UnityEngine;

public class PataDeCabra : NetworkBehaviour
{
    private bool jugadorCerca = false;

    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.F))
        {
            // Le decimos al servidor que la hemos cogido
            RecogerServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RecogerServerRpc()
    {
        // 1. Desbloqueamos el uso de cajas para todo el mundo
        GameManager.Instance.pataDeCabraDesbloqueada.Value = true;

        Debug.Log("[Servidor] ¡Un jugador ha encontrado la Pata de Cabra!");

        // 2. Destruimos el objeto de la escena para todos
        GetComponent<NetworkObject>().Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<NetworkObject>() != null && other.GetComponentInParent<NetworkObject>().IsLocalPlayer)
        {
            jugadorCerca = true;
            Debug.Log("[UI] Pulsa 'F' para recoger la Pata de Cabra");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<NetworkObject>() != null && other.GetComponentInParent<NetworkObject>().IsLocalPlayer)
        {
            jugadorCerca = false;
        }
    }
}