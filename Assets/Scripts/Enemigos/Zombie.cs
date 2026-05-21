using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class Zombie : NetworkBehaviour
{
    [Header("Estadisticas")]
    public NetworkVariable<int> salud = new NetworkVariable<int>(18);

    public int puntosPorImpacto = 10;
    public int puntosPorMuerte = 70;

    [Header("Animaciones")]
    public Animator animator;

    [Header("Efectos de Nacimiento")]
    [Tooltip("El objeto hijo que contiene el Particle System (ej. Tierra saltando).")]
    public GameObject particulasSpawn;
    [Tooltip("El sonido que hace al salir del suelo.")]
    public AudioClip sonidoSpawn;
    [Tooltip("El AudioSource del zombie para reproducir el sonido.")]
    public AudioSource fuenteAudio;
    private float volumenNacimiento = 0.3f;
    [Tooltip("Cuánto tiempo duran las partículas antes de apagarse.")]
    public float tiempoParticulas = 4.5f;

    public override void OnNetworkSpawn()
    {
        // Envolvemos la asignación de vida para que los clientes no la toquen
        if (IsServer)
        {
            NetworkVariable<int> rondaActual = GameManager.Instance.rondaActual;
            salud.Value = salud.Value * rondaActual.Value;
        }

        // Ejecutamos los efectos visuales y sonoros en TODOS los clientes (Servidor y Jugadores)
        StartCoroutine(RutinaEfectosSpawn());
    }

    private IEnumerator RutinaEfectosSpawn()
    {
        // 1. Reproducir el sonido
        if (fuenteAudio != null && sonidoSpawn != null)
        {
            // Usamos un pitch aleatorio para que no suenen todos los zombis exactamente igual
            fuenteAudio.pitch = Random.Range(0.9f, 1.1f);
            fuenteAudio.PlayOneShot(sonidoSpawn, volumenNacimiento);
        }

        // 2. Encender las partículas
        if (particulasSpawn != null)
        {
            particulasSpawn.SetActive(true);

            // Buscamos todos los sistemas de partículas hijos y los forzamos a reproducir
            ParticleSystem[] sistemas = particulasSpawn.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in sistemas)
            {
                ps.Play();
            }
        }

        // 3. Esperar el tiempo configurado (el tiempo que el zombie está saliendo activamente)
        yield return new WaitForSeconds(tiempoParticulas);

        // 4. PARADA GRADUAL: Cortamos la emisión, pero dejamos que las partículas actuales se desvanezcan
        if (particulasSpawn != null)
        {
            ParticleSystem[] sistemas = particulasSpawn.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem ps in sistemas)
            {
                // 'StopEmitting' corta la fuente, pero el polvo en el aire sigue su curso
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // 5. Esperar el tiempo de disipación (Ajusta esto según el 'Start Lifetime' de tus partículas)
        // 2 segundos suele ser perfecto para que el humo/tierra termine de caer o desaparecer.
        yield return new WaitForSeconds(2f);

        // 6. Apagar el objeto final para limpiar la escena y ahorrar memoria
        if (particulasSpawn != null)
        {
            particulasSpawn.SetActive(false);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ulong idAtacante, bool esTiroALaCabeza = false)
    {
        if (salud.Value <= 0) return;

        salud.Value -= damage;
        IngresarDineroEnBancoServidor(idAtacante, puntosPorImpacto);

        if (salud.Value <= 0)
        {
            // --- MAGIA DEL HEADSHOT ---
            int puntosFinales = esTiroALaCabeza ? 100 : puntosPorMuerte;
            IngresarDineroEnBancoServidor(idAtacante, puntosFinales);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ZombieEliminado();
            }

            UnityEngine.AI.NavMeshAgent agente = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agente != null)
            {
                agente.isStopped = true;
                agente.velocity = Vector3.zero;
                agente.speed = 0f;
                agente.enabled = false;
            }

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            MorirClientRpc();

            StartCoroutine(EsperarYDespawnear());
        }
    }

    [ClientRpc]
    private void MorirClientRpc()
    {
        if (animator != null) animator.SetTrigger("Death");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        ParteDelCuerpo[] hitboxes = GetComponentsInChildren<ParteDelCuerpo>();
        foreach (ParteDelCuerpo hitbox in hitboxes)
        {
            Collider hitboxCol = hitbox.GetComponent<Collider>();
            if (hitboxCol != null) hitboxCol.enabled = false;
        }

        ZombieIA ia = GetComponent<ZombieIA>();
        if (ia != null) ia.enabled = false;
    }

    private IEnumerator EsperarYDespawnear()
    {
        AudioZombie audio = GetComponent<AudioZombie>();
        audio.SonarMuerte();
        yield return new WaitForSeconds(3f);

        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn();
        }
    }

    private void IngresarDineroEnBancoServidor(ulong idJugador, int cantidad)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(idJugador, out var cliente))
        {
            var bolsillo = cliente.PlayerObject.GetComponentInChildren<SistemaPuntosFPS>();
            if (bolsillo != null) bolsillo.puntos.Value += cantidad;
        }
    }
}