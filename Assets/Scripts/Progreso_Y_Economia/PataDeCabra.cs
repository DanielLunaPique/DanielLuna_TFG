using Unity.Netcode;
using UnityEngine;

public class PataDeCabra : NetworkBehaviour
{
    private bool jugadorCerca = false;
    private UIManager uiManagerLocal;

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
        if (uiManagerLocal != null)
        {
            uiManagerLocal.OcultarTextoInteraccion();
            uiManagerLocal = null;
        }

        GetComponent<NetworkObject>().Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorCerca = true;

            // Buscamos desde la raíz hacia abajo
            uiManagerLocal = netObj.GetComponentInChildren<UIManager>();

            if (uiManagerLocal != null)
            {
                uiManagerLocal.MostrarTextoInteraccion("Pulsa [F] para recoger Pata de Cabra");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorCerca = false;

            if (uiManagerLocal != null)
            {
                uiManagerLocal.OcultarTextoInteraccion();
                uiManagerLocal = null;
            }
        }
    }
}