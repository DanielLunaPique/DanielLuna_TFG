using UnityEngine;
using Unity.Netcode;

public class RadioInteractuable : NetworkBehaviour
{
    [Header("Configuración de Audio")]
    public AudioSource fuenteAudio;
    public AudioClip pistaRadio;

    [Tooltip("Si es true, la radio ya no se podrá volver a encender una vez termine.")]
    public bool reproducirSoloUnaVez = true;

    private bool yaActivada = false;
    private bool jugadorEnRango = false;

    // Referencia temporal al UI del jugador local que entra en el trigger
    private UIManager uiJugadorLocal;

    private void Start()
    {
        if (fuenteAudio == null) fuenteAudio = GetComponent<AudioSource>();

        // Nos aseguramos de que el audio sea posicional (3D)
        fuenteAudio.spatialBlend = 1f;
    }

    private void Update()
    {
        // Solo el cliente local que está físicamente frente a la radio puede darle a la E
        if (jugadorEnRango && Input.GetKeyDown(KeyCode.E))
        {
            if (reproducirSoloUnaVez && yaActivada) return;

            // Avisamos al servidor para que encienda la radio en todas las pantallas
            ActivarRadioServerRpc();

            // Limpiamos la UI local porque ya hemos interactuado
            if (uiJugadorLocal != null)
            {
                uiJugadorLocal.MostrarTextoInteraccion(""); // Ocultamos el texto
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo nos interesa si lo que entra es el jugador local (nuestro propio personaje)
        if (other.CompareTag("Player"))
        {
            NetworkObject netObj = other.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                if (reproducirSoloUnaVez && yaActivada) return;

                jugadorEnRango = true;

                // Buscamos su UI y mostramos el texto
                uiJugadorLocal = other.GetComponentInChildren<UIManager>(true);
                if (uiJugadorLocal != null)
                {
                    uiJugadorLocal.MostrarTextoInteraccion("Presiona [E] para encender la radio");
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
                jugadorEnRango = false;

                // Ocultamos el texto al salir del rango si no le dimos a la E
                if (uiJugadorLocal != null)
                {
                    uiJugadorLocal.MostrarTextoInteraccion("");
                    uiJugadorLocal = null;
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivarRadioServerRpc()
    {
        // El servidor recibe la petición y manda la orden de Play a todos
        ActivarRadioClientRpc();
    }

    [ClientRpc]
    private void ActivarRadioClientRpc()
    {
        yaActivada = true;

        if (fuenteAudio != null && pistaRadio != null)
        {
            fuenteAudio.clip = pistaRadio;
            fuenteAudio.Play();
        }
    }
}