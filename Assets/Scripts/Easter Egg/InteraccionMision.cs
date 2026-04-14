using UnityEngine;
using Unity.Netcode;

public class InteraccionMision : NetworkBehaviour
{
    [Header("Configuración")]
    [Tooltip("Debe coincidir EXACTAMENTE con el campo 'ID' de tu ScriptableObject QuestStep")]
    public string idMisionAsociada = "Energia";

    private bool jugadorCerca = false;
    private UIManager uiLocalJugador;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                jugadorCerca = true;
                // Accedemos a su UIManager local
                uiLocalJugador = other.GetComponentInChildren<UIManager>();

                if (uiLocalJugador != null)
                {
                    uiLocalJugador.MostrarTextoInteraccion($"Pulsa [E] para {idMisionAsociada}");
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
                }
            }
        }
    }

    private void Update()
    {
        if (QuestManager.Instance.pasoActivo == null ||
        QuestManager.Instance.pasoActivo.ID != idMisionAsociada)
        {
            // Opcional: Si el jugador estaba cerca, ocultamos el texto de interacción
            if (jugadorCerca) uiLocalJugador.OcultarTextoInteraccion();
            return;
        }

        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();

            // Llamamos a la nueva función del QuestManager
            CompletarAccionServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CompletarAccionServerRpc()
    {
        if (QuestManager.Instance != null)
        {
            // CAMBIO CLAVE: Usamos el nuevo método del Manager
            QuestManager.Instance.NotificarPasoCompletadoServerRpc(idMisionAsociada);
        }
    }
}