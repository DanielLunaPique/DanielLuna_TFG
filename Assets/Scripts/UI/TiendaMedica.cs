using UnityEngine;
using Unity.Netcode;

public class TiendaMedica : NetworkBehaviour
{
    [Header("Configuración")]
    [Tooltip("El objeto vacío en la puerta de la tienda donde aparecerá el jugador")]
    public Transform puntoSpawnRevivir;

    [Header("Economía de Revivir")]
    public int precioInicial = 500;
    public int incrementoPorUso = 500;
    public int precioMaximo = 5000;

    public NetworkVariable<int> precioActualRevivir = new NetworkVariable<int>(500, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool jugadorCerca = false;
    private UIManager uiLocalJugador;

    private void Start()
    {
        // El servidor establece el precio inicial al arrancar la partida
        if (IsServer)
        {
            precioActualRevivir.Value = precioInicial;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                jugadorCerca = true;
                uiLocalJugador = other.GetComponentInChildren<UIManager>();

                if (uiLocalJugador != null)
                {
                    uiLocalJugador.tiendaActual = this; // Le decimos al menú: "Yo soy tu tienda"
                    uiLocalJugador.MostrarTextoInteraccion("Mantén [E] para Tienda Médica");
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                jugadorCerca = false;
                if (uiLocalJugador != null)
                {
                    uiLocalJugador.OcultarTextoInteraccion();
                    uiLocalJugador.CerrarMenuTiendaMedica();
                    uiLocalJugador.tiendaActual = null;
                }
            }
        }
    }

    private void Update()
    {
        if (!jugadorCerca || uiLocalJugador == null) return;

        if (Input.GetKeyDown(KeyCode.E) && !uiLocalJugador.menuTiendaAbierto)
        {
            uiLocalJugador.OcultarTextoInteraccion();
            uiLocalJugador.AbrirMenuTiendaMedica();
        }
        else if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)) && uiLocalJugador.menuTiendaAbierto)
        {
            uiLocalJugador.CerrarMenuTiendaMedica();
            uiLocalJugador.MostrarTextoInteraccion("Mantén [E] para Tienda Médica");
        }
    }

    // ==========================================
    // MAGIA DE RED: EL SERVIDOR TOMA LA DECISIÓN
    // ==========================================
    [ServerRpc(RequireOwnership = false)]
    public void SolicitarRevivirServerRpc(ulong idComprador, ulong idMuerto, int costeEnviado)
    {
        Debug.Log($"[SERVIDOR] Petición recibida: El Jugador {idComprador} quiere revivir al Jugador {idMuerto}");

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(idComprador, out var comprador))
        {
            SistemaPuntosFPS puntosComprador = comprador.PlayerObject.GetComponent<SistemaPuntosFPS>();

            if (puntosComprador != null)
            {
                Debug.Log($"[SERVIDOR] El comprador tiene {puntosComprador.puntos.Value} puntos. El precio del servidor es {precioActualRevivir.Value}.");

                if (puntosComprador.puntos.Value >= precioActualRevivir.Value)
                {
                    Debug.Log("[SERVIDOR] Dinero suficiente. ¡Cobrando al jugador!");
                    puntosComprador.puntos.Value -= precioActualRevivir.Value;

                    int nuevoPrecio = precioActualRevivir.Value + incrementoPorUso;
                    precioActualRevivir.Value = Mathf.Min(nuevoPrecio, precioMaximo);

                    if (NetworkManager.Singleton.ConnectedClients.TryGetValue(idMuerto, out var muerto))
                    {
                        SaludJugador saludMuerto = muerto.PlayerObject.GetComponent<SaludJugador>();
                        if (saludMuerto != null)
                        {
                            Debug.Log($"[SERVIDOR] Hemos encontrado el cuerpo del muerto. ¿El servidor sabe que está muerto?: {saludMuerto.estaMuerto}");

                            if (saludMuerto.estaMuerto)
                            {
                                Debug.Log("[SERVIDOR] ¡TODO CORRECTO! Lanzando el milagro (ClientRpc) a todas las pantallas...");
                                saludMuerto.saludActual.Value = saludMuerto.saludMaxima;
                                saludMuerto.EjecutarRevivirClientRpc(puntoSpawnRevivir.position, puntoSpawnRevivir.rotation);
                            }
                            else
                            {
                                Debug.LogWarning("[SERVIDOR] Falla porque el servidor piensa que el jugador NO está muerto.");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[SERVIDOR] Compra denegada: El jugador no tiene suficiente dinero.");
                }
            }
        }
    }
}