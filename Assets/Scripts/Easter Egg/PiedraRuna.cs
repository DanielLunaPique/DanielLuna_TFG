using System.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Rendering.Universal;

public class PiedraRuna : NetworkBehaviour
{
    private GestorPuzzleRunas gestor;
    private DecalProjector proyector;
    private MeshRenderer meshRenderer;
    private Collider miCollider;

    public NetworkVariable<int> estadoBrillo = new NetworkVariable<int>(0);

    // Esta variable controla si la piedra se dibuja o no en el mapa
    public NetworkVariable<bool> piedraVisible = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        gestor = GetComponentInParent<GestorPuzzleRunas>();
        proyector = GetComponentInChildren<DecalProjector>();
        meshRenderer = GetComponent<MeshRenderer>();
        miCollider = GetComponent<Collider>();

        estadoBrillo.OnValueChanged += ActualizarColor;
        piedraVisible.OnValueChanged += AlternarVisibilidad;

        // Forzamos el estado visual nada más nacer (normalmente invisibles hasta que toque el paso)
        AplicarVisibilidad(piedraVisible.Value);
    }

    private void AlternarVisibilidad(bool viejo, bool nuevo)
    {
        AplicarVisibilidad(nuevo);
    }

    private void AplicarVisibilidad(bool visible)
    {
        if (meshRenderer != null) meshRenderer.enabled = visible;
        if (miCollider != null) miCollider.enabled = visible;
        if (proyector != null) proyector.enabled = visible;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RecibirDisparoServerRpc()
    {
        // Evitamos que nos disparen si somos invisibles
        if (gestor != null && piedraVisible.Value)
        {
            gestor.ComprobarDisparoServer(this);
        }
    }

    private void ActualizarColor(int viejo, int nuevo)
    {
        if (proyector == null) return;

        // Asegúrate de que tu Shader de Decal tiene la propiedad _EmissionColor
        if (nuevo == 0) proyector.material.SetColor("_EmissionColor", Color.cyan * 2f); // Normal
        else if (nuevo == 1) proyector.material.SetColor("_EmissionColor", Color.yellow * 4f); // Acierto
        else if (nuevo == 2) proyector.material.SetColor("_EmissionColor", Color.red * 4f); // Error
    }

    [ClientRpc]
    public void DesaparecerPiedraClientRpc()
    {
        // Todos los clientes ejecutan la animación simultáneamente
        StartCoroutine(RutinaDesaparecer());
    }

    private IEnumerator RutinaDesaparecer()
    {
        // Desactivamos el collider de inmediato para no absorber más balas
        if (miCollider != null) miCollider.enabled = false;

        Vector3 escalaOriginal = transform.localScale;
        Vector3 escalaAmpliada = escalaOriginal * 1.3f;

        // 1. Ampliación rápida (anticipación)
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaAmpliada, t / 0.2f);
            yield return null;
        }

        // 2. Reducción a 0 (desaparición)
        t = 0;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaAmpliada, Vector3.zero, t / 0.4f);
            yield return null;
        }

        // Por seguridad, las ocultamos del todo
        AplicarVisibilidad(false);
    }
}