using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HitmarkerFPS : MonoBehaviour
{
    [Header("UI del Hitmarker")]
    public Image imagenHitmarker; // La imagen de la crucecita en el centro
    public float tiempoVisible = 0.1f; // Ajustado a 0.1s para que sea rápido y seco

    [Header("Audio")]
    public AudioSource audioSourceArma;
    public AudioClip sonidoHitmarker;

    void Start()
    {
        if (imagenHitmarker != null)
        {
            // Nos aseguramos de que empiece invisible y con el alpha a 0
            Color colorInicial = imagenHitmarker.color;
            colorInicial.a = 0f;
            imagenHitmarker.color = colorInicial;
            imagenHitmarker.enabled = false;
        }
    }

    public void MostrarHitmarker()
    {
        if (imagenHitmarker != null)
        {
            StopAllCoroutines(); // Reiniciamos el parpadeo si disparamos muy rápido
            StartCoroutine(EfectoHitmarker());
        }

        // --- HUECO PARA EL AUDIO ---
        if (audioSourceArma != null && sonidoHitmarker != null)
        {
            audioSourceArma.pitch = Random.Range(0.9f, 1.1f);
            audioSourceArma.PlayOneShot(sonidoHitmarker, 0.5f);
            audioSourceArma.pitch = 1f;
        }
    }

    private IEnumerator EfectoHitmarker()
    {
        // 1. Encendemos la imagen y la ponemos 100% opaca (Alpha = 1)
        imagenHitmarker.enabled = true;
        Color colorActual = imagenHitmarker.color;
        colorActual.a = 1f;
        imagenHitmarker.color = colorActual;

        float tiempoPasado = 0f;

        // 2. Bucle de desvanecimiento
        while (tiempoPasado < tiempoVisible)
        {
            // Sumamos el tiempo transcurrido en este fotograma
            tiempoPasado += Time.deltaTime;

            // Calculamos el porcentaje del tiempo que ha pasado (de 0 a 1)
            float porcentaje = tiempoPasado / tiempoVisible;

            // Interpolamos el alpha desde 1 (opaco) hasta 0 (invisible) según el porcentaje
            colorActual.a = Mathf.Lerp(1f, 0f, porcentaje);
            imagenHitmarker.color = colorActual;

            // Esperamos al siguiente fotograma antes de repetir el bucle
            yield return null;
        }

        // 3. Al terminar, nos aseguramos de que sea totalmente invisible y apagamos el componente
        colorActual.a = 0f;
        imagenHitmarker.color = colorActual;
        imagenHitmarker.enabled = false;
    }
}