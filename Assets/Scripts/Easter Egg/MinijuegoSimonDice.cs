using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class MinijuegoSimonDice : NetworkBehaviour
{
    public string idMisionAsociada = "Hackeo";

    [Header("Configuración")]
    public InteraccionBotonSimon[] botones;
    public Material matApagado;
    public int nivelMaximo = 5;
    public float tiempoLimiteInactividad = 10f; // Tiempo antes de fallar por AFK

    [Header("Audio")]
    public AudioClip sonidoBoton; // Un pitido genérico que tú o tu colega grabéis

    // Estados
    private enum EstadoSimon { Inactivo, EsperandoInicio, Presentacion, Reproduciendo, EsperandoJugador }
    private EstadoSimon estadoActual = EstadoSimon.Inactivo;

    private List<int> secuenciaServidor = new List<int>();
    private int indiceJugadorActual = 0;
    private int nivelActual = 1;
    private float temporizadorAFK = 0f;

    private void Update()
    {
        if (!IsServer) return;

        // 1. Activar el minijuego si toca el paso
        if (estadoActual == EstadoSimon.Inactivo)
        {
            if (QuestManager.Instance.idPasoActual.Value.ToString() == idMisionAsociada)
            {
                estadoActual = EstadoSimon.EsperandoInicio;
            }
        }

        // 2. Control de Inactividad (Anti-Trolls)
        if (estadoActual == EstadoSimon.EsperandoJugador)
        {
            temporizadorAFK += Time.deltaTime;
            if (temporizadorAFK >= tiempoLimiteInactividad)
            {
                Debug.LogWarning("[SimonDice] ¡Tiempo agotado! Reseteando...");
                StartCoroutine(PenalizacionFallo());
            }
        }
    }

    // ==========================================
    // FLUJO DEL JUEGO
    // ==========================================

    public void IntentarInteractuar(int indiceBoton)
    {
        if (!IsServer) return;

        if (estadoActual == EstadoSimon.EsperandoInicio)
        {
            // El jugador pulsa por primera vez para arrancar
            StartCoroutine(FasePresentacion());
        }
        else if (estadoActual == EstadoSimon.EsperandoJugador)
        {
            // El jugador pulsa intentando adivinar la secuencia
            RecibirPulsacion(indiceBoton);
        }
    }

    private IEnumerator FasePresentacion()
    {
        estadoActual = EstadoSimon.Presentacion;

        // Generamos TODA la secuencia de los 5 niveles de golpe
        secuenciaServidor.Clear();
        for (int i = 0; i < nivelMaximo; i++)
        {
            secuenciaServidor.Add(Random.Range(0, botones.Length));
        }

        nivelActual = 1; // Empezamos pidiendo 1 sola pulsación

        // Encendemos todas las luces 2 segundos
        for (int i = 0; i < botones.Length; i++) IluminarBotonClientRpc(i, true);
        ReproducirSonidoClientRpc(0); // Sonido de inicio

        yield return new WaitForSeconds(2f);

        // Apagamos todas
        for (int i = 0; i < botones.Length; i++) IluminarBotonClientRpc(i, false);

        yield return new WaitForSeconds(1f);

        StartCoroutine(ReproducirSecuencia());
    }

    private IEnumerator ReproducirSecuencia()
    {
        estadoActual = EstadoSimon.Reproduciendo;
        yield return new WaitForSeconds(0.5f);

        // Reproducimos solo hasta el nivel actual (ej: Nivel 3 = 3 luces)
        for (int i = 0; i < nivelActual; i++)
        {
            int indice = secuenciaServidor[i];

            IluminarBotonClientRpc(indice, true);
            ReproducirSonidoClientRpc(indice);

            yield return new WaitForSeconds(0.6f);

            IluminarBotonClientRpc(indice, false);
            yield return new WaitForSeconds(0.2f);
        }

        // Le toca al jugador
        indiceJugadorActual = 0;
        temporizadorAFK = 0f; // Reseteamos temporizador
        estadoActual = EstadoSimon.EsperandoJugador;
    }

    private void RecibirPulsacion(int indicePulsado)
    {
        temporizadorAFK = 0f; // Cada vez que pulsa, le damos más tiempo

        StartCoroutine(DestelloRapidoBoton(indicePulsado));

        if (indicePulsado == secuenciaServidor[indiceJugadorActual])
        {
            // ¡Acierto!
            indiceJugadorActual++;

            if (indiceJugadorActual >= nivelActual)
            {
                nivelActual++; // Sube la dificultad
                estadoActual = EstadoSimon.Reproduciendo; // Bloquea inputs

                if (nivelActual > nivelMaximo)
                {
                    Debug.Log("[SimonDice] ¡HACKEO COMPLETADO!");
                    QuestManager.Instance.NotificarPasoCompletadoServerRpc(idMisionAsociada);
                    estadoActual = EstadoSimon.Inactivo;
                }
                else
                {
                    StartCoroutine(ReproducirSecuencia());
                }
            }
        }
        else
        {
            // ¡Fallo!
            StartCoroutine(PenalizacionFallo());
        }
    }

    private IEnumerator DestelloRapidoBoton(int indice)
    {
        IluminarBotonClientRpc(indice, true);
        ReproducirSonidoClientRpc(indice);
        yield return new WaitForSeconds(0.3f);
        IluminarBotonClientRpc(indice, false);
    }

    private IEnumerator PenalizacionFallo()
    {
        estadoActual = EstadoSimon.Reproduciendo; // Bloquea inputs
        // Podrías poner aquí un sonido de error grave
        yield return new WaitForSeconds(1.5f);

        // Volvemos a empezar toda la partida desde el nivel 1
        StartCoroutine(FasePresentacion());
    }

    // ==========================================
    // SINCRONIZACIÓN CON CLIENTES
    // ==========================================

    [ClientRpc]
    private void IluminarBotonClientRpc(int indiceBoton, bool encender)
    {
        InteraccionBotonSimon boton = botones[indiceBoton];

        if (boton.mallaPantalla != null)
        {
            boton.mallaPantalla.material = encender ? boton.materialEncendido : matApagado;
        }
        else
        {
            // Por si acaso se te olvida arrastrarlo, intentamos buscarlo en el objeto o hijos
            MeshRenderer mallaAuto = boton.GetComponentInChildren<MeshRenderer>();
            if (mallaAuto != null) mallaAuto.material = encender ? boton.materialEncendido : matApagado;
        }
    }

    [ClientRpc]
    private void ReproducirSonidoClientRpc(int indiceBoton)
    {
        if (sonidoBoton == null) return;

        // Creamos un audio fantasma con Pitch dinámico (Magia musical)
        GameObject tempAudio = new GameObject("SonidoSimon");
        tempAudio.transform.position = botones[indiceBoton].transform.position;
        AudioSource source = tempAudio.AddComponent<AudioSource>();

        source.clip = sonidoBoton;
        source.spatialBlend = 1f; // 3D
        source.pitch = 1f + (indiceBoton * 0.2f); // Botón 0=1.0, Botón 1=1.2, Botón 2=1.4...

        source.Play();
        Destroy(tempAudio, sonidoBoton.length + 0.1f); // Se destruye solo al terminar
    }
}