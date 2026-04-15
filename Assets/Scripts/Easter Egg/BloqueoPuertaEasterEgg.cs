using UnityEngine;
using Unity.Netcode;

public class BloqueoPuertaEasterEgg : NetworkBehaviour
{
    [Header("Configuración")]
    [Tooltip("El índice de la Línea de Tiempo en el que se desbloquea esta puerta (Ej: 2)")]
    public int indiceTimelineDesbloqueo = 2;

    [Tooltip("Arrastra aquí el script original de tu puerta para apagarlo temporalmente")]
    public MonoBehaviour scriptPuertaOriginal; // Puede ser tu ComprarPuerta.cs

    private bool jugadorCerca = false;
    private UIManager uiLocalJugador;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            jugadorCerca = true;
            uiLocalJugador = other.GetComponentInChildren<UIManager>();
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
        if (QuestManager.Instance == null) return;

        // 1. ¿Hemos superado el bloqueo?
        bool estaBloqueada = QuestManager.Instance.indiceTimeline.Value < indiceTimelineDesbloqueo;

        // Apagamos o encendemos el script de compra real
        if (scriptPuertaOriginal != null)
        {
            scriptPuertaOriginal.enabled = !estaBloqueada;
        }

        // 2. Si ya no está bloqueada, este script deja de molestar
        if (!estaBloqueada) return;

        // 3. Si sigue bloqueada y el jugador está cerca, mostramos el motivo
        if (jugadorCerca && uiLocalJugador != null)
        {
            string misionActual = QuestManager.Instance.idPasoActual.Value.ToString();
            string textoRechazo = "[BLOQUEADO] ";

            // El texto muta según lo que haya tocado en el pool aleatorio
            if (misionActual == "Tarjeta")
                textoRechazo += "Se requiere Tarjeta de Acceso.";
            else if (misionActual == "Hackeo")
                textoRechazo += "Se requiere Anulación de Seguridad.";
            else if (misionActual == "EncontrarBaston")
                textoRechazo += "Encuentra las piezas del baston primero";
            else
                textoRechazo += "Sistemas de energía inestables."; // Mensaje por defecto (Ej: Si están en el Paso 1)

            uiLocalJugador.MostrarTextoInteraccion(textoRechazo);
        }
    }
}