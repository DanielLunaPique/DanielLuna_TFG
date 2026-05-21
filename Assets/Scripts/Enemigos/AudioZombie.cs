using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AudioZombie : NetworkBehaviour
{
    // --- EL SISTEMA DE TOKENS (Estático para que sea compartido por todos) ---
    // Como es estático, cuenta cuántos zombis están sonando a la vez EN TU ORDENADOR.
    private static int zombiesHablando = 0;

    [Header("Configuración de Tokens")]
    [Tooltip("Máximo de zombis que pueden hacer ruidos de ambiente a la vez")]
    public int limiteZombiesSimultaneos = 3;

    [Header("Componentes")]
    public AudioSource fuente3D;

    [Header("Listas de Sonidos")]
    public AudioClip[] clipsAmbiente;
    public AudioClip[] clipsMuerte;
    public AudioClip clipAtaque;

    [Header("Configuración de Frecuencia")]
    public float distanciaDeteccionEscucha = 20f;

    private float tiempoSiguienteIntento;
    private Transform transformJugadorLocal;
    private bool tieneTokenActivo = false;

    void Start()
    {
        if (fuente3D == null) fuente3D = GetComponent<AudioSource>();

        fuente3D.spatialBlend = 1.0f; // 3D total
        fuente3D.dopplerLevel = 0.5f;
        fuente3D.minDistance = 1f;
        fuente3D.maxDistance = distanciaDeteccionEscucha;
        fuente3D.rolloffMode = AudioRolloffMode.Linear;

        BuscarJugadorLocal();

        // El primer intento de hablar será aleatorio para que no todos pregunten a la vez
        tiempoSiguienteIntento = Random.Range(1f, 5f);
    }

    private void BuscarJugadorLocal()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SpawnManager != null)
        {
            var playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (playerObj != null) transformJugadorLocal = playerObj.transform;
        }
    }

    void Update()
    {
        // Si el zombie ha muerto o no hay jugador, no hace nada
        if (transformJugadorLocal == null)
        {
            BuscarJugadorLocal();
            return;
        }

        ManejarSistemaDeTokens();
    }

    private void ManejarSistemaDeTokens()
    {
        tiempoSiguienteIntento -= Time.deltaTime;

        // Si es hora de intentar gruñir y NO está ya gruñiendo
        if (tiempoSiguienteIntento <= 0 && !tieneTokenActivo)
        {
            float distancia = Vector3.Distance(transform.position, transformJugadorLocal.position);

            if (distancia <= distanciaDeteccionEscucha)
            {
                // 1. PREGUNTA: ¿Hay hueco libre para hablar?
                if (zombiesHablando < limiteZombiesSimultaneos)
                {
                    // 2. ¡HAY HUECO! Agarramos el token y hablamos
                    LanzarGruñido(distancia);
                }
                else
                {
                    // 3. NO HAY HUECO. Nos callamos e intentamos de nuevo en un par de segundos
                    tiempoSiguienteIntento = Random.Range(1f, 2.5f);
                }
            }
            else
            {
                // Si está muy lejos, ni siquiera lo intenta en un buen rato para ahorrar recursos
                tiempoSiguienteIntento = Random.Range(5f, 8f);
            }
        }
    }

    private void LanzarGruñido(float distanciaAlJugador)
    {
        if (clipsAmbiente == null || clipsAmbiente.Length == 0) return;

        // Elegimos sonido y variamos el tono
        AudioClip clipElegido = clipsAmbiente[Random.Range(0, clipsAmbiente.Length)];
        fuente3D.pitch = Random.Range(0.85f, 1.15f);
        fuente3D.PlayOneShot(clipElegido);

        // Activamos el token local y subimos el contador global
        tieneTokenActivo = true;
        zombiesHablando++;

        // Calculamos cuándo será su próximo gruñido BASADO EN LA DISTANCIA
        float proximoGruñido;
        if (GameManager.Instance.zombiesVivos.Value == 1)
            proximoGruñido = 1f;
        else if (distanciaAlJugador < 5f)
            proximoGruñido = Random.Range(2f, 4f); // Muy cerca: Muy insistente
        else if (distanciaAlJugador < 12f)
            proximoGruñido = Random.Range(4f, 8f); // Distancia media
        else
            proximoGruñido = Random.Range(8f, 15f); // Lejos: Poco frecuente

        tiempoSiguienteIntento = proximoGruñido;

        // Iniciamos la rutina para devolver el token cuando termine el sonido
        StartCoroutine(LiberarToken(clipElegido.length));
    }

    private IEnumerator LiberarToken(float duracionSonido)
    {
        // Esperamos a que termine el sonido de ambiente
        yield return new WaitForSeconds(duracionSonido);

        zombiesHablando--;
        tieneTokenActivo = false;

        // Medida de seguridad por si acaso
        if (zombiesHablando < 0) zombiesHablando = 0;
    }

    // --- EVENTOS DE COMBATE (Estos saltan las reglas del token porque son obligatorios) ---

    public void SonarAtaque()
    {
        if (clipAtaque != null)
        {
            fuente3D.pitch = Random.Range(0.9f, 1.1f);
            fuente3D.PlayOneShot(clipAtaque);
        }
    }

    public void SonarMuerte()
    {
        fuente3D.Stop();

        // Si morimos mientras teníamos un token, lo devolvemos
        if (tieneTokenActivo)
        {
            zombiesHablando--;
            if (zombiesHablando < 0) zombiesHablando = 0;
            tieneTokenActivo = false;
        }

        if (clipsMuerte.Length > 0)
        {
            AudioClip clipMuerte = clipsMuerte[Random.Range(0, clipsMuerte.Length)];
            AudioSource.PlayClipAtPoint(clipMuerte, transform.position, 1f);
        }

        this.enabled = false;
    }
}