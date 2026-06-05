using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.Animations;
using UnityEngine.Rendering;

public class SincronizacionTerceraPersona : NetworkBehaviour
{
    [Header("Referencias del Modelo 3D")]
    [Tooltip("El hueso de la mano derecha donde nacerá el arma")]
    public Transform huesoManoDerecha;

    [Tooltip("El target IK de la mano izquierda (El objeto que tiene el ParentConstraint)")]
    public Transform huesoManoIzquierdaIK;

    [Header("Base de Datos Global")]
    [Tooltip("Mete aquí TODOS los ScriptableObjects de las armas de tu juego para que la red sepa buscarlas")]
    public EstadisticasArma[] todasLasArmasJuego;

    // Variable sincronizada: Guarda el nombre del arma que tenemos en las manos
    public NetworkVariable<FixedString32Bytes> nombreArmaRed = new NetworkVariable<FixedString32Bytes>("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private GameObject armaFalsaActual;
    private NetworkMovement scriptMovimientoLocal;

    public override void OnNetworkSpawn()
    {
        scriptMovimientoLocal = GetComponent<NetworkMovement>();
        nombreArmaRed.OnValueChanged += AlCambiarArmaRed;
        ConfigurarRestriccion(huesoManoIzquierdaIK);

        // --- FIX: Ahora TODOS instancian el arma inicial si existe ---
        if (!string.IsNullOrEmpty(nombreArmaRed.Value.ToString()))
        {
            ActualizarArmaFalsa(nombreArmaRed.Value.ToString());
        }
    }

    public override void OnNetworkDespawn()
    {
        nombreArmaRed.OnValueChanged -= AlCambiarArmaRed;
    }

    public void NotificarCambioArma(string nombreNuevaArma)
    {
        // Solo el dueño puede pedirle al servidor que cambie su arma
        if (IsOwner) CambiarArmaServerRpc(nombreNuevaArma);
    }

    [ServerRpc]
    private void CambiarArmaServerRpc(string nombreArma)
    {
        nombreArmaRed.Value = new FixedString32Bytes(nombreArma);
    }

    private void AlCambiarArmaRed(FixedString32Bytes viejo, FixedString32Bytes nuevo)
    {
        // --- FIX: Quitamos el if(!IsOwner) para que el dueño también genere el arma para sus sombras ---
        ActualizarArmaFalsa(nuevo.ToString());
    }

    private void ActualizarArmaFalsa(string nombreArma)
    {
        if (armaFalsaActual != null) Destroy(armaFalsaActual);

        EstadisticasArma stats = BuscarStats(nombreArma);
        if (stats == null || stats.prefabTerceraPersona == null) return;

        armaFalsaActual = Instantiate(stats.prefabTerceraPersona, huesoManoDerecha);
        armaFalsaActual.transform.localPosition = stats.posicionManoDerecha;
        armaFalsaActual.transform.localEulerAngles = stats.rotacionManoDerecha;

        // Lógica para que el dueño no vea el arma 3D atravesando su cámara
        if (IsOwner)
        {
            bool verCuerpo = scriptMovimientoLocal != null && scriptMovimientoLocal.forzarVerCuerpoLocal;

            foreach (Renderer r in armaFalsaActual.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = verCuerpo ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            }
        }

        // --- FIX: Cambiado a LeftHandGrip ---
        Transform puntoAgarreIzq = armaFalsaActual.transform.Find("LeftHandGrip");

        if (puntoAgarreIzq != null && huesoManoIzquierdaIK != null)
        {
            EnlazarManoIzquierda(huesoManoIzquierdaIK, puntoAgarreIzq);
        }
        else if (huesoManoIzquierdaIK != null)
        {
            ParentConstraint pc = huesoManoIzquierdaIK.GetComponent<ParentConstraint>();
            if (pc != null) pc.constraintActive = false;
        }
    }

    private EstadisticasArma BuscarStats(string nombre)
    {
        foreach (var a in todasLasArmasJuego)
        {
            if (a != null && a.nombreArma == nombre) return a;
        }
        return null;
    }

    void ConfigurarRestriccion(Transform target)
    {
        if (target == null) return;
        ParentConstraint constraint = target.GetComponent<ParentConstraint>();
        if (constraint == null) constraint = target.gameObject.AddComponent<ParentConstraint>();
    }

    void EnlazarManoIzquierda(Transform targetIK, Transform gripArma)
    {
        if (targetIK == null || gripArma == null) return;
        ParentConstraint constraint = targetIK.GetComponent<ParentConstraint>();

        if (constraint.sourceCount > 0) constraint.RemoveSource(0);

        ConstraintSource nuevaFuente = new ConstraintSource();
        nuevaFuente.sourceTransform = gripArma;
        nuevaFuente.weight = 1f;

        constraint.AddSource(nuevaFuente);
        constraint.SetTranslationOffset(0, Vector3.zero);
        constraint.SetRotationOffset(0, Vector3.zero);
        constraint.constraintActive = true;
    }
}