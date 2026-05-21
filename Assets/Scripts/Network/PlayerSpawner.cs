using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [Header("Configuración")]
    [Tooltip("Arrastra aquí el Prefab de tu jugador (el que tiene el NetworkObject)")]
    public GameObject playerPrefab;

    public override void OnNetworkSpawn()
    {
        // Solo el Servidor/Host tiene el poder de instanciar objetos en la red
        if (!IsServer) return;

        // Nos suscribimos al evento de Netcode que avisa cuando cambian las escenas
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
        }
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        // Comprobamos si el evento es que un cliente ha terminado de cargar la escena con éxito
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            SpawnPlayerForClient(sceneEvent.ClientId);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        // 1. Buscar el punto de spawn en la escena actual
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("Respawn");
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (spawnPoint != null)
        {
            spawnPos = spawnPoint.transform.position;
            spawnRot = spawnPoint.transform.rotation;
        }
        else
        {
            Debug.LogWarning("[SPAWNER] ¡No se encontró ningún objeto con el Tag 'Respawn'! El jugador nacerá en el origen.");
        }

        // 2. Instanciar el prefab del jugador en la posición correcta físicamente
        GameObject playerInstance = Instantiate(playerPrefab, spawnPos, spawnRot);

        // 3. Hacer el Spawn en red asignándole la propiedad al cliente correspondiente
        // Esto hace que aparezca en las pantallas de todos y que el cliente sea el 'Owner'
        playerInstance.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        Debug.Log($"[SPAWNER] Jugador generado con éxito para el Cliente ID: {clientId} en la posición {spawnPos}");
    }

    public override void OnNetworkDespawn()
    {
        // Limpieza de eventos al salir de la partida para evitar errores
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
        }
    }
}