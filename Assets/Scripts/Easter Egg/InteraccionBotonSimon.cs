using UnityEngine;
using Unity.Netcode;

public class InteraccionBotonSimon : NetworkBehaviour
{
    [Header("Configuración")]
    public MinijuegoSimonDice gestorPrincipal;
    public int indiceDeEsteBoton;
    public Material materialEncendido;

    [Header("Referencias Visuales")]
    [Tooltip("Arrastra aquí el objeto HIJO que tiene el MeshRenderer de la pantalla")]
    public MeshRenderer mallaPantalla;

    private bool jugadorCerca = false;
    private UIManager uiLocalJugador;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            // Solo mostramos texto si estamos en el paso del Hackeo
            if (QuestManager.Instance.idPasoActual.Value.ToString() == gestorPrincipal.idMisionAsociada)
            {
                jugadorCerca = true;
                uiLocalJugador = other.GetComponentInChildren<UIManager>();
                if (uiLocalJugador != null)
                    uiLocalJugador.MostrarTextoInteraccion("Pulsa [E] para interactuar");
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
        if (!jugadorCerca) return;

        // Si el paso de hackeo termina/cambia, apagamos el texto por seguridad
        if (QuestManager.Instance.idPasoActual.Value.ToString() != gestorPrincipal.idMisionAsociada)
        {
            jugadorCerca = false;
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            PulsarBotonServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PulsarBotonServerRpc()
    {
        if (gestorPrincipal != null)
        {
            gestorPrincipal.IntentarInteractuar(indiceDeEsteBoton);
        }
    }
}