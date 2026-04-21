using UnityEngine;
using Unity.Netcode;

public class QuestStepRecolectar : NetworkBehaviour
{
    public static QuestStepRecolectar Instance;

    [Header("Configuración")]
    public string idMisionAsociada = "BuscarPiezas";
    public int piezasTotales = 3;

    // Esta variable está sincronizada en toda la red automáticamente
    public NetworkVariable<int> piezasRecogidas = new NetworkVariable<int>(0);

    private void Awake() { Instance = this; }

    public override void OnNetworkSpawn()
    {
        // Si el servidor se inicia y ya estamos en este paso por algún motivo, inicializamos
        if (IsServer) piezasRecogidas.Value = 0;
    }

    // El servidor recibe la notificación de que una pieza ha sido recogida
    public void NotificarPiezaRecogidaServer()
    {
        if (!IsServer) return;

        // Solo contamos si estamos en el paso correcto
        if (QuestManager.Instance.idPasoActual.Value.ToString() == idMisionAsociada)
        {
            piezasRecogidas.Value++;
            Debug.Log($"[Quest] Pieza recogida. Progreso: {piezasRecogidas.Value}/{piezasTotales}");

            // Comprobamos si hemos terminado
            if (piezasRecogidas.Value >= piezasTotales)
            {
                FinalizarPaso();
            }
        }
    }

    private void FinalizarPaso()
    {
        Debug.Log("[Quest] ¡Todas las piezas recogidas! Avanzando misión...");
        QuestManager.Instance.NotificarPasoCompletadoServerRpc(idMisionAsociada);
    }
}