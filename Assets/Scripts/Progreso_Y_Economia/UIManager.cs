using UnityEngine;
using TMPro;
using Unity.Netcode;

public class UIManager : NetworkBehaviour
{
    [Header("Textos de UI")]
    public TextMeshProUGUI textoPuntos;
    public TextMeshProUGUI textoRonda;

    

    private SistemaPuntosFPS bolsillo;

    public override void OnNetworkSpawn()
    {
        // Si no somos el dueño de este jugador, apagamos su Canvas para no ver la UI de los demás
        if (!IsOwner)
        {
            // Asumiendo que el script está en el objeto Canvas o que tienes una referencia. 
            // Si lo pones en el Jugador, haz: GetComponentInChildren<Canvas>().enabled = false;
            return;
        }

        // 1. Conectar los Puntos
        bolsillo = GetComponentInParent<SistemaPuntosFPS>();
        if (bolsillo != null)
        {
            // Nos suscribimos al evento que creamos el otro día
            bolsillo.OnPuntosCambiados += ActualizarTextoPuntos;
            // Forzamos la primera actualización
            ActualizarTextoPuntos(bolsillo.puntos.Value);
        }

        // 2. Conectar la Ronda (Usamos la Instancia global que acabamos de crear)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.rondaActual.OnValueChanged += ActualizarTextoRonda;
            ActualizarTextoRonda(0, GameManager.Instance.rondaActual.Value);
        }
    }

    private void ActualizarTextoPuntos(int nuevosPuntos)
    {
        if (textoPuntos != null)
        {
            textoPuntos.text = $"$ {nuevosPuntos}";
        }
    }

    // Las NetworkVariables siempre pasan el valor viejo y el nuevo
    private void ActualizarTextoRonda(int rondaAnterior, int rondaNueva)
    {
        if (textoRonda != null)
        {
            textoRonda.text = $"{rondaNueva}"; // Aquí puedes poner el texto en rojo sangre
        }
    }

    public override void OnNetworkDespawn()
    {
        // Siempre es buena práctica desconectar los eventos al destruir el objeto
        if (bolsillo != null) bolsillo.OnPuntosCambiados -= ActualizarTextoPuntos;
        if (GameManager.Instance != null) GameManager.Instance.rondaActual.OnValueChanged -= ActualizarTextoRonda;
    }
}