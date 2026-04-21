using UnityEngine;
using Unity.Netcode;

public class InteraccionHangarAnomalia : NetworkBehaviour
{
    [Header("Configuración")]
    public string idMisionAsociada = "BuscarPiezas";
    public GameObject prefabOrbeAnomalia;
    public Transform puntoAparicionOrbe;

    private bool jugadorCerca = false;
    private UIManager uiLocalJugador;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            if (QuestManager.Instance.idPasoActual.Value.ToString() == idMisionAsociada)
            {
                jugadorCerca = true;
                uiLocalJugador = other.GetComponentInChildren<UIManager>();
                ActualizarTextoUI();
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

        // Seguridad: Si cambia el paso de la misión, nos apagamos
        if (QuestManager.Instance.idPasoActual.Value.ToString() != idMisionAsociada)
        {
            jugadorCerca = false;
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();
            return;
        }

        // Actualizamos el texto dinámicamente
        ActualizarTextoUI();

        // Solo permitimos pulsar E si NO hay otra anomalía viva en el mapa
        if (Input.GetKeyDown(KeyCode.E) && FindObjectOfType<AnomaliaLaberinto>() == null)
        {
            if (uiLocalJugador != null) uiLocalJugador.OcultarTextoInteraccion();
            ExtraerAnomaliaServerRpc(NetworkManager.Singleton.LocalClientId);
        }
    }

    private void ActualizarTextoUI()
    {
        if (uiLocalJugador == null) return;

        // Comprobamos si el orbe ya existe en el mundo
        if (FindObjectOfType<AnomaliaLaberinto>() != null)
        {
            uiLocalJugador.MostrarTextoInteraccion("Anomalía ya activa en el sector...");
        }
        else
        {
            uiLocalJugador.MostrarTextoInteraccion("Pulsa [E] para extraer la Anomalía");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ExtraerAnomaliaServerRpc(ulong idJugador)
    {
        // Doble seguridad en el servidor (Race Condition Shield):
        // Por si dos jugadores en hangares distintos pulsan la 'E' en el mismo milisegundo exacto
        if (FindObjectOfType<AnomaliaLaberinto>() != null) return;

        // Instanciamos el orbe en el servidor
        GameObject nuevoOrbe = Instantiate(prefabOrbeAnomalia, puntoAparicionOrbe.position, Quaternion.identity);
        nuevoOrbe.GetComponent<NetworkObject>().Spawn(true);

        // Le decimos al orbe quién lo está escoltando
        nuevoOrbe.GetComponent<AnomaliaLaberinto>().ArrancarEscolta(idJugador);
    }
}