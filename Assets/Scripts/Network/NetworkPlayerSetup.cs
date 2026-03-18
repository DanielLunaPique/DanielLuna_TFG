using Unity.Netcode;
using UnityEngine;

public class NetworkPlayerSetup : NetworkBehaviour
{
    [Header("Cosas a desactivar en los demás")]
    public GameObject camaraObjeto;   // Arrastra el objeto 'Main Camera' (hijo del jugador)
    //public AudioListener audioListener; // Arrastra el componente AudioListener

    [Header("Componentes a controlar")]
    public CharacterController controller; // Necesario para poder teletransportar al nacer

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // --- SI SOY YO (EL DUEÑO) ---

            // 1. Activar mi cámara y audio
            if (camaraObjeto != null) camaraObjeto.SetActive(true);
            //if (audioListener != null) audioListener.enabled = true;

            // 2. Moverme al punto de Spawn
            MoverAlSpawn();
        }
        else
        {
            // --- SI ES OTRO JUGADOR (CLON) ---

            // 1. Apagar su cámara para no ver a través de ella
            if (camaraObjeto != null) camaraObjeto.SetActive(false);
            //if (audioListener != null) audioListener.enabled = false;
        }
    }

    void MoverAlSpawn()
    {
        // Buscamos el objeto que tenga la etiqueta "Respawn"
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("Respawn");

        if (spawnPoint != null)
        {
            // Truco: Desactivar el CharacterController un milisegundo para teletransportar
            // (Si no lo desactivas, a veces Unity bloquea el teletransporte)
            if (controller != null) controller.enabled = false;

            transform.position = spawnPoint.transform.position;
            transform.rotation = spawnPoint.transform.rotation;

            if (controller != null) controller.enabled = true;
        }
        else
        {
            Debug.LogWarning("¡No se encontró ningún objeto con el Tag 'Respawn'!");
        }
    }
}