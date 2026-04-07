using GLTFast.Schema;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieIA : NetworkBehaviour
{
    [Header("Configuración de IA")]
    public float tiempoDeNacimiento = 2.5f;
    public float tiempoEntreBusquedas = 0.5f;
    public float distanciaIman = 2.5f;

    [Header("Velocidad y Rondas")]
    public float velocidadBaseInicial = 1.5f; // Velocidad en ronda 1 (Caminando lento)
    public float incrementoVelocidadRonda = 0.5f; // Cuánto sube la velocidad por cada ronda
    public float velocidadMaxima = 6.0f; // El tope máximo para que no sea injugable
    public float multiplicadorUltimoZombie = 1.5f; // Si es el último, correrá un 50% más rápido

    private NavMeshAgent agente;
    private Transform objetivoActual;
    private bool estaSpawneando = true;

    // Guardamos la velocidad que le toca en esta ronda
    private float velocidadCalculadaRonda;

    [Header("Animaciones")]
    public Animator animator;
    private Vector3 ultimaPosicion;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            agente.enabled = false;
            return;
        }

        // 1. Calculamos la velocidad en base a la ronda actual
        CalcularVelocidadPorRonda();

        StartCoroutine(RutinaNacimiento());
    }

    private void CalcularVelocidadPorRonda()
    {
        int rondaActual = 1;

        // Protegemos por si el GameManager no está listo un milisegundo
        if (GameManager.Instance != null)
        {
            rondaActual = GameManager.Instance.rondaActual.Value;
        }

        // Formula: Velocidad Base + (Ronda * Incremento). Limitado por la Velocidad Maxima.
        velocidadCalculadaRonda = Mathf.Min(velocidadBaseInicial + (rondaActual * incrementoVelocidadRonda), velocidadMaxima);
    }

    private IEnumerator RutinaNacimiento()
    {
        estaSpawneando = true;

        // En lugar de pausar el agente, le quitamos la velocidad para que no patine
        agente.speed = 0f;

        if (animator != null)
        {
            // 1. Cada zombie reproducirá la animación a una velocidad ligeramente distinta (entre 90% y 110%)
            animator.speed = Random.Range(0.9f, 1.1f);

            // 2. Le decimos al Animator que empiece la animación actual en un punto aleatorio (de 0.0 a 1.0)
            animator.Play(0, -1, Random.Range(0f, 1f));
        }

        yield return new WaitForSeconds(tiempoDeNacimiento);

        estaSpawneando = false;

        // Le devolvemos su velocidad normal de ronda
        agente.speed = velocidadCalculadaRonda;

        StartCoroutine(RutinaBuscarObjetivo());
    }

    private IEnumerator RutinaBuscarObjetivo()
    {
        while (true)
        {
            EncontrarJugadorMasCercano();
            ComprobarSiEsElUltimo(); // Verificamos si tiene que correr más

            yield return new WaitForSeconds(tiempoEntreBusquedas);
        }
    }

    private void ComprobarSiEsElUltimo()
    {
        if (GameManager.Instance == null) return;

        // OJO AQUÍ: Tienes que cambiar "zombiesVivos" por el nombre exacto de la variable 
        // que uses en tu GameManager para contar cuántos zombies quedan vivos en el mapa.
        if (GameManager.Instance.zombiesVivos.Value == 1)
        {
            // Es el último: mete el turbo (ignorando la velocidad máxima si hace falta)
            agente.speed = velocidadCalculadaRonda * multiplicadorUltimoZombie;
        }
        else
        {
            // Hay más zombies: velocidad normal de ronda
            agente.speed = velocidadCalculadaRonda;
        }
    }

    private void EncontrarJugadorMasCercano()
    {
        Transform mejorObjetivo = null;
        float distanciaMinima = float.MaxValue;

        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject != null)
            {
                float distancia = Vector3.Distance(transform.position, cliente.PlayerObject.transform.position);

                if (objetivoActual != null && cliente.PlayerObject.transform == objetivoActual)
                {
                    distancia -= distanciaIman;
                }

                if (distancia < distanciaMinima)
                {
                    distanciaMinima = distancia;
                    mejorObjetivo = cliente.PlayerObject.transform;
                }
            }
        }

        objetivoActual = mejorObjetivo;
    }

    private void Update()
    {
        // 1. MOVIMIENTO (SOLO SERVIDOR)
        if (IsServer && !estaSpawneando && objetivoActual != null)
        {
            agente.SetDestination(objetivoActual.position);
        }

        // 2. ANIMACIONES (SERVIDOR Y CLIENTES)
        if (animator != null)
        {
            float velocidadReal = 0f;

            if (IsServer)
            {
                velocidadReal = agente.velocity.magnitude;
            }
            else
            {
                // Calculamos la velocidad bruta
                float velocidadBruta = (transform.position - ultimaPosicion).magnitude / Time.deltaTime;

                float velocidadAnterior = animator.GetFloat("Speed");
                velocidadReal = Mathf.Lerp(velocidadAnterior, velocidadBruta, Time.deltaTime * 10f);

                ultimaPosicion = transform.position;
            }


            // El '0.25f' es el tiempo de amortiguación. Ignorará frenazos que duren menos que eso.
            animator.SetFloat("Speed", velocidadReal, 1f, Time.deltaTime);
        }
    }
}