using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class PuertaDesbloqueable : NetworkBehaviour
{
    [Header("Configuracion de la Puerta")]
    public string nombreZona;
    public int coste = 1000;

    [Header("Conexión de Zonas")]
    public ZonaZombies[] zonasADesbloquear;

    public NetworkVariable<bool> estaAbierta = new NetworkVariable<bool>(false);

    private bool jugadorEnZona = false;
    private UIManager uiManagerLocal;

    void Update()
    {
        if(jugadorEnZona && !estaAbierta.Value)
        {
            if (Input.GetKey(KeyCode.F))
            {
                ulong idPlayer = NetworkManager.Singleton.LocalClientId;
                AbrirPuertaServerRpc(idPlayer);
                
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (estaAbierta.Value) return;

        // Comprobamos si el que ha chocado es un jugador y si es NUESTRO jugador
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorEnZona = true;
            uiManagerLocal = netObj.GetComponentInChildren<UIManager>();
            if (uiManagerLocal != null)
            {
                uiManagerLocal.MostrarTextoInteraccion($"Pulsa 'F' para abrir el paso a {nombreZona} [Coste: {coste} pts");
            }
        }
    }

    // Cuando salimos del área invisible...
    private void OnTriggerExit(Collider other)
    {
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorEnZona = false;
            if (uiManagerLocal != null)
            {
                uiManagerLocal.OcultarTextoInteraccion();
                uiManagerLocal = null;
            }
        }
    }


    [ServerRpc(RequireOwnership = false)]
    private void AbrirPuertaServerRpc(ulong idPlayer)
    {
        if (estaAbierta.Value) return;

        var jugador = NetworkManager.Singleton.ConnectedClients[idPlayer].PlayerObject;
        if(jugador != null)
        {
            SistemaPuntosFPS bolsillo = jugador.GetComponent<SistemaPuntosFPS>();
            if (bolsillo != null && bolsillo.IntentarComprar(coste))
            {
                estaAbierta.Value = true;

                foreach(ZonaZombies zona in zonasADesbloquear)
                {
                    if(zona != null)
                    {
                        zona.estaActiva = true;
                    }
                }
                if(uiManagerLocal != null)
                {
                    uiManagerLocal.OcultarTextoInteraccion();
                }

                GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}
