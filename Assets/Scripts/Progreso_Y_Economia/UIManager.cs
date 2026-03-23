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

        // 1. Conectar los Puntos
        bolsillo = GetComponentInParent<SistemaPuntosFPS>();
        if (bolsillo != null)
        {
            bolsillo.OnPuntosCambiados += ActualizarTextoPuntos;
            ActualizarTextoPuntos(bolsillo.puntos.Value);
        }

        // 2. Conectar la Ronda
        if (GameManager.Instance != null)
        {
            GameManager.Instance.rondaActual.OnValueChanged += ActualizarTextoRonda;
            ActualizarTextoRonda(0, GameManager.Instance.rondaActual.Value);
        }

        // --- NUEVO: Asegurarnos de que el texto empieza apagado ---
        if (textoInteraccion != null) textoInteraccion.gameObject.SetActive(false);
    }

    private void ActualizarTextoPuntos(int nuevosPuntos)
    {
        if (textoPuntos != null) textoPuntos.text = $"$ {nuevosPuntos}";
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
        if (bolsillo != null) bolsillo.OnPuntosCambiados -= ActualizarTextoPuntos;
        if (GameManager.Instance != null) GameManager.Instance.rondaActual.OnValueChanged -= ActualizarTextoRonda;
    }
}