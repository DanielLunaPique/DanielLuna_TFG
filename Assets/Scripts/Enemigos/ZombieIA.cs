using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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
    public float distanciaAtaque = 1.6f;
    public float tiempoEntreAtaques = 1.5f;
    private bool estaAtacando = false;
    private float proximoAtaque = 0f;

    private NavMeshAgent agente;
    private Transform objetivoActual;
    private bool estaSpawneando = true;
    private float velocidadCalculadaRonda;

    [Header("Animaciones")]
    public Animator animator;

    // --- CAMBIO CLAVE: Variable de red para sincronizar la velocidad ---
    private NetworkVariable<float> velocidadRed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
        if (GameManager.Instance != null) rondaActual = GameManager.Instance.rondaActual.Value;
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
        agente.speed = (GameManager.Instance.zombiesVivos.Value == 1) ? velocidadCalculadaRonda * multiplicadorUltimoZombie : velocidadCalculadaRonda;
    }

    private void EncontrarJugadorMasCercano()
    {
        Transform mejorObjetivo = null;
        float distanciaMinima = float.MaxValue;

        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject != null)
            {
                SaludJugador salud = cliente.PlayerObject.GetComponent<SaludJugador>();
                if (salud == null || salud.estaMuerto) continue;

                float distancia = Vector3.Distance(transform.position, cliente.PlayerObject.transform.position);
                if (objetivoActual != null && cliente.PlayerObject.transform == objetivoActual) distancia -= distanciaIman;

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
        // El cliente solo lee la variable, el servidor la escribe
        if (IsServer)
        {
            if (estaSpawneando || objetivoActual == null || estaAtacando)
                velocidadRed.Value = 0f;
            else
                velocidadRed.Value = agente.velocity.magnitude;

            // Lógica de ataque
            if (objetivoActual != null)
            {
                float distanciaAlJugador = Vector3.Distance(transform.position, objetivoActual.position);
                if (distanciaAlJugador <= distanciaAtaque && !estaAtacando && Time.time >= proximoAtaque)
                    StartCoroutine(SecuenciaAtaque());

                if (!estaAtacando) agente.SetDestination(objetivoActual.position);
            }
        }

        GestionarAnimaciones();
    }

    private IEnumerator SecuenciaAtaque()
    {
        estaAtacando = true;
        proximoAtaque = Time.time + tiempoEntreAtaques;
        agente.isStopped = true;
        agente.velocity = Vector3.zero;

        AtacarClientRpc();

        yield return new WaitForSeconds(2.5f);
        if (agente != null && agente.enabled) agente.isStopped = false;
        estaAtacando = false;
    }

    [ClientRpc]
    private void AtacarClientRpc() => animator?.SetTrigger(triggerAtaqueHash);

    private void GestionarAnimaciones()
    {
        if (animator == null) return;

        // --- FIX DEFINITIVO: Usamos el valor sincronizado en lugar de calcular posiciones ---
        float v = velocidadRed.Value;
        if (v < 0.1f) v = 0f; // Evita el "micro-andado"

        animator.SetFloat("Speed", v, 0.25f, Time.deltaTime);
    }
}