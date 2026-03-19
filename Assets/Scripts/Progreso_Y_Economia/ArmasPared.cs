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
            jugadorEnZona = true;
            inventarioLocal = other.GetComponentInChildren<InventarioArmas>();
            bolsilloLocal = other.GetComponent<SistemaPuntosFPS>();

            bool tieneArma = ComprobarSiTieneArma();
            int coste = tieneArma ? precioCompra / 2 : precioCompra;
            string tipo = tieneArma ? "Munición de" : "Comprar";

            Debug.Log($"[UI] Pulsa 'F' para {tipo} {armaAComprar.nombreArma} [Coste: {coste}]");
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
            Debug.Log("[UI] (Ocultar texto)");
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
            SistemaPuntosFPS bolsillo = jugador.GetComponent<SistemaPuntosFPS>();

            // 2. Intentamos cobrarle
            if (bolsillo != null && bolsillo.IntentarComprar(coste))
            {
                Debug.Log($"[Servidor] Compra aceptada. Enviando el arma al jugador {idComprador}.");

                // 3. Preparamos un "sobre cerrado" para que solo este jugador reciba el mensaje
                ClientRpcParams parametrosPrivados = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { idComprador } }
                };

                // 4. Mandamos el mensaje privado
                EntregarArmaClientRpc(eraMunicion, parametrosPrivados);
            }
            else
            {
                Debug.Log($"[Servidor] El jugador {idComprador} no tiene dinero suficiente.");
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