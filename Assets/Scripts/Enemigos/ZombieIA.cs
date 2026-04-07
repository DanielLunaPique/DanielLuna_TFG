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
    public float velocidadBaseInicial = 1.5f;
    public float incrementoVelocidadRonda = 0.5f;
    public float velocidadMaxima = 6.0f;
    public float multiplicadorUltimoZombie = 1.5f;

    [Header("Combate")]
    public float distanciaAtaque = 1.6f; // Distancia para empezar a pegar
    public float tiempoEntreAtaques = 1.5f; // Segundos entre manotazos
    private bool estaAtacando = false;
    private float proximoAtaque = 0f;

    private NavMeshAgent agente;
    private Transform objetivoActual;
    private bool estaSpawneando = true;

    private float velocidadCalculadaRonda;

    [Header("Animaciones")]
    public Animator animator;
    private Vector3 ultimaPosicion;

    // Hash del trigger de ataque para optimizar
    private readonly int triggerAtaqueHash = Animator.StringToHash("Attack");

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

        CalcularVelocidadPorRonda();
        StartCoroutine(RutinaNacimiento());
    }

    private void CalcularVelocidadPorRonda()
    {
        int rondaActual = 1;
        if (GameManager.Instance != null)
        {
            rondaActual = GameManager.Instance.rondaActual.Value;
        }
        velocidadCalculadaRonda = Mathf.Min(velocidadBaseInicial + (rondaActual * incrementoVelocidadRonda), velocidadMaxima);
    }

    private IEnumerator RutinaNacimiento()
    {
        estaSpawneando = true;
        agente.speed = 0f;

        if (animator != null)
        {
            animator.speed = Random.Range(0.9f, 1.1f);
            animator.Play(0, -1, Random.Range(0f, 1f));
        }

        yield return new WaitForSeconds(tiempoDeNacimiento);

        estaSpawneando = false;
        agente.speed = velocidadCalculadaRonda;

        StartCoroutine(RutinaBuscarObjetivo());
    }

    private IEnumerator RutinaBuscarObjetivo()
    {
        while (true)
        {
            EncontrarJugadorMasCercano();
            ComprobarSiEsElUltimo();

            yield return new WaitForSeconds(tiempoEntreBusquedas);
        }
    }

    private void ComprobarSiEsElUltimo()
    {
        if (GameManager.Instance == null || estaAtacando) return;

        if (GameManager.Instance.zombiesVivos.Value == 1)
        {
            agente.speed = velocidadCalculadaRonda * multiplicadorUltimoZombie;
        }
        else
        {
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
        GestionarAnimaciones();

        if (!IsServer || estaSpawneando || objetivoActual == null) return;

        // 2. LÓGICA DE ATAQUE (Solo Servidor)
        float distanciaAlJugador = Vector3.Distance(transform.position, objetivoActual.position);

        if (distanciaAlJugador <= distanciaAtaque && !estaAtacando && Time.time >= proximoAtaque)
        {
            StartCoroutine(SecuenciaAtaque());
        }

        // 3. MOVIMIENTO (Solo Servidor, si no está atacando)
        if (!estaAtacando)
        {
            agente.SetDestination(objetivoActual.position);
        }
    }

    private IEnumerator SecuenciaAtaque()
    {
        estaAtacando = true;
        proximoAtaque = Time.time + tiempoEntreAtaques;

        // Frenamos al zombie en seco para que no patine mientras pega
        agente.isStopped = true;
        agente.velocity = Vector3.zero;

        AtacarClientRpc();

        // Esperamos a que la animación de ataque progrese. 
        // Ajusta este tiempo según lo que dure tu clip de ataque.
        yield return new WaitForSeconds(2.5f);

        if (agente != null && agente.enabled)
        {
            agente.isStopped = false;
        }

        estaAtacando = false;
    }

    [ClientRpc]
    private void AtacarClientRpc()
    {
        if (animator != null)
        {
            animator.SetTrigger(triggerAtaqueHash);
        }
    }

    private void GestionarAnimaciones()
    {
        if (animator == null) return;

        float velocidadReal = 0f;

        if (IsServer)
        {
            velocidadReal = agente.velocity.magnitude;
        }
        else
        {
            float velocidadBruta = (transform.position - ultimaPosicion).magnitude / Time.deltaTime;
            float velocidadAnterior = animator.GetFloat("Speed");
            velocidadReal = Mathf.Lerp(velocidadAnterior, velocidadBruta, Time.deltaTime * 10f);
            ultimaPosicion = transform.position;
        }

        animator.SetFloat("Speed", velocidadReal, 0.25f, Time.deltaTime);
    }
}