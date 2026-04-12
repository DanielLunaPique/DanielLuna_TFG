using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class QuestManager : NetworkBehaviour
{
    public static QuestManager Instance;

    [Header("Hilo Principal (Main Easter Egg)")]
    [Tooltip("Arrastra aquí los ScriptableObjects de los pasos fijos en orden")]
    public List<QuestStep> pasosPrincipales = new List<QuestStep>();

    // Variable de red para que todos los jugadores sepan en qué paso estamos
    // Se usa un "int" (índice) porque sincronizar textos largos por red pesa mucho
    public NetworkVariable<int> indicePasoActual = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Guardamos la misión activa para no buscarla todo el rato
    private QuestStep misionActiva;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        // Solo el servidor controla el flujo de misiones
        if (IsServer)
        {
            if (pasosPrincipales.Count > 0)
            {
                EmpezarPaso(0);
            }
        }

        // Cada vez que el servidor cambia de paso, los clientes actualizan su HUD
        indicePasoActual.OnValueChanged += AlCambiarDePaso;

        if (pasosPrincipales.Count > 0)
        {
            AlCambiarDePaso(0, indicePasoActual.Value);
        }
    }

    // ==========================================
    // LÓGICA DEL SERVIDOR (El que toma decisiones)
    // ==========================================

    private void EmpezarPaso(int indice)
    {
        if (!IsServer) return;

        if (indice < pasosPrincipales.Count)
        {
            indicePasoActual.Value = indice;
            misionActiva = pasosPrincipales[indice];
            misionActiva.EmpezarMision();
        }
        else
        {
            Debug.Log("<color=cyan>[QuestManager] ¡EASTER EGG COMPLETADO! Iniciar huida a Utopía.</color>");
        }
    }

    private void Update()
    {
        if (!IsServer || misionActiva == null) return;

        // Si la misión necesita comprobar cosas en el tiempo (ej: aguantar 1 minuto), lo hace aquí
        if (!misionActiva.estaCompletada)
        {
            misionActiva.ActualizarMision();
        }
    }

    /// <summary>
    /// Esta función la llamarán los objetos del mapa (ej. La palanca de luz).
    /// </summary>
    public void IntentarAvanzarMision(string nombreAComprobar)
    {
        if (!IsServer || misionActiva == null) return;

        // El objeto pregunta: "¿Soy yo la misión actual?"
        if (misionActiva.nombreMision == nombreAComprobar)
        {
            misionActiva.CompletarMision();

            // Pasamos a la siguiente "perla" del collar
            EmpezarPaso(indicePasoActual.Value + 1);
        }
        else
        {
            Debug.Log($"[QuestManager] El jugador ha intentado interactuar con '{nombreAComprobar}', pero la misión actual es '{misionActiva.nombreMision}'.");
        }
    }

    // ==========================================
    // LÓGICA DE LOS CLIENTES (El HUD y los efectos)
    // ==========================================

    private void AlCambiarDePaso(int viejoIndice, int nuevoIndice)
    {
        if (nuevoIndice < pasosPrincipales.Count)
        {
            string textoNuevoObjetivo = pasosPrincipales[nuevoIndice].textoHUD;
            Debug.Log($"[CLIENTE] HUD Actualizado: {textoNuevoObjetivo}");

            // Buscamos el UIManager del jugador local y le cambiamos el texto
            UIManager uiLocal = FindObjectOfType<UIManager>();
            if (uiLocal != null)
            {
                uiLocal.ActualizarTextoObjetivo(textoNuevoObjetivo);
            }
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            indicePasoActual.OnValueChanged -= AlCambiarDePaso;
        }
        base.OnDestroy();
    }
}