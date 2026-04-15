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
    // AÑADIMOS ESTA: Para que los clientes sepan el ID de la misión activa
    public NetworkVariable<FixedString32Bytes> idPasoActual = new NetworkVariable<FixedString32Bytes>("");

    private QuestStep pasoActivo; // Solo servidor

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) DeterminarSiguientePaso();

        // Cada vez que cambie el texto, actualizamos el HUD
        textoHUDActual.OnValueChanged += (oldV, newV) => {
            ActualizarHUDLocal(newV.ToString());
        };

        // Forzamos actualización inicial
        ActualizarHUDLocal(textoHUDActual.Value.ToString());
    }

    private void ActualizarHUDLocal(string texto)
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ActualizarTextoObjetivo(texto);
    }

    // Si el UIManager tarda en cargar, él mismo llamará aquí al Start
    public void ForzarActualizacionHUD()
    {
        ActualizarHUDLocal(textoHUDActual.Value.ToString());
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotificarPasoCompletadoServerRpc(string idCompletado)
    {
        if (pasoActivo != null && pasoActivo.ID == idCompletado)
        {
            indiceTimeline.Value++;
            DeterminarSiguientePaso();
        }
    }

    private void DeterminarSiguientePaso()
    {
        if (!IsServer) return;

        if (indiceTimeline.Value >= timelineActiva.secuencia.Count)
        {
            textoHUDActual.Value = "¡ESCAPE DISPONIBLE!";
            idPasoActual.Value = "FIN";
            pasoActivo = null;
            return;
        }

        ElementoTimeline elemento = timelineActiva.secuencia[indiceTimeline.Value];

        if (Random.Range(0, 100) > elemento.probabilidadDeActivacion)
        {
            indiceTimeline.Value++;
            DeterminarSiguientePaso();
            return;
        }

        if (elemento.tipo == ElementoTimeline.TipoElemento.Fijo)
            pasoActivo = elemento.pasoFijo;
        else
            pasoActivo = elemento.poolAleatorio[Random.Range(0, elemento.poolAleatorio.Count)];

        if (pasoActivo != null)
        {
            textoHUDActual.Value = pasoActivo.textoHUD;
            idPasoActual.Value = pasoActivo.ID; // Sincronizamos el ID con los clientes
        }
    }
}