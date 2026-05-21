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
    private float velocidadCalculadaRonda;

    [Header("Animaciones")]
    public Animator animator;

    // --- CAMBIOS CLAVE PARA NETCODE ---
    // Sincronizamos el estado de spawn para que los clientes también sepan cuándo esperar
    private NetworkVariable<bool> estaSpawneandoRed = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> velocidadRed = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly int triggerAtaqueHash = Animator.StringToHash("Attack");

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return; // Los clientes se quedan aquí con el agente apagado esperando al servidor

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
        estaSpawneandoRed.Value = true;

        if (animator != null)
        {
            // 1. El nacimiento SIEMPRE empieza en 0 y a velocidad 1
            // para que coincida matemáticamente con los 2.5 segundos que dura.
            animator.speed = 1f;
            animator.Play(0, -1, 0f);
        }

        yield return new WaitForSeconds(tiempoDeNacimiento);

        // 2. Ya ha salido de la tierra: Activamos el NavMeshAgent
        if (agente != null)
        {
            agente.speed = velocidadCalculadaRonda;
        }

        // 3. --- EL DESFASE DE ANIMACIÓN (Anti-Clones) ---
        if (animator != null)
        {
            // A partir de este momento, cada zombi moverá las piernas a un ritmo ligeramente distinto.
            // Esto rompe la sincronización de los pasos inmediatamente.
            animator.speed = Random.Range(0.85f, 1.15f);
        }

        estaSpawneandoRed.Value = false;

        // 4. Empiezan a perseguirte
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
        if (GameManager.Instance == null || estaAtacando || agente == null || !agente.enabled) return;
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
        if (IsServer)
        {
            // Si está spawneando, el agente está apagado, controlamos la velocidad manualmente a 0
            if (estaSpawneandoRed.Value || objetivoActual == null || estaAtacando)
                velocidadRed.Value = 0f;
            else if (agente != null && agente.enabled)
                velocidadRed.Value = agente.velocity.magnitude;

            // Lógica de ataque
            if (objetivoActual != null && !estaSpawneandoRed.Value)
            {
                float distanciaAlJugador = Vector3.Distance(transform.position, objetivoActual.position);
                if (distanciaAlJugador <= distanciaAtaque && !estaAtacando && Time.time >= proximoAtaque)
                    StartCoroutine(SecuenciaAtaque());

                if (!estaAtacando && agente != null && agente.enabled)
                    agente.SetDestination(objetivoActual.position);
            }
        }

        GestionarAnimaciones();
    }

    private IEnumerator SecuenciaAtaque()
    {
        AudioZombie audio = GetComponent<AudioZombie>();
        estaAtacando = true;
        proximoAtaque = Time.time + tiempoEntreAtaques;

        if (agente != null && agente.enabled)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }

        AtacarClientRpc();
        if (audio != null) audio.SonarAtaque();

        yield return new WaitForSeconds(1.3f);
        if (agente != null && agente.enabled) agente.isStopped = false;
        estaAtacando = false;
    }

    [ClientRpc]
    private void AtacarClientRpc() => animator?.SetTrigger(triggerAtaqueHash);

    private void GestionarAnimaciones()
    {
        if (animator == null) return;

        // Si la variable de red dice que está spawneando, congelamos la velocidad de animación a 0
        // para que no intente mezclar la caminata con la emersión.
        if (estaSpawneandoRed.Value)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        float v = velocidadRed.Value;
        if (v < 0.1f) v = 0f;

        animator.SetFloat("Speed", v, 0.25f, Time.deltaTime);
    }
}