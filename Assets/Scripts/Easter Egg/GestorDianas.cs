using UnityEngine;
using Unity.Netcode;

public class GestorDianas : NetworkBehaviour
{
    [Header("Configuración de Galería")]
    public string idMisionAsociada = "BuscarPiezas";
    public Diana[] misDianas; // Arrastra los HIJOS (Diana.cs)
    public SoporteDiana[] misSoportes; // Arrastra los PADRES (SoporteDiana.cs)

    [Header("Recompensa Global (Sola la última galería)")]
    public GameObject prefabPomoLanza;
    public Transform puntoAparicionPomo;

    // Solo el servidor gestiona el conteo global de galerías completadas
    private static int galeriasTerminadasGlobal = 0;

    // Esta variable sincronizada avisa a las dianas para que empiecen a moverse
    public NetworkVariable<bool> juegoActivo = new NetworkVariable<bool>(false);

    private bool galeriaCompletada = false;
    private bool jugadorCerca = false;
    private UIManager uiLocal;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            if (QuestManager.Instance.idPasoActual.Value.ToString() == idMisionAsociada && !galeriaCompletada)
            {
                jugadorCerca = true;
                uiLocal = other.GetComponentInChildren<UIManager>();
                if (uiLocal != null) uiLocal.MostrarTextoInteraccion("Pulsa [E] para activar galería");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            jugadorCerca = false;
            if (uiLocal != null) uiLocal.OcultarTextoInteraccion();

            // NUEVO: Si el juego estaba activo y el jugador se va, lo reiniciamos
            if (juegoActivo.Value)
            {
                ReiniciarTodoServerRpc();
            }
        }
    }

    private void Update()
    {
        if (!jugadorCerca || galeriaCompletada) return;

        if (Input.GetKeyDown(KeyCode.E) && !juegoActivo.Value)
        {
            if (uiLocal != null) uiLocal.OcultarTextoInteraccion();
            IniciarGaleriaServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void IniciarGaleriaServerRpc()
    {
        Debug.Log($"[GESTOR] Intentando iniciar galería. ¿Estaba activa?: {juegoActivo.Value}");
        if (juegoActivo.Value || galeriaCompletada) return;

        juegoActivo.Value = true;
        Debug.Log($"[GESTOR] ¡JUEGO ACTIVO! Tengo en mi lista: {misSoportes.Length} Soportes y {misDianas.Length} Dianas.");

        if (misSoportes != null) foreach (var soporte in misSoportes) if (soporte != null) soporte.ResetearPosicion();
        if (misDianas != null) foreach (var diana in misDianas) if (diana != null) diana.abatida.Value = false;
    }

    public void ComprobarProgresoServer()
    {
        if (!IsServer || misDianas == null) return;

        int abatidas = 0;
        foreach (var diana in misDianas)
        {
            if (diana != null && diana.abatida.Value) abatidas++;
        }

        // Si se han abatido todas las dianas de esta galería específica
        if (abatidas >= misDianas.Length)
        {
            FinalizarGaleria();
        }
    }

    private void FinalizarGaleria()
    {
        juegoActivo.Value = false;
        galeriaCompletada = true;
        galeriasTerminadasGlobal++;

        Debug.Log($"[CampoTiro] Galería completada ({galeriasTerminadasGlobal}/3).");

        // Si es la última galería de las 3 que tienes (ajusta el número si tienes más)
        if (galeriasTerminadasGlobal >= 3)
        {
            if (prefabPomoLanza != null)
            {
                GameObject pomo = Instantiate(prefabPomoLanza, puntoAparicionPomo.position, Quaternion.identity);
                pomo.GetComponent<NetworkObject>().Spawn(true);
            }
            // Reiniciamos por si se quiere jugar otra vez en otra partida
            galeriasTerminadasGlobal = 0;
        }
    }

    // --- NUEVO: Función para forzar el reinicio completo si el jugador se aleja ---
    [ServerRpc(RequireOwnership = false)]
    private void ReiniciarTodoServerRpc()
    {
        if (!IsServer) return; // Solo el servidor apaga y reinicia

        juegoActivo.Value = false;

        // Resetear dianas y soportes para dejarlos en el estado inicial de "espera"
        if (misSoportes != null) foreach (var soporte in misSoportes) if (soporte != null) soporte.ResetearPosicion();
        if (misDianas != null) foreach (var diana in misDianas) if (diana != null) diana.abatida.Value = false;

        Debug.Log("[CampoTiro] Jugador se alejó. Galería reiniciada y apagada.");
    }
}