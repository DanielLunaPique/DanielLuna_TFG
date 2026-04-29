using UnityEngine;
using Unity.Netcode;
using TMPro;
using System.Collections;

public class PiedraRuna : NetworkBehaviour
{
    [Header("Referencias")]
    public GestorPuzzleRunas miGestor;
    public TextMeshPro textoRuna; // Arrastra aquí el TextMeshPro hijo

    [Header("Colores Visuales")]
    public Color colorNormal = new Color(0, 0.5f, 1f); // Azul
    public Color colorCorrecto = new Color(1f, 0.8f, 0f); // Dorado/Amarillo
    public Color colorError = new Color(1f, 0f, 0f); // Rojo

    // Sincronización en red
    public NetworkVariable<int> indiceSimbologia = new NetworkVariable<int>(-1);
    public NetworkVariable<int> estadoBrillo = new NetworkVariable<int>(0); // 0=Normal, 1=Correcto, 2=Error

    public override void OnNetworkSpawn()
    {
        // Cuando cambien los valores en el servidor, los clientes actualizan el aspecto
        indiceSimbologia.OnValueChanged += ActualizarSimboloVisual;
        estadoBrillo.OnValueChanged += ActualizarColorVisual;

        // Búsqueda de emergencia por si se te olvida arrastrar el gestor
        if (miGestor == null) miGestor = FindObjectOfType<GestorPuzzleRunas>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RecibirDisparoServerRpc()
    {
        // Si el puzzle ya está completado o la piedra ya está pulsada, ignoramos
        if (miGestor == null || miGestor.puzzleCompletado.Value || estadoBrillo.Value == 1) return;

        miGestor.ComprobarDisparoServer(this);
    }

    private void ActualizarSimboloVisual(int viejo, int nuevo)
    {
        if (nuevo >= 0 && miGestor != null)
        {
            textoRuna.text = miGestor.listaSimbolos[nuevo];
            textoRuna.color = colorNormal;
        }
    }

    private void ActualizarColorVisual(int viejo, int nuevo)
    {
        if (nuevo == 0) textoRuna.color = colorNormal;
        else if (nuevo == 1) textoRuna.color = colorCorrecto;
        else if (nuevo == 2) StartCoroutine(EfectoError());
    }

    private IEnumerator EfectoError()
    {
        textoRuna.color = colorError;
        // Tiembla un poco visualmente
        Vector3 posOriginal = textoRuna.transform.localPosition;
        for (int i = 0; i < 10; i++)
        {
            textoRuna.transform.localPosition = posOriginal + (Vector3)Random.insideUnitCircle * 0.05f;
            yield return new WaitForSeconds(0.05f);
        }
        textoRuna.transform.localPosition = posOriginal;
        textoRuna.color = colorNormal;
    }
}