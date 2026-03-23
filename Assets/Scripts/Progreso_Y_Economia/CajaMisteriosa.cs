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
    public bool empezarActiva = false;
    public NetworkVariable<bool> esLaCajaActiva = new NetworkVariable<bool>(false);

    [Header("Configuración General")]
    public int precioCaja = 950;
    public float tiempoAparicionArma = 4f;
    public float tiempoParaRecoger = 10f;

    [Header("Referencias Visuales")]
    [Tooltip("La caja negra física. Déjalo vacío si quieres que las cajas negras siempre estén por el mapa.")]
    public GameObject modeloCajaVisual;

    [Tooltip("Tu 'Magic circle 2'. Se enciende para avisar de que esta es la caja activa.")]
    public GameObject indicadorCajaActiva;

    [Tooltip("El objeto 'Particulas' (Hijo del Magic circle). Solo se enciende al comprar.")]
    public GameObject particulasInvocacion;

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
    private UIManager uiManagerLocal;

    public override void OnNetworkSpawn()
    {
        if (IsServer && empezarActiva)
        {
            esLaCajaActiva.Value = true;
        }

        esLaCajaActiva.OnValueChanged += ActualizarVisibilidadCaja;
        ActualizarVisibilidadCaja(false, esLaCajaActiva.Value);

        // Nos aseguramos de que el sub-objeto de partículas empiece apagado
        if (particulasInvocacion != null) particulasInvocacion.SetActive(false);
    }

    private void ActualizarVisibilidadCaja(bool estadoViejo, bool estadoNuevo)
    {
        // Si has asignado la caja física y quieres que desaparezca/aparezca entera
        if (modeloCajaVisual != null) modeloCajaVisual.SetActive(estadoNuevo);

        // Encendemos el Magic Circle 2 (luz morada) si la caja es la activa del mapa
        if (indicadorCajaActiva != null) indicadorCajaActiva.SetActive(estadoNuevo);
    }

    void Update()
    {
        if (!jugadorEnZona || !esLaCajaActiva.Value || uiManagerLocal == null) return;

        if (estadoActual.Value == EstadoCaja.Inactiva)
        {
            if (!GameManager.Instance.pataDeCabraDesbloqueada.Value)
            {
                uiManagerLocal.MostrarTextoInteraccion("Necesitas la Pata de Cabra para abrir esto");
            }
            else
            {
                uiManagerLocal.MostrarTextoInteraccion($"Pulsa [F] para usar la Caja - {precioCaja} pts");
                if (Input.GetKeyDown(KeyCode.F)) SolicitarCaja();
            }
        }
        else if (estadoActual.Value == EstadoCaja.Procesando)
        {
            // Mientras salen los rayos morados, ocultamos el texto
            uiManagerLocal.OcultarTextoInteraccion();
        }
        else if (estadoActual.Value == EstadoCaja.EsperandoRecogida)
        {
            if (NetworkManager.Singleton.LocalClientId == idCompradorActual.Value)
            {
                uiManagerLocal.MostrarTextoInteraccion($"Pulsa [F] para coger {armasPosibles[indiceArmaGenerada.Value].nombreArma}");
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
    // LÓGICA DEL SERVIDOR
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
        // ¿La caja se muda?
        if (Random.value <= probabilidadDeHuir)
        {
            EfectoHuirClientRpc();
            yield return new WaitForSeconds(2.5f);
            MudarCajaServer();
            yield break;
        }

        // Elegimos el arma al azar
        indiceArmaGenerada.Value = Random.Range(0, armasPosibles.Length);
        IniciarInvocacionClientRpc(indiceArmaGenerada.Value);

        yield return new WaitForSeconds(tiempoAparicionArma);

        estadoActual.Value = EstadoCaja.EsperandoRecogida;

        // Esperamos el tiempo de recogida
        yield return new WaitForSeconds(tiempoParaRecoger - 2.5f);

        if (estadoActual.Value == EstadoCaja.EsperandoRecogida)
        {
            AvisarParpadeoClientRpc();
            yield return new WaitForSeconds(2.5f);

            if (estadoActual.Value == EstadoCaja.EsperandoRecogida)
            {
                ApagarCajaClientRpc();
                estadoActual.Value = EstadoCaja.Inactiva;
                idCompradorActual.Value = 9999;
            }
        }
    }

    private void MudarCajaServer()
    {
        esLaCajaActiva.Value = false;
        estadoActual.Value = EstadoCaja.Inactiva;
        idCompradorActual.Value = 9999;

        CajaMisteriosa[] todasLasCajas = FindObjectsOfType<CajaMisteriosa>();
        List<CajaMisteriosa> cajasDisponibles = new List<CajaMisteriosa>();

        foreach (CajaMisteriosa caja in todasLasCajas)
        {
            if (caja != this) cajasDisponibles.Add(caja);
        }

        if (cajasDisponibles.Count > 0)
        {
            int elegida = Random.Range(0, cajasDisponibles.Count);
            cajasDisponibles[elegida].esLaCajaActiva.Value = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RecogerArmaServerRpc()
    {
        if (estadoActual.Value != EstadoCaja.EsperandoRecogida) return;

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
    // LÓGICA DE CLIENTES (Magia y Visuales)
    // ==========================================

    [ClientRpc]
    private void EfectoHuirClientRpc()
    {
        // Activar aquí algo visual cuando la caja se muda
    }

    [ClientRpc]
    private void IniciarInvocacionClientRpc(int indice)
    {
        // 1. Encendemos el objeto "Particulas" para que salgan chispas y humo
        if (particulasInvocacion != null) particulasInvocacion.SetActive(true);

        // 2. Instanciamos el arma
        EstadisticasArma stats = armasPosibles[indice];
        if (stats.prefabVisualCaja != null && puntoFlotanteArma != null)
        {
            modeloArmaVisual = Instantiate(stats.prefabVisualCaja, puntoFlotanteArma.position, puntoFlotanteArma.rotation);
            StartCoroutine(AparecerArmaMagicamente());
        }
    }

    private IEnumerator AparecerArmaMagicamente()
    {
        if (modeloArmaVisual == null) yield break;

        Vector3 tamanoFinal = modeloArmaVisual.transform.localScale;
        modeloArmaVisual.transform.localScale = Vector3.zero;

        float tiempoPasado = 0f;

        while (tiempoPasado < tiempoAparicionArma)
        {
            tiempoPasado += Time.deltaTime;
            float porcentaje = tiempoPasado / tiempoAparicionArma;

            modeloArmaVisual.transform.localScale = Vector3.Lerp(Vector3.zero, tamanoFinal, porcentaje);
            yield return null;
        }

        modeloArmaVisual.transform.localScale = tamanoFinal;
    }

    [ClientRpc]
    private void AvisarParpadeoClientRpc()
    {
        StartCoroutine(RutinaParpadeo());
    }

    private IEnumerator RutinaParpadeo()
    {
        while (modeloArmaVisual != null)
        {
            modeloArmaVisual.SetActive(!modeloArmaVisual.activeSelf);
            yield return new WaitForSeconds(0.15f);
        }
    }

    [ClientRpc]
    private void ApagarCajaClientRpc()
    {
        // Apagamos SOLO las partículas de invocación, el Magic Circle base se queda encendido
        if (particulasInvocacion != null) particulasInvocacion.SetActive(false);
        if (modeloArmaVisual != null) Destroy(modeloArmaVisual);
    }

    [ClientRpc]
    private void EntregarArmaAlInventarioClientRpc(int indice, ClientRpcParams rpcParams = default)
    {
        var miJugador = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (miJugador != null)
        {
            InventarioArmas miInventario = miJugador.GetComponentInChildren<InventarioArmas>();
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
            uiManagerLocal = netObj.GetComponentInChildren<UIManager>();
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

            if (uiManagerLocal != null)
            {
                uiManagerLocal.OcultarTextoInteraccion();
                uiManagerLocal = null;
            }
        }
    }
}