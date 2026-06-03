using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PataDeCabra : NetworkBehaviour
{
    [Header("Audio del Mundo (Físico)")]
    public AudioClip sonidoRecoger;

    [Header("Audio del Personaje (Voz)")]
    [Tooltip("La frase que dirá el jugador al recoger la pata de cabra")]
    public AudioClip frasePersonajeAlRecoger;

    private bool jugadorCerca = false;
    private UIManager uiManagerLocal;

    void Update()
    {
        if (jugadorCerca && Input.GetKeyDown(KeyCode.E))
        {
            if (uiManagerLocal != null) uiManagerLocal.OcultarTextoInteraccion();

            // --- LA CORRECCIÓN CLAVE ---
            // Buscamos el ID único del objeto de nuestro jugador, no el ID de nuestra conexión
            ulong miCuerpoID = NetworkManager.Singleton.LocalClient.PlayerObject.NetworkObjectId;

            RecogerServerRpc(miCuerpoID);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RecogerServerRpc(ulong idCuerpo)
    {
        GameManager.Instance.pataDeCabraDesbloqueada.Value = true;
        Debug.Log("[Servidor] ¡Un jugador ha encontrado la Pata de Cabra!");

        // 1. Lanzamos el combo de audios usando la matrícula del cuerpo
        EventosDeAudioClientRpc(idCuerpo, transform.position);

        // 2. Apagamos el objeto visualmente
        OcultarObjetoClientRpc();

        // 3. Esperamos a que los audios se procesen antes de destruirlo de la red
        StartCoroutine(DespawnSeguro());
    }

    [ClientRpc]
    private void EventosDeAudioClientRpc(ulong idCuerpo, Vector3 posicion)
    {
        // 1. Sonido Físico (Clinc metálico)
        if (sonidoRecoger != null)
        {
            AudioSource.PlayClipAtPoint(sonidoRecoger, posicion);
        }

        // 2. Sonido Vocal (Busca al jugador por la matrícula)
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(idCuerpo, out NetworkObject objetoJugador))
        {
            SistemaVoces voces = objetoJugador.GetComponent<SistemaVoces>();
            if (voces != null)
            {
                voces.ReproducirFraseEspecificaVip(frasePersonajeAlRecoger);
            }
            else
            {
                Debug.LogWarning("[DEBUG AUDIO] Se encontró al jugador, pero no tiene el componente 'SistemaVoces'.");
            }
        }
        else
        {
            Debug.LogWarning($"[DEBUG AUDIO] No se encontró ningún cuerpo en la red con el ID: {idCuerpo}");
        }
    }

    [ClientRpc]
    private void OcultarObjetoClientRpc()
    {
        if (TryGetComponent(out Collider col)) col.enabled = false;
        foreach (var render in GetComponentsInChildren<Renderer>()) render.enabled = false;
    }

    private IEnumerator DespawnSeguro()
    {
        yield return new WaitForSeconds(1.5f);

        if (IsServer && GetComponent<NetworkObject>().IsSpawned)
        {
            GetComponent<NetworkObject>().Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorCerca = true;
            uiManagerLocal = netObj.GetComponentInChildren<UIManager>();
            if (uiManagerLocal != null) uiManagerLocal.MostrarTextoInteraccion("Pulsa [E] para recoger Pata de Cabra");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.IsLocalPlayer)
        {
            jugadorCerca = false;
            if (uiManagerLocal != null)
            {
                uiManagerLocal.OcultarTextoInteraccion();
                uiManagerLocal = null;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (uiManagerLocal != null)
        {
            uiManagerLocal.OcultarTextoInteraccion();
            uiManagerLocal = null;
        }
    }
}