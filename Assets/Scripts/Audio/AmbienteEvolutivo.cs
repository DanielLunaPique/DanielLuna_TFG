using UnityEngine;
using System.Collections;

public class AmbienteEvolutivo : MonoBehaviour
{
    [Header("Componentes")]
    public AudioSource fuenteVientoGeneral;
    public AudioSource fuenteSustosEsporadicos;

    [Header("Colección de Sustos Sueltos")]
    [Tooltip("Pon aquí sonidos cortos de 2-3 segundos (susurros, ramas rotas, eco)")]
    public AudioClip[] sonidosCreepy;

    [Header("Configuración de Tiempos")]
    public float tiempoMinimoEntreSustos = 30f;  // Medio minuto
    public float tiempoMaximoEntreSustos = 120f; // Dos minutos

    void Start()
    {
        // El viento general se reproduce solo y en bucle eterno
        if (fuenteVientoGeneral != null)
        {
            fuenteVientoGeneral.loop = true;
            fuenteVientoGeneral.Play();
        }

        // Iniciamos el reloj de los sustos
        StartCoroutine(RutinaInyectarSustos());
    }

    private IEnumerator RutinaInyectarSustos()
    {
        while (true)
        {
            // Esperamos un tiempo aleatorio para que el jugador nunca sepa cuándo va a sonar
            float tiempoEspera = Random.Range(tiempoMinimoEntreSustos, tiempoMaximoEntreSustos);
            yield return new WaitForSeconds(tiempoEspera);

            // Inyectamos un sonido creepy por encima del viento sin cortarlo
            if (fuenteSustosEsporadicos != null && sonidosCreepy.Length > 0)
            {
                AudioClip sustoElegido = sonidosCreepy[Random.Range(0, sonidosCreepy.Length)];

                // Variamos un poco el pitch y el volumen para dar más variedad
                fuenteSustosEsporadicos.pitch = Random.Range(0.8f, 1.2f);
                fuenteSustosEsporadicos.volume = Random.Range(0.3f, 0.7f);

                fuenteSustosEsporadicos.PlayOneShot(sustoElegido);
            }
        }
    }
}