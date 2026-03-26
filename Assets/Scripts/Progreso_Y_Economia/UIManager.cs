using UnityEngine;
using TMPro;
using Unity.Netcode;

public class UIManager : NetworkBehaviour
{
    [Header("Textos de UI")]
    public TextMeshProUGUI textoPuntos;
    public TextMeshProUGUI textoRonda;

    // --- NUEVO: TEXTO CENTRAL ---
    public TextMeshProUGUI textoInteraccion;

    private SistemaPuntosFPS bolsillo;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        bolsillo = GetComponentInParent<SistemaPuntosFPS>();
        if (bolsillo != null)
        {
            // LA MAGIA: Ahora la UI vigila la variable de red directamente
            bolsillo.puntos.OnValueChanged += ActualizarTextoPuntos;
            ActualizarTextoPuntos(0, bolsillo.puntos.Value);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.rondaActual.OnValueChanged += ActualizarTextoRonda;
            ActualizarTextoRonda(0, GameManager.Instance.rondaActual.Value);
        }

        if (textoInteraccion != null) textoInteraccion.gameObject.SetActive(false);
    }

    // NetworkVariable siempre envía el valor viejo y el nuevo, así que actualizamos la función:
    private void ActualizarTextoPuntos(int valorViejo, int valorNuevo)
    {
        if (textoPuntos != null) textoPuntos.text = $"$ {valorNuevo}";
    }

    private void ActualizarTextoRonda(int rondaAnterior, int rondaNueva)
    {
        if (textoRonda != null) textoRonda.text = $"{rondaNueva}";
    }

    // ==========================================
    // NUEVAS FUNCIONES DE INTERACCIÓN
    // ==========================================
    public void MostrarTextoInteraccion(string mensaje)
    {
        if (textoInteraccion != null)
        {
            textoInteraccion.text = mensaje;
            // Encendemos el objeto de texto para que se vea
            textoInteraccion.gameObject.SetActive(true);
        }
    }

    public void OcultarTextoInteraccion()
    {
        if (textoInteraccion != null)
        {
            // Apagamos el objeto de texto
            textoInteraccion.gameObject.SetActive(false);
        }
    }

    public override void OnNetworkDespawn()
    {
        // Acuérdate de cambiar aquí también la desconexión
        if (bolsillo != null) bolsillo.puntos.OnValueChanged -= ActualizarTextoPuntos;
        if (GameManager.Instance != null) GameManager.Instance.rondaActual.OnValueChanged -= ActualizarTextoRonda;
    }
}