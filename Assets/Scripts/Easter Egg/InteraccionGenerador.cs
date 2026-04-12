using UnityEngine;
using Unity.Netcode;

public class InteraccionGenerador : NetworkBehaviour
{
    [Header("Configuración")]
    [Tooltip("Debe llamarse EXACTAMENTE igual que el Nombre Mision del ScriptableObject")]
    public string nombrePasoRequerido = "DarEnergia";

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
                uiLocalJugador = other.GetComponentInChildren<UIManager>();

                if (uiLocalJugador != null)
                {
                    uiLocalJugador.MostrarTextoInteraccion("Pulsa [E] para encender Generador");
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
        // Si estoy cerca y pulso la E...
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();

            // ...le mando un WhatsApp al servidor
            ActivarGeneradorServerRpc();
        }
    }

    // El servidor recibe la petición y avisa al QuestManager
    [ServerRpc(RequireOwnership = false)]
    private void ActivarGeneradorServerRpc()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.IntentarAvanzarMision(nombrePasoRequerido);
        }
    }
}