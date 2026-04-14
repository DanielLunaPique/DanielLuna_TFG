using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class QuestManager : NetworkBehaviour
{
    public static QuestManager Instance;

    [Header("Configuración")]
    public QuestTimeline timelineActiva;

    // Variables de Red sincronizadas
    public NetworkVariable<int> indiceTimeline = new NetworkVariable<int>(0);
    public NetworkVariable<FixedString128Bytes> textoHUDActual = new NetworkVariable<FixedString128Bytes>("");

    public QuestStep pasoActivo;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) DeterminarSiguientePaso();

        // Cuando el servidor cambie el texto, el cliente actualiza SU UIManager
        textoHUDActual.OnValueChanged += (oldV, newV) => {
            if (UIManager.Instance != null)
                UIManager.Instance.ActualizarTextoObjetivo(newV.ToString());
        };

        // Forzar el texto al aparecer por primera vez
        if (UIManager.Instance != null)
            UIManager.Instance.ActualizarTextoObjetivo(textoHUDActual.Value.ToString());
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotificarPasoCompletadoServerRpc(string idCompletado)
    {
        // Solo avanzamos si el ID coincide con la misión que el servidor tiene activa
        if (pasoActivo != null && pasoActivo.ID == idCompletado)
        {
            Debug.Log($"[EasterEgg] Paso '{idCompletado}' completado con éxito.");
            indiceTimeline.Value++;
            DeterminarSiguientePaso();
        }
    }

    private void DeterminarSiguientePaso()
    {
        if (!IsServer) return;

        // ¿Hemos llegado al final de la línea de tiempo?
        if (indiceTimeline.Value >= timelineActiva.secuencia.Count)
        {
            textoHUDActual.Value = "¡ESCAPE DISPONIBLE!";
            pasoActivo = null;
            return;
        }

        ElementoTimeline elemento = timelineActiva.secuencia[indiceTimeline.Value];

        // Probabilidad de que esta casilla se active (útil para misiones opcionales)
        if (Random.Range(0, 100) > elemento.probabilidadDeActivacion)
        {
            indiceTimeline.Value++;
            DeterminarSiguientePaso();
            return;
        }

        if (elemento.tipo == ElementoTimeline.TipoElemento.Fijo)
        {
            pasoActivo = elemento.pasoFijo;
        }
        else
        {
            // ELEGIMOS UNA AL AZAR del pool que tú has configurado para este hueco
            if (elemento.poolAleatorio.Count > 0)
            {
                int r = Random.Range(0, elemento.poolAleatorio.Count);
                pasoActivo = elemento.poolAleatorio[r];
            }
        }

        // Actualizamos el texto para que todos los jugadores lo vean en su HUD
        if (pasoActivo != null)
            textoHUDActual.Value = pasoActivo.textoHUD;
    }
}