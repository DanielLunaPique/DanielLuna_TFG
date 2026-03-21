using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CajaMisteriosa : NetworkBehaviour
{
    // --- ESTADOS DE LA CAJA ---
    public enum EstadoCaja { Inactiva, Procesando, EsperandoRecogida }
    public NetworkVariable<EstadoCaja> estadoActual = new NetworkVariable<EstadoCaja>(EstadoCaja.Inactiva);

    [Header("Control de Aparición")]
    [Tooltip("Marca esto como TRUE solo en la caja que quieres que esté activa al empezar la partida")]
    public bool empezarActiva = false;
    public NetworkVariable<bool> esLaCajaActiva = new NetworkVariable<bool>(false);

    [Header("Configuración General")]
    public int precioCaja = 950;
    public float tiempoAparicionArma = 4f;
    public float tiempoParaRecoger = 10f;

    [Header("Referencias Visuales")]
    [Tooltip("El objeto que contiene el modelo de la caja (se apagará si la caja no está aquí)")]
    public GameObject modeloCajaVisual;
    public Transform puntoFlotanteArma;

    [Header("Arsenal y Mudanza")]
    public EstadisticasArma[] armasPosibles;
    [Range(0f, 1f)] public float probabilidadDeHuir = 0.15f;

    // Variables internas
    private NetworkVariable<int> indiceArmaGenerada = new NetworkVariable<int>(-1);
    private NetworkVariable<ulong> idCompradorActual = new NetworkVariable<ulong>(9999);
    private GameObject modeloArmaVisual;
    private bool jugadorEnZona = false;
    private SistemaPuntosFPS bolsilloLocal;

    public override void OnNetworkSpawn()
    {
        // El servidor decide quién empieza encendida
        if (IsServer && empezarActiva)
        {
            esLaCajaActiva.Value = true;
        }

        // Nos suscribimos al cambio para que la caja aparezca/desaparezca sola visualmente
        esLaCajaActiva.OnValueChanged += ActualizarVisibilidadCaja;
        ActualizarVisibilidadCaja(false, esLaCajaActiva.Value);
    }

    private void ActualizarVisibilidadCaja(bool estadoViejo, bool estadoNuevo)
    {
        // Aquí puedes hacer que aparezca/desaparezca la caja, o encender unas luces, etc.
        if (modeloCajaVisual != null)
        {
            modeloCajaVisual.SetActive(estadoNuevo);
        }
    }

    void Update()
    {
        // Si el jugador no está cerca o ESTA caja no es la activa, no hacemos nada
        if (!jugadorEnZona || !esLaCajaActiva.Value) return;

        // --- GESTIÓN DE LA UI LOCAL ---
        if (estadoActual.Value == EstadoCaja.Inactiva)
        {
            if (!GameManager.Instance.pataDeCabraDesbloqueada.Value)
            {
                // [FUTURO UI] "Necesitas la Pata de Cabra para abrir esto."
            }
            else
            {
                // [FUTURO UI] "Pulsa 'F' para usar la Caja [Coste: 950]"
                if (Input.GetKeyDown(KeyCode.F)) SolicitarCaja();
            }
        }
        else if (estadoActual.Value == EstadoCaja.EsperandoRecogida)
        {
            if (NetworkManager.Singleton.LocalClientId == idCompradorActual.Value)
            {
                // [FUTURO UI] $"Pulsa 'F' para coger {armasPosibles[indiceArmaGenerada.Value].nombreArma}"
                if (Input.GetKeyDown(KeyCode.F)) RecogerArmaServerRpc();
            }
        }
    }

    private void SolicitarCaja()
    {
        if (bolsilloLocal != null)
        {
            ulong miDNI = NetworkManager.Singleton.LocalClientId;
            ComprarCajaServerRpc(miDNI);
        }
    }

    // ==========================================
    // LÓGICA DEL SERVIDOR (El que toma decisiones)
    // ==========================================

    [ServerRpc(RequireOwnership = false)]
    private void ComprarCajaServerRpc(ulong idComprador)
    {
        if (estadoActual.Value != EstadoCaja.Inactiva) return;

        var jugador = NetworkManager.Singleton.ConnectedClients[idComprador].PlayerObject;
        SistemaPuntosFPS bolsillo = jugador.GetComponent<SistemaPuntosFPS>();

        if (bolsillo != null && bolsillo.IntentarComprar(precioCaja))
        {
            idCompradorActual.Value = idComprador;
            estadoActual.Value = EstadoCaja.Procesando;

            StartCoroutine(RutinaProcesarCaja());
        }
    }

    private IEnumerator RutinaProcesarCaja()
    {
        // 1. Avisamos a todos de que enciendan el humo mágico (Tus futuras partículas)
        EncenderEfectosClientRpc(true);

        yield return new WaitForSeconds(tiempoAparicionArma);

        // 2. ¿La caja se muda?
        if (Random.value <= probabilidadDeHuir)
        {
            EncenderEfectosClientRpc(false); // Apagamos humo

            // [FUTURO] Aquí podrías meter las partículas del osito o una risa macabra
            yield return new WaitForSeconds(1.5f);

            MudarCajaServer();
            yield break; // Cortamos la función, la caja se fue
        }

        // 3. Si no se muda, elegimos un arma al azar
        indiceArmaGenerada.Value = Random.Range(0, armasPosibles.Length);
        MostrarArmaGeneradaClientRpc(indiceArmaGenerada.Value);
        estadoActual.Value = EstadoCaja.EsperandoRecogida;

        // 4. Temporizador para recogerla
        yield return new WaitForSeconds(tiempoParaRecoger);

        // 5. Si nadie la coge, se esconde el arma
        if (estadoActual.Value == EstadoCaja.EsperandoRecogida)
        {
            ApagarCajaClientRpc();
            estadoActual.Value = EstadoCaja.Inactiva;
            idCompradorActual.Value = 9999;
        }
    }

    private void MudarCajaServer()
    {
        // 1. Apagamos ESTA caja
        esLaCajaActiva.Value = false;
        estadoActual.Value = EstadoCaja.Inactiva;
        idCompradorActual.Value = 9999;

        // 2. Buscamos todas las cajas de la escena
        CajaMisteriosa[] todasLasCajas = FindObjectsOfType<CajaMisteriosa>();
        List<CajaMisteriosa> cajasDisponibles = new List<CajaMisteriosa>();

        foreach (CajaMisteriosa caja in todasLasCajas)
        {
            if (caja != this) cajasDisponibles.Add(caja);
        }

        // 3. Elegimos una al azar y la encendemos
        if (cajasDisponibles.Count > 0)
        {
            int elegida = Random.Range(0, cajasDisponibles.Count);
            cajasDisponibles[elegida].esLaCajaActiva.Value = true;

            Debug.Log($"[Servidor] La caja se ha mudado a la posición: {cajasDisponibles[elegida].transform.position}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RecogerArmaServerRpc()
    {
        if (estadoActual.Value != EstadoCaja.EsperandoRecogida) return;

        // Damos el arma "real" (la del inventario) usando el ClientRpc privado
        ClientRpcParams parametrosPrivados = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { idCompradorActual.Value } }
        };
        EntregarArmaAlInventarioClientRpc(indiceArmaGenerada.Value, parametrosPrivados);

        ApagarCajaClientRpc();
        estadoActual.Value = EstadoCaja.Inactiva;
        idCompradorActual.Value = 9999;
    }

    // ==========================================
    // LÓGICA DE CLIENTES (Visuales)
    // ==========================================

    [ClientRpc]
    private void EncenderEfectosClientRpc(bool encender)
    {
        // [FUTURO] encender o apagar tu sistema de partículas de humo
    }

    [ClientRpc]
    private void MostrarArmaGeneradaClientRpc(int indice)
    {
        EstadisticasArma stats = armasPosibles[indice];

        // ¡Usamos tu prefab visual personalizado sin destruir nada!
        if (stats.prefabVisualCaja != null && puntoFlotanteArma != null)
        {
            modeloArmaVisual = Instantiate(stats.prefabVisualCaja, puntoFlotanteArma.position, puntoFlotanteArma.rotation);
        }
    }

    [ClientRpc]
    private void ApagarCajaClientRpc()
    {
        EncenderEfectosClientRpc(false);
        if (modeloArmaVisual != null) Destroy(modeloArmaVisual);
    }

    [ClientRpc]
    private void EntregarArmaAlInventarioClientRpc(int indice, ClientRpcParams rpcParams = default)
    {
        var miJugador = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (miJugador != null)
        {
            InventarioArmas miInventario = miJugador.GetComponent<InventarioArmas>();
            if (miInventario != null)
            {
                miInventario.RecibirNuevaArma(armasPosibles[indice]);
            }
        }
    }

    // ==========================================
    // TRIGGERS DE ZONA
    // ==========================================
    private void OnTriggerEnter(Collider other)
    {
        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorEnZona = true;
            bolsilloLocal = other.GetComponentInParent<SistemaPuntosFPS>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorEnZona = false;
            bolsilloLocal = null;
        }
    }
}