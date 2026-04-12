using UnityEngine;
using Unity.Netcode;

public class SistemaVoces : NetworkBehaviour
{
    // Definimos los tipos de frases para que aparezcan en un desplegable
    public enum TipoVoz
    {
        Recarga,
        RecogerObjeto,
        ArmaNueva,
        EasterEgg,
        Radio,
        Herido,
        MunicionBaja,
        Muerte
    }

    [Header("Configuración")]
    public AudioSource fuenteVoz;

    [Header("Colecciones de Audio")]
    public AudioClip[] frasesRecarga;
    public AudioClip[] frasesRecoger;
    public AudioClip[] frasesArmaNueva;
    public AudioClip[] frasesEasterEgg;
    public AudioClip[] frasesRadio;
    public AudioClip[] frasesHerido;
    public AudioClip[] frasesMunicionBaja;
    public AudioClip[] frasesMuerte;

    /// <summary>
    /// Función principal para activar una voz. 
    /// Solo el dueño del personaje (IsOwner) decide cuándo hablar.
    /// </summary>
    public void ReproducirFrase(TipoVoz tipo)
    {
        if (!IsOwner) return;

        // Evitamos solapar voces (opcional, podrías quitarlo para radios)
        if (fuenteVoz.isPlaying && tipo != TipoVoz.Radio) return;

        // Avisamos al servidor para que todos escuchen
        ReproducirVozServerRpc(tipo);
    }

    [ServerRpc]
    private void ReproducirVozServerRpc(TipoVoz tipo)
    {
        // El servidor le dice a todos los clientes que reproduzcan el sonido
        ReproducirVozClientRpc(tipo);
    }

    [ClientRpc]
    private void ReproducirVozClientRpc(TipoVoz tipo)
    {
        AudioClip clipElegido = ObtenerClipAleatorio(tipo);

        if (clipElegido != null)
        {
            // PlayOneShot permite que si hay un sonido de ambiente, la voz suene encima
            fuenteVoz.PlayOneShot(clipElegido);
        }
    }

    private AudioClip ObtenerClipAleatorio(TipoVoz tipo)
    {
        AudioClip[] listaCandidata = null;

        // El famoso SWITCH que organiza todo
        switch (tipo)
        {
            case TipoVoz.Recarga: listaCandidata = frasesRecarga; break;
            case TipoVoz.RecogerObjeto: listaCandidata = frasesRecoger; break;
            case TipoVoz.ArmaNueva: listaCandidata = frasesArmaNueva; break;
            case TipoVoz.EasterEgg: listaCandidata = frasesEasterEgg; break;
            case TipoVoz.Radio: listaCandidata = frasesRadio; break;
            case TipoVoz.Herido: listaCandidata = frasesHerido; break;
            case TipoVoz.MunicionBaja: listaCandidata = frasesMunicionBaja; break;
            case TipoVoz.Muerte: listaCandidata = frasesMuerte; break;
        }

        if (listaCandidata != null && listaCandidata.Length > 0)
        {
            return listaCandidata[Random.Range(0, listaCandidata.Length)];
        }

        return null;
    }
}