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

            // De momento usamos la consola. Más adelante conectaremos tu Canvas aquí.
            Debug.Log($"[UI] Pulsa 'F' para abrir el paso a {nombreZona} [Coste: {coste}]");
        }
    }

    // Cuando salimos del área invisible...
    private void OnTriggerExit(Collider other)
    {
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorEnZona = false;
            Debug.Log("[UI] (Apagar texto de la pantalla)");
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
                        Debug.Log($"[Servidor] ¡Zona {zona.gameObject.name} activada!");
                    }
                }

                GetComponent<NetworkObject>().Despawn();
            }

            else
            {
                Debug.Log("[Servidor] Compra rechazada. Faltan puntos.");
            }
        }
    }
}
