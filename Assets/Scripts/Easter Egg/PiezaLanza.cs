using UnityEngine;
using Unity.Netcode;

public class PiezaLanza : NetworkBehaviour
{
    public string idMisionRequerida = "BuscarPiezas";

    [Header("Audio del Mundo (Físico)")]
    public AudioClip sonidoRecogida;

    [Header("Audio del Personaje (Voz)")]
    [Tooltip("La frase que dirá el jugador al recoger esta pieza específica")]
    public AudioClip frasePersonajeAlRecoger;

    private bool jugadorCerca = false;
    private UIManager uiLocal;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
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
            if (uiLocal != null) uiLocal.OcultarTextoInteraccion();

            // --- MATRÍCULA DEL CUERPO ---
            ulong miCuerpoID = NetworkManager.Singleton.LocalClient.PlayerObject.NetworkObjectId;
            RecogerPiezaServerRpc(miCuerpoID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RecogerPiezaServerRpc(ulong idCuerpo)
    {
        QuestStepRecolectar.Instance.NotificarPiezaRecogidaServer();

        // 1. Lanzamos el combo de audios
        EventosDeAudioClientRpc(idCuerpo, transform.position);

        // 2. Destruimos la pieza
        GetComponent<NetworkObject>().Despawn(true);
    }

    [ClientRpc]
    private void EventosDeAudioClientRpc(ulong idCuerpo, Vector3 posicion)
    {
        if (sonidoRecogida != null)
        {
            AudioSource.PlayClipAtPoint(sonidoRecogida, posicion);
        }

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idCuerpo, out NetworkObject objetoJugador))
        {
            SistemaVoces voces = objetoJugador.GetComponent<SistemaVoces>();
            if (voces != null)
            {
                voces.ReproducirFraseEspecificaVip(frasePersonajeAlRecoger);
            }
        }
    }
}