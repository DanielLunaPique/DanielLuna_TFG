using UnityEngine;
using Unity.Netcode;

public class InteraccionBotonSimon : NetworkBehaviour
{
    [Header("Configuración")]
    public MinijuegoSimonDice gestorPrincipal;
    [Tooltip("El número de este botón (0, 1, 2 o 3)")]
    public int indiceDeEsteBoton;

    [Header("Visuales")]
    [Tooltip("El material brillante específico para este botón (Rojo, Azul, etc)")]
    public Material materialEncendido;

    private bool jugadorCerca = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            jugadorCerca = true;
            other.GetComponentInChildren<UIManager>().MostrarTextoInteraccion("Pulsa [E] para activar nodo");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            jugadorCerca = false;
            other.GetComponentInChildren<UIManager>().OcultarTextoInteraccion();
        }
    }

    private void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            // Le mandamos la pulsación al servidor
            PulsarBotonServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PulsarBotonServerRpc()
    {
        if (gestorPrincipal != null)
        {
            gestorPrincipal.RecibirPulsacion(indiceDeEsteBoton);
        }
    }
}