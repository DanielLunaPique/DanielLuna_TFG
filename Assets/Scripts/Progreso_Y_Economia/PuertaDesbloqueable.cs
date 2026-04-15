using UnityEngine;
using Unity.Netcode;

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
        if (jugadorEnZona && !estaAbierta.Value)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                ulong miDNI = NetworkManager.Singleton.LocalClientId;
                ComprarPuertaServerRpc(miDNI);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (estaAbierta.Value) return;

        // Usamos GetComponentInParent porque el collider suele colgar de la raíz del Player
        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorEnZona = true;
            uiManagerLocal = netObj.GetComponentInChildren<UIManager>();

            if (uiManagerLocal != null)
            {
                uiManagerLocal.MostrarTextoInteraccion($"Pulsa 'E' para abrir el paso a {nombreZona} [Coste: {coste} pts]");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Corregido: GetComponentInParent igual que en el Enter
        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
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
    public void ComprarPuertaServerRpc(ulong idComprador)
    {
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(idComprador, out var cliente)) return;

        var jugador = cliente.PlayerObject;
        if (jugador == null) return;

        SistemaPuntosFPS bolsillo = jugador.GetComponentInChildren<SistemaPuntosFPS>();
        if (bolsillo == null) return;

        if (bolsillo.IntentarComprar(coste))
        {
            estaAbierta.Value = true;

            // ==========================================
            // LO QUE SE ME OLVIDÓ: ¡ACTIVAR LAS ZONAS!
            // ==========================================
            foreach (ZonaZombies zona in zonasADesbloquear)
            {
                if (zona != null)
                {
                    // AQUÍ TIENES QUE PONER TU VARIABLE O FUNCIÓN REAL
                    // Dependiendo de cómo hiciste el script ZonaZombies, será algo así:
                    zona.estaActiva = true;
                    // o quizás: zona.ActivarZona();
                }
            }

            // El Servidor destruye la puerta. Esto disparará OnNetworkDespawn en todos lados.
            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }
    }

    // ==========================================
    // LA SOLUCIÓN: Limpieza justo antes de morir
    // ==========================================
    public override void OnNetworkDespawn()
    {
        // Esto se ejecuta localmente en el Cliente justo antes de que la puerta desaparezca
        if (uiManagerLocal != null)
        {
            uiManagerLocal.OcultarTextoInteraccion();
            uiManagerLocal = null;
        }
    }
}