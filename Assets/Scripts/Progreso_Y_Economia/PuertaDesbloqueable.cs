using UnityEngine;
using Unity.Netcode;

public class PuertaDesbloqueable : NetworkBehaviour
{
    [Header("Configuracion de la Puerta")]
    public string nombreZona;
    public int coste = 1000;

    [Header("Bloqueo por Misión (Easter Egg)")]
    public bool requierePasoHistoria = false;
    public int indiceTimelineDesbloqueo = 2;

    [Header("Conexión de Zonas")]
    public ZonaZombies[] zonasADesbloquear;

    [Header("Audio del Mundo (Físico)")]
    public AudioClip sonidoAbrirPuerta;
    public AudioClip sonidoAccesoDenegado;

    [Header("Audio del Personaje (Voz)")]
    [Tooltip("Probabilidad de que el personaje diga una frase al abrir la puerta (0.0 a 1.0)")]
    [Range(0f, 1f)] public float probabilidadFrase = 0.6f;

    public NetworkVariable<bool> estaAbierta = new NetworkVariable<bool>(false);

    private bool jugadorEnZona = false;
    private UIManager uiManagerLocal;

    void Update()
    {
        if (jugadorEnZona && !estaAbierta.Value)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (EstaBloqueadaPorHistoria())
                {
                    if (sonidoAccesoDenegado != null)
                        AudioSource.PlayClipAtPoint(sonidoAccesoDenegado, transform.position);
                    return;
                }

                // --- LA CORRECCIÓN: Matrícula del cuerpo, no de la conexión ---
                ulong miCuerpoID = NetworkManager.Singleton.LocalClient.PlayerObject.NetworkObjectId;
                ComprarPuertaServerRpc(miCuerpoID);
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
                if (EstaBloqueadaPorHistoria()) MostrarTextoDeBloqueo();
                else uiManagerLocal.MostrarTextoInteraccion($"Pulsa [E] para abrir el paso a {nombreZona} [Coste: {coste} pts]");
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

    private bool EstaBloqueadaPorHistoria()
    {
        if (!requierePasoHistoria || QuestManager.Instance == null) return false;
        return QuestManager.Instance.indiceTimeline.Value < indiceTimelineDesbloqueo;
    }

    private void MostrarTextoDeBloqueo()
    {
        string misionActual = QuestManager.Instance.idPasoActual.Value.ToString();
        string textoRechazo = "<color=red>[ACCESO DENEGADO]</color> ";

        if (misionActual == "Tarjeta") textoRechazo += "Se requiere Tarjeta de Acceso.";
        else if (misionActual == "Hackeo") textoRechazo += "Se requiere Anulación de Seguridad.";
        else if (misionActual == "BuscarPiezas") textoRechazo += "Encuentra las piezas del bastón primero.";
        else textoRechazo += "Sistemas de energía inestables.";

        uiManagerLocal.MostrarTextoInteraccion(textoRechazo);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ComprarPuertaServerRpc(ulong idCuerpo)
    {
        // En lugar del ID de conexión, usamos la lógica anterior pero mandando el idCuerpo al ClientRpc
        // Buscamos al comprador mirando a quién pertenece este cuerpo para quitarle el dinero
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idCuerpo, out NetworkObject objetoJugador))
        {
            SistemaPuntosFPS bolsillo = objetoJugador.GetComponentInChildren<SistemaPuntosFPS>();
            if (bolsillo == null) return;

            if (bolsillo.IntentarComprar(coste))
            {
                estaAbierta.Value = true;

                foreach (ZonaZombies zona in zonasADesbloquear)
                {
                    if (zona != null) zona.estaActiva = true;
                }

                // 1. Mandamos el evento de audio
                EventosDeAudioClientRpc(idCuerpo, transform.position);

                // 2. Destruimos la puerta directamente
                NetworkObject netObj = GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned) netObj.Despawn();
            }
        }
    }

    [ClientRpc]
    private void EventosDeAudioClientRpc(ulong idCuerpo, Vector3 posicionPuerta)
    {
        if (sonidoAbrirPuerta != null)
        {
            AudioSource.PlayClipAtPoint(sonidoAbrirPuerta, posicionPuerta);
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idCuerpo, out NetworkObject objetoJugador))
        {
            SistemaVoces voces = objetoJugador.GetComponent<SistemaVoces>();

            // Solo el dueño tira el dado para hablar
            if (voces != null && voces.IsOwner)
            {
                if (Random.value <= probabilidadFrase)
                {
                    voces.ReproducirFrase(SistemaVoces.TipoVoz.AbrirPuertas);
                }
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