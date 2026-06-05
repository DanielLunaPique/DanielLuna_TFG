using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AltarRitual : NetworkBehaviour
{
    [Header("Configuración del Evento")]
    public string idPasoRequerido = "MontarLanza";
    public float duracionLockdown = 40f;
    public AudioClip sonidoInicioRitual;
    public AudioClip sonidoFinRitual;
    public AudioSource fuenteAudio;

    [Header("Referencias Visuales y Físicas")]
    public GameObject barreraLockdown;
    public Transform puntoAparicionBaston;
    public GameObject prefabBastonVisual;

    [Header("Lógica de Arma (Estilo Caja/Pared)")]
    public EstadisticasArma statsLanza;

    private GameObject visualLanzaEnAltar;

    [Header("Zonas de Peligro")]
    public List<PuntoSpawnZombie> spawnsExclusivosAltar;

    private NetworkVariable<bool> ritualIniciado = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> ritualCompletado = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> lanzaDisponibleEnAltar = new NetworkVariable<bool>(false);

    private bool jugadorCerca = false;
    private UIManager uiLocalJugador;

    private GameObject bastonVisualInstanciado;

    private void Start()
    {
        if (barreraLockdown != null) barreraLockdown.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            jugadorCerca = true;
            uiLocalJugador = other.GetComponentInChildren<UIManager>();
            ActualizarUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            jugadorCerca = false;
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();
        }
    }

    private void ActualizarUI()
    {
        if (uiLocalJugador == null) return;

        if (!ritualIniciado.Value)
        {
            if (QuestManager.Instance != null && QuestManager.Instance.idPasoActual.Value.ToString() == idPasoRequerido)
                uiLocalJugador.MostrarTextoInteraccion("Pulsa [E] para forjar el Bastón.");
        }
        else if (ritualCompletado.Value)
        {
            InventarioArmas invLocal = null;
            if (NetworkManager.Singleton.LocalClient != null && NetworkManager.Singleton.LocalClient.PlayerObject != null)
            {
                // CORRECCIÓN: Usamos InChildren para la UI también
                invLocal = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponentInChildren<InventarioArmas>();
            }

            if (lanzaDisponibleEnAltar.Value)
            {
                uiLocalJugador.MostrarTextoInteraccion("Pulsa [E] para coger el Bastón.");
            }
            else if (invLocal != null && invLocal.TieneArma(statsLanza))
            {
                // Contamos cuántas armas tenemos en total
                int cantidadArmas = 0;
                foreach (var arma in invLocal.armasEquipadas) if (arma != null) cantidadArmas++;

                if (cantidadArmas > 1)
                {
                    uiLocalJugador.MostrarTextoInteraccion("Pulsa [E] para depositar y recargar el Bastón.");
                }
                else
                {
                    uiLocalJugador.MostrarTextoInteraccion("No puedes soltar tu única arma.");
                }
            }
            else
            {
                uiLocalJugador.OcultarTextoInteraccion();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void IntercambiarBastonServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        var playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        InventarioArmas inv = playerObject.GetComponentInChildren<InventarioArmas>();
        if (inv == null) return;

        ClientRpcParams parametrosPrivados = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
        };

        if (lanzaDisponibleEnAltar.Value)
        {
            RecibirArmaClientRpc(parametrosPrivados);
            AparecerLanzaEnAltar(false);
        }
        else if (inv.TieneArma(statsLanza))
        {
            // El servidor también comprueba que no sea tu única arma por seguridad
            int cantidadArmas = 0;
            foreach (var arma in inv.armasEquipadas) if (arma != null) cantidadArmas++;

            if (cantidadArmas > 1)
            {
                SoltarArmaClientRpc(parametrosPrivados);
                AparecerLanzaEnAltar(true);
            }
        }
    }

    [ClientRpc]
    private void SoltarArmaClientRpc(ClientRpcParams rpcParams = default)
    {
        var miJugador = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (miJugador != null)
        {
            InventarioArmas miInventario = miJugador.GetComponentInChildren<InventarioArmas>();
            if (miInventario != null)
            {
                // Llamamos a nuestra nueva función limpia
                miInventario.QuitarArma(statsLanza);
            }
        }
    }

    private void Update()
    {
        if (!jugadorCerca || !Input.GetKeyDown(KeyCode.E)) return;

        if (!ritualIniciado.Value)
        {
            if (QuestManager.Instance.idPasoActual.Value.ToString() == idPasoRequerido)
            {
                uiLocalJugador.OcultarTextoInteraccion();
                IniciarRitualServerRpc();
            }
        }
        else if (ritualCompletado.Value)
        {
            IntercambiarBastonServerRpc();

            // Actualizamos la UI inmediatamente para que el texto cambie rápido
            Invoke(nameof(ActualizarUI), 0.1f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void IniciarRitualServerRpc()
    {
        if (ritualIniciado.Value) return;
        ritualIniciado.Value = true;

        ActivarBarrerasClientRpc(true);

        if (prefabBastonVisual != null && puntoAparicionBaston != null)
        {
            bastonVisualInstanciado = Instantiate(prefabBastonVisual, puntoAparicionBaston.position, puntoAparicionBaston.rotation);
            bastonVisualInstanciado.GetComponent<NetworkObject>().Spawn(true);
        }

        if (GameManager.Instance != null)
            GameManager.Instance.IniciarLockdownDeSupervivencia(duracionLockdown, spawnsExclusivosAltar, this);
    }

    public void CompletarRitual()
    {
        if (!IsServer) return;
        ritualCompletado.Value = true;

        ActivarBarrerasClientRpc(false);

        if (bastonVisualInstanciado != null)
            bastonVisualInstanciado.GetComponent<NetworkObject>().Despawn(true);

        AparecerLanzaEnAltar(true);

        QuestManager.Instance.NotificarPasoCompletadoServerRpc(idPasoRequerido);
    }

    private void AparecerLanzaEnAltar(bool aparecer)
    {
        if (!IsServer) return;

        lanzaDisponibleEnAltar.Value = aparecer;
        GestionarVisualLanzaClientRpc(aparecer);
    }

    [ClientRpc]
    private void GestionarVisualLanzaClientRpc(bool mostrar)
    {
        // CORRECCIÓN: Usamos prefabVisualCaja para que sea un simple modelo 3D estético
        if (mostrar && visualLanzaEnAltar == null && statsLanza.prefabVisualCaja != null)
        {
            visualLanzaEnAltar = Instantiate(statsLanza.prefabVisualCaja, puntoAparicionBaston.position, puntoAparicionBaston.rotation);
            visualLanzaEnAltar.transform.SetParent(puntoAparicionBaston);
        }

        if (visualLanzaEnAltar != null) visualLanzaEnAltar.SetActive(mostrar);

        if (jugadorCerca) ActualizarUI();
    }

    [ClientRpc]
    private void RecibirArmaClientRpc(ClientRpcParams rpcParams = default)
    {
        // Usamos la misma lógica exacta y segura de tu EntregarArmaAlInventarioClientRpc
        var miJugador = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (miJugador != null)
        {
            InventarioArmas miInventario = miJugador.GetComponentInChildren<InventarioArmas>();
            if (miInventario != null)
            {
                miInventario.RecibirNuevaArma(statsLanza);
            }
        }
    }

    [ClientRpc]
    private void ActivarBarrerasClientRpc(bool activar)
    {
        if (barreraLockdown != null) barreraLockdown.SetActive(activar);
        if (activar && sonidoInicioRitual != null) fuenteAudio.PlayOneShot(sonidoInicioRitual);
        else if (!activar && sonidoFinRitual != null) AudioSource.PlayClipAtPoint(sonidoFinRitual, transform.position);
    }
}