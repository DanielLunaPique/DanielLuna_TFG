using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public class GestorPosicionesTarjeta : NetworkBehaviour
{
    public static GestorPosicionesTarjeta Instance;

    [Header("Configuración")]
    public GameObject prefabTarjetaFisica; // La tarjeta que tienes escondida en la escena
    public Transform[] puntosDeSpawn; // Los 4 sitios posibles

    private void Awake() { Instance = this; }

    public override void OnNetworkSpawn()
    {
        // Solo el Servidor se encarga de mover los objetos reales
        if (IsServer)
        {
            // Nos suscribimos a la radio del QuestManager
            QuestManager.Instance.idPasoActual.OnValueChanged += AlCambiarMision;

            // Comprobamos por si la misión ya había empezado antes de cargar este objeto
            AlCambiarMision("", QuestManager.Instance.idPasoActual.Value);
        }
    }

    private void AlCambiarMision(FixedString32Bytes idViejo, FixedString32Bytes idNuevo)
    {
        // Si el Director dice "Tarjeta" y la tarjeta aún no está encendida...
        if (idNuevo.ToString() == "Tarjeta")
        {
            AparecerTarjetaAleatoria();
        }
    }

    private void AparecerTarjetaAleatoria()
    {
        if (!IsServer) return; // Doble seguridad

        int indice = Random.Range(0, puntosDeSpawn.Length);

        // 1. Creamos la tarjeta físicamente en el servidor
        GameObject nuevaTarjeta = Instantiate(prefabTarjetaFisica, puntosDeSpawn[indice].position, puntosDeSpawn[indice].rotation);

        // 2. MAGIA DE NETCODE: Le decimos a todos los jugadores que esta tarjeta existe
        nuevaTarjeta.GetComponent<NetworkObject>().Spawn(true);

        Debug.Log($"[EasterEgg] Tarjeta GENERADA EN RED en la ubicación {indice}");
    }

    public override void OnDestroy()
    {
        if (IsServer && QuestManager.Instance != null)
        {
            QuestManager.Instance.idPasoActual.OnValueChanged -= AlCambiarMision;
        }
        base.OnDestroy();
    }
}