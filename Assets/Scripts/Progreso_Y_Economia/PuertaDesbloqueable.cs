using UnityEngine;
using Unity.Netcode;

public class PuertaDesbloqueable : NetworkBehaviour
{
    [Header("Configuracion de la Puerta")]
    public string nombreZona;
    public int coste = 1000;

    [Header("Bloqueo por Misión (Easter Egg)")]
    [Tooltip("¿Esta puerta está bloqueada hasta cierto punto de la partida?")]
    public bool requierePasoHistoria = false;
    [Tooltip("El índice de la Línea de Tiempo en el que se desbloquea esta puerta (Ej: 2)")]
    public int indiceTimelineDesbloqueo = 2;

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
                // ESCUDO: Si está bloqueada por la historia, ignoramos el botón "E" por completo
                if (EstaBloqueadaPorHistoria())
                {
                    // Opcional: Aquí podrías reproducir un sonido de "Error/Acceso Denegado"
                    return;
                }

                ulong miDNI = NetworkManager.Singleton.LocalClientId;
                ComprarPuertaServerRpc(miDNI);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (estaAbierta.Value) return;

        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorEnZona = true;
            uiManagerLocal = netObj.GetComponentInChildren<UIManager>();

            if (uiManagerLocal != null)
            {
                // Verificamos si la puerta está bloqueada por el Easter Egg
                if (EstaBloqueadaPorHistoria())
                {
                    MostrarTextoDeBloqueo();
                }
                else
                {
                    // Si no está bloqueada (o ya hemos pasado esa fase), mostramos la compra normal
                    uiManagerLocal.MostrarTextoInteraccion($"Pulsa [E] para abrir el paso a {nombreZona} [Coste: {coste} pts]");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
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

    // --- FUNCIONES DE LÓGICA DE HISTORIA ---

    private bool EstaBloqueadaPorHistoria()
    {
        if (!requierePasoHistoria || QuestManager.Instance == null) return false;

        // Retorna TRUE si nuestro nivel en la partida es menor al que pide la puerta
        return QuestManager.Instance.indiceTimeline.Value < indiceTimelineDesbloqueo;
    }

    private void MostrarTextoDeBloqueo()
    {
        string misionActual = QuestManager.Instance.idPasoActual.Value.ToString();
        string textoRechazo = "<color=red>[ACCESO DENEGADO]</color> ";

        if (misionActual == "Tarjeta")
            textoRechazo += "Se requiere Tarjeta de Acceso.";
        else if (misionActual == "Hackeo")
            textoRechazo += "Se requiere Anulación de Seguridad.";
        else if (misionActual == "BuscarPiezas")
            textoRechazo += "Encuentra las piezas del bastón primero.";
        else
            textoRechazo += "Sistemas de energía inestables.";

        uiManagerLocal.MostrarTextoInteraccion(textoRechazo);
    }

    // --- FUNCIONES DE RED ---

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

            foreach (ZonaZombies zona in zonasADesbloquear)
            {
                if (zona != null)
                {
                    zona.estaActiva = true;
                }
            }

            NetworkObject netObj = GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (uiManagerLocal != null)
        {
            uiManagerLocal.OcultarTextoInteraccion();
            uiManagerLocal = null;
        }
    }
}