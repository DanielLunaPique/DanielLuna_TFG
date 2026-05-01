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

    [Header("Referencias Visuales y Físicas")]
    public GameObject barreraLockdown;
    public Transform puntoAparicionBaston;
    public GameObject prefabBastonVisual;
    public GameObject prefabBastonArma;

    [Header("Zonas de Peligro")]
    // CORRECCIÓN: Ahora usa tu script personalizado
    public List<PuntoSpawnZombie> spawnsExclusivosAltar;

    private NetworkVariable<bool> ritualIniciado = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> ritualCompletado = new NetworkVariable<bool>(false);

    private bool jugadorCerca = false;
    private UIManager uiLocalJugador;
    private GameObject bastonVisualInstanciado;

    private void Start()
    {
        if (barreraLockdown != null) barreraLockdown.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (ritualIniciado.Value || ritualCompletado.Value) return;

        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            // CORRECCIÓN: Comprobamos si estamos en el paso correcto de la historia
            if (QuestManager.Instance == null || QuestManager.Instance.idPasoActual.Value.ToString() != idPasoRequerido)
                return;

            jugadorCerca = true;
            uiLocalJugador = other.GetComponentInChildren<UIManager>();

            if (uiLocalJugador != null)
                uiLocalJugador.MostrarTextoInteraccion("Pulsa [E] para forjar el Bastón.");
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

    private void Update()
    {
        if (jugadorCerca && !ritualIniciado.Value && Input.GetKeyDown(KeyCode.E))
        {
            // Doble comprobación de seguridad antes de iniciar
            if (QuestManager.Instance.idPasoActual.Value.ToString() == idPasoRequerido)
            {
                if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();
                IniciarRitualServerRpc();
            }
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
        {
            // Pasamos nuestra lista de spawns adaptada
            GameManager.Instance.IniciarLockdownDeSupervivencia(duracionLockdown, spawnsExclusivosAltar, this);
        }
    }

    public void CompletarRitual()
    {
        if (!IsServer) return;
        ritualCompletado.Value = true;

        ActivarBarrerasClientRpc(false);

        if (bastonVisualInstanciado != null)
        {
            bastonVisualInstanciado.GetComponent<NetworkObject>().Despawn(true);
        }

        if (prefabBastonArma != null && puntoAparicionBaston != null)
        {
            GameObject armaRecogible = Instantiate(prefabBastonArma, puntoAparicionBaston.position, puntoAparicionBaston.rotation);
            armaRecogible.GetComponent<NetworkObject>().Spawn(true);
        }

        // Notificamos al sistema de misiones que el paso ha terminado
        QuestManager.Instance.NotificarPasoCompletadoServerRpc(idPasoRequerido);
    }

    [ClientRpc]
    private void ActivarBarrerasClientRpc(bool activar)
    {
        if (barreraLockdown != null) barreraLockdown.SetActive(activar);

        if (activar && sonidoInicioRitual != null)
            AudioSource.PlayClipAtPoint(sonidoInicioRitual, transform.position);
        else if (!activar && sonidoFinRitual != null)
            AudioSource.PlayClipAtPoint(sonidoFinRitual, transform.position);
    }
}