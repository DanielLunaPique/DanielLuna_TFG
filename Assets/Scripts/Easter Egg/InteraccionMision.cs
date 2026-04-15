using UnityEngine;
using Unity.Netcode;

public class InteraccionMision : NetworkBehaviour
{
    public string idMisionAsociada = "Energia";
    private bool jugadorCerca = false;
    private UIManager uiLocalJugador;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            // Solo intentamos mostrar si es el paso correcto
            if (QuestManager.Instance.idPasoActual.Value.ToString() == idMisionAsociada)
            {
                jugadorCerca = true;
                uiLocalJugador = other.GetComponentInChildren<UIManager>();
                if (uiLocalJugador != null)
                    uiLocalJugador.MostrarTextoInteraccion($"Pulsa [E] para {idMisionAsociada}");
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
        // SEGURIDAD: Si el paso cambia mientras estamos cerca, ocultamos el texto
        if (jugadorCerca && QuestManager.Instance.idPasoActual.Value.ToString() != idMisionAsociada)
        {
            jugadorCerca = false;
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();
            return;
        }

        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();
            CompletarAccionServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CompletarAccionServerRpc()
    {
        QuestManager.Instance.NotificarPasoCompletadoServerRpc(idMisionAsociada);
    }
}