using Unity.Netcode;
using UnityEngine;

public class ArmasPared : NetworkBehaviour
{
    [Header("Configuración del Arma")]
    public EstadisticasArma armaAComprar; // El ScriptableObject del arma que vende esta pared
    public int precioCompra = 1000;

    // Variables locales para el jugador que se acerca
    private bool jugadorEnZona = false;
    private InventarioArmas inventarioLocal;
    private SistemaPuntosFPS bolsilloLocal;
    private UIManager uiManagerLocal;

    void Update()
    {
        if (jugadorEnZona && inventarioLocal != null && bolsilloLocal != null)
        {
            // 1. Comprobamos si el jugador ya tiene esta arma exacta
            bool yaTieneElArma = ComprobarSiTieneArma();

            // 2. Calculamos el coste (La mitad si solo es munición)
            int costeActual = yaTieneElArma ? (precioCompra / 2) : precioCompra;
            string accion = yaTieneElArma ? "Munición de" : "Arma";

            // [FUTURO UI] Aquí conectarás el texto de tu Canvas. De momento usamos la consola.
            // Para no spamear la consola 60 veces por segundo, lo ponemos solo si pulsa la tecla, 
            // pero en tu UI real esto actualizará el texto de la pantalla.

            if (Input.GetKeyDown(KeyCode.F))
            {
                // Le pedimos al servidor que procese la compra
                ulong miDNI = NetworkManager.Singleton.LocalClientId;
                ProcesarCompraServerRpc(miDNI, costeActual, yaTieneElArma);
            }
        }
    }

    private bool ComprobarSiTieneArma()
    {
        // Revisamos los 2 huecos del inventario local
        foreach (DatosArma arma in inventarioLocal.armasEquipadas)
        {
            if (arma != null && arma.estadisticas == armaAComprar)
            {
                return true; // ¡Ya la tiene!
            }
        }
        return false; // No la tiene
    }

    private void OnTriggerEnter(Collider other)
    {
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            inventarioLocal = other.GetComponentInChildren<InventarioArmas>();
            bolsilloLocal = other.GetComponentInChildren<SistemaPuntosFPS>();

            uiManagerLocal = netObj.GetComponentInChildren<UIManager>();

            if (inventarioLocal == null || bolsilloLocal == null) return;

            jugadorEnZona = true;

            bool tieneArma = ComprobarSiTieneArma();
            int coste = tieneArma ? precioCompra / 2 : precioCompra;
            string tipo = tieneArma ? "Munición de" : "Comprar";

            if (uiManagerLocal != null)
            {
                uiManagerLocal.MostrarTextoInteraccion($"Pulsa [F] para {tipo} {armaAComprar.nombreArma} - {coste} pts");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorEnZona = false;
            inventarioLocal = null;
            bolsilloLocal = null;

            if (uiManagerLocal != null)
            {
                uiManagerLocal.OcultarTextoInteraccion();
                uiManagerLocal = null;
            }
        }
    }

    // --- MAGIA DE RED: EL SERVIDOR COBRA Y ENVÍA UN MENSAJE PRIVADO ---

    [ServerRpc(RequireOwnership = false)]
    private void ProcesarCompraServerRpc(ulong idComprador, int coste, bool eraMunicion)
    {
        // 1. Buscamos al jugador en el servidor
        var jugador = NetworkManager.Singleton.ConnectedClients[idComprador].PlayerObject;
        if (jugador != null)
        {
            SistemaPuntosFPS bolsillo = jugador.GetComponentInChildren<SistemaPuntosFPS>();

            // 2. Intentamos cobrarle
            if (bolsillo != null && bolsillo.IntentarComprar(coste))
            {

                // 3. Preparamos un "sobre cerrado" para que solo este jugador reciba el mensaje
                ClientRpcParams parametrosPrivados = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { idComprador } }
                };

                if (uiManagerLocal != null)
                {
                    uiManagerLocal.OcultarTextoInteraccion();
                    uiManagerLocal = null;
                }

                // 4. Mandamos el mensaje privado
                EntregarArmaClientRpc(eraMunicion, parametrosPrivados);
            }
        }
    }

    [ClientRpc]
    private void EntregarArmaClientRpc(bool esMunicion, ClientRpcParams rpcParams = default)
    {
        // ESTO SOLO SE EJECUTA EN EL ORDENADOR DEL QUE HA COMPRADO

        // Volvemos a buscar nuestro propio jugador local
        var miJugador = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (miJugador != null)
        {
            InventarioArmas miInventario = miJugador.GetComponentInChildren<InventarioArmas>();

            if (miInventario != null)
            {
                if (esMunicion)
                {
                    // Llenamos las balas
                    miInventario.RellenarMunicion(armaAComprar);
                }
                else
                {
                    // ¡Nos da el arma nueva!
                    miInventario.RecibirNuevaArma(armaAComprar);
                }
            }
        }
    }
}