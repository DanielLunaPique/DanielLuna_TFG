using UnityEngine;
using Unity.Netcode;

public class PiezaLanza : NetworkBehaviour
{
    public string idMisionRequerida = "BuscarPiezas";
    public AudioClip sonidoRecogida;

    private bool jugadorCerca = false;
    private UIManager uiLocal;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            // Solo mostramos el mensaje si el QuestManager dice que toca buscar piezas
            if (QuestManager.Instance.idPasoActual.Value.ToString() == idMisionRequerida)
            {
                jugadorCerca = true;
                uiLocal = other.GetComponentInChildren<UIManager>();
                if (uiLocal != null) uiLocal.MostrarTextoInteraccion("Pulsa [E] para recoger pieza del arma");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            jugadorCerca = false;
            if (uiLocal != null) uiLocal.OcultarTextoInteraccion();
        }
    }

    private void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            RecogerPiezaServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RecogerPiezaServerRpc()
    {
        // 1. Avisamos al contador global
        QuestStepRecolectar.Instance.NotificarPiezaRecogidaServer();

        // 2. Reproducimos sonido y destruimos el objeto en red
        if (sonidoRecogida != null)
        {
            // Aquí podrías usar un ClientRpc para el sonido, o PlayClipAtPoint
            AudioSource.PlayClipAtPoint(sonidoRecogida, transform.position);
        }

        uiLocal.OcultarTextoInteraccion();

        // Desaparece de la red para todos
        GetComponent<NetworkObject>().Despawn(true);
    }
}