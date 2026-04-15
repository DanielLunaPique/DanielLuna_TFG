using UnityEngine;
using Unity.Netcode;

public class InteraccionMision : NetworkBehaviour
{
    [Header("Configuración de Misión")]
    public string idMisionAsociada = "Energia";

    [Header("Feedback (Opcional)")]
    [Tooltip("¿El objeto debe desaparecer al cogerlo? (Sí para Tarjeta, No para Generador)")]
    public bool destruirAlCompletar = false;
    public AudioClip sonidoInteraccion;

    private bool jugadorCerca = false;
    private UIManager uiLocalJugador;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            if (QuestManager.Instance.idPasoActual.Value.ToString() == idMisionAsociada)
            {
                jugadorCerca = true;
                uiLocalJugador = other.GetComponentInChildren<UIManager>();
                if (uiLocalJugador != null)
                    uiLocalJugador.MostrarTextoInteraccion($"Pulsa [E] para interactuar");
            }
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
        if (jugadorCerca && QuestManager.Instance.idPasoActual.Value.ToString() != idMisionAsociada)
        {
            jugadorCerca = false;
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();
            return;
        }

        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();

            // Reproducimos el sonido LOCALMENTE justo en el momento de pulsar para que no haya lag
            if (sonidoInteraccion != null)
            {
                AudioSource.PlayClipAtPoint(sonidoInteraccion, transform.position, 1f);
            }

            CompletarAccionServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CompletarAccionServerRpc()
    {
        QuestManager.Instance.NotificarPasoCompletadoServerRpc(idMisionAsociada);

        // Si es una tarjeta o pata de cabra, el servidor ordena a todos destruirla
        if (destruirAlCompletar)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }
}