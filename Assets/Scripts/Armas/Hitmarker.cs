using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HitmarkerFPS : MonoBehaviour
{
    [Header("UI del Hitmarker")]
    public Image imagenHitmarker; // La imagen de la crucecita en el centro
    public float tiempoVisible = 0.15f;

    [Header("Audio")]
    public AudioSource audioSourceArma; // El mismo de disparar, o uno dedicado a hitmarkers
    public AudioClip sonidoHitmarker;

    void Start()
    {
        if (imagenHitmarker != null)
            imagenHitmarker.enabled = false; // Empezamos con el hitmarker invisible
    }

    // Llamaremos a esta función desde el script de disparo
    public void MostrarHitmarker()
    {
        if (imagenHitmarker != null)
        {
            StopAllCoroutines(); // Por si disparamos muy rápido, reiniciamos el parpadeo
            StartCoroutine(EfectoHitmarker());
        }

        // --- HUECO PARA EL AUDIO ---
        if (audioSourceArma != null && sonidoHitmarker != null)
        {
            // Usamos un pitch ligeramente aleatorio para que no suene robótico si disparamos rápido
            audioSourceArma.pitch = Random.Range(0.9f, 1.1f);
            audioSourceArma.PlayOneShot(sonidoHitmarker, 0.5f);
            audioSourceArma.pitch = 1f; // Restauramos
        }
    }

    private IEnumerator EfectoHitmarker()
    {
        imagenHitmarker.enabled = true;
        yield return new WaitForSeconds(tiempoVisible);
        imagenHitmarker.enabled = false;
    }
}