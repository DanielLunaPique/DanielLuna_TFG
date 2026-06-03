using UnityEngine;
using Unity.Netcode;

public class SistemaVoces : NetworkBehaviour
{
    public enum TipoVoz
    {
        Herido, MunicionBaja, SinPuntos, AbrirPuertas
    }

    [Header("Configuración")]
    public AudioSource fuenteVoz;

    [Header("Colecciones de Audio")]
    public AudioClip[] frasesHerido;
    public AudioClip[] frasesMunicionBaja;
    public AudioClip[] frasesSinPuntos;
    public AudioClip[] frasesAbrirPuertas;

    // --- EL SEGURO DINÁMICO LOCAL ---
    private float tiempoParaVolverAHablar = 0f;

    public void ReproducirFraseEspecificaVip(AudioClip clipEspecial)
    {
        if (clipEspecial == null || fuenteVoz == null) return;

        // 1. Prioridad absoluta: Si estaba diciendo una frase random, le mandamos callar
        if (fuenteVoz.isPlaying)
        {
            fuenteVoz.Stop();
        }

        // 2. Reproducimos la frase de historia
        fuenteVoz.PlayOneShot(clipEspecial);

        // 3. Bloqueamos las frases random durante el tiempo que dure esta frase para que no la pisen
        tiempoParaVolverAHablar = Time.time + clipEspecial.length + 1f;
    }

    public void ReproducirFrase(TipoVoz tipo)
    {
        // Solo el dueño del personaje decide cuándo hablar
        if (!IsOwner) return;

        // Comprobamos si está hablando o si está en el segundo de "respiro"
        if (fuenteVoz.isPlaying || Time.time < tiempoParaVolverAHablar) return;

        // Obtenemos la lista que toca
        AudioClip[] listaCandidata = ObtenerListaCandidata(tipo);
        if (listaCandidata == null || listaCandidata.Length == 0) return;

        // Elegimos el audio
        int indiceAleatorio = Random.Range(0, listaCandidata.Length);
        AudioClip clipElegido = listaCandidata[indiceAleatorio];

        // Bloqueamos la voz el tiempo que dure el clip + 1 segundo de respiro
        tiempoParaVolverAHablar = Time.time + clipElegido.length + 1f;

        fuenteVoz.PlayOneShot(clipElegido);
    }

    private AudioClip[] ObtenerListaCandidata(TipoVoz tipo)
    {
        switch (tipo)
        {
            case TipoVoz.Herido: return frasesHerido;
            case TipoVoz.MunicionBaja: return frasesMunicionBaja;
            case TipoVoz.SinPuntos: return frasesSinPuntos;
            case TipoVoz.AbrirPuertas: return frasesAbrirPuertas;
            default: return null;
        }
    }
}