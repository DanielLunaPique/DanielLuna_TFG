using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public enum EstadoJuego
    {
        EsperandoJugadores,
        Preparacion,
        RondaNormal,
        RondaEspecial,
        RondaAsedio,
        RondaAguantar  // <-- ¡Ha llegado su momento!
    }

    [Header("Control de partida")]
    public NetworkVariable<EstadoJuego> estadoActual = new NetworkVariable<EstadoJuego>();
    public NetworkVariable<int> rondaActual = new NetworkVariable<int>();

    [Header("ContadorDeZombies")]
    public NetworkVariable<int> zombiesVivos = new NetworkVariable<int>();
    public NetworkVariable<int> zombiesPorGenerar = new NetworkVariable<int>();

    [Header("Ajustes")]
    public float tiempoPreparacion = 10f;
    public int spawnsSimultaneos = 5;
    public float tiempoEntreOleadas = 4f;

    [Header("Prefab Zombie")]
    public GameObject prefabZombie;

    [Header("Audio de Interfaz")]
    public AudioSource audioSourceUI;
    public AudioClip sonidoCambioRonda;

    public static GameManager Instance;

    [Header("Objetos Globales")]
    public NetworkVariable<bool> pataDeCabraDesbloqueada = new NetworkVariable<bool>();

    // --- VARIABLES PARA EL LOCKDOWN ---
    private int zombiesGuardadosParaLuego = 0;
    private AltarRitual altarActual; // Para avisarle cuando termine

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) StartCoroutine(IniciarPrimeraRonda());
    }

    private IEnumerator IniciarPrimeraRonda()
    {
        estadoActual.Value = EstadoJuego.Preparacion;
        yield return new WaitForSeconds(2f);

        rondaActual.Value++;
        zombiesPorGenerar.Value = rondaActual.Value * 5;
        estadoActual.Value = EstadoJuego.RondaNormal;
        StartCoroutine(RutinaGenerarZombies());
    }

    private IEnumerator IniciarSiguienteRonda()
    {
        estadoActual.Value = EstadoJuego.Preparacion;
        if (audioSourceUI != null && sonidoCambioRonda != null)
            audioSourceUI.PlayOneShot(sonidoCambioRonda);

        yield return new WaitForSeconds(tiempoPreparacion);

        rondaActual.Value++;
        zombiesPorGenerar.Value = rondaActual.Value * 5;
        estadoActual.Value = EstadoJuego.RondaNormal;
        StartCoroutine(RutinaGenerarZombies());
    }

    private IEnumerator RutinaGenerarZombies()
    {
        while ((estadoActual.Value == EstadoJuego.RondaNormal || estadoActual.Value == EstadoJuego.RondaEspecial)
               && zombiesPorGenerar.Value > 0)
        {
            var clientes = NetworkManager.Singleton.ConnectedClientsList;
            if (clientes.Count == 0) { yield return new WaitForSeconds(1f); continue; }

            ZonaZombies[] todasLasZonas = FindObjectsOfType<ZonaZombies>();
            List<PuntoSpawnZombie> spawnsValidos = new List<PuntoSpawnZombie>();

            foreach (ZonaZombies zona in todasLasZonas)
            {
                if (zona.estaActiva) spawnsValidos.AddRange(zona.puntosDeSpawn);
            }

            if (spawnsValidos.Count == 0) { yield return new WaitForSeconds(2f); continue; }

            var spawnsOrdenados = spawnsValidos.OrderBy(spawn =>
            {
                float distanciaMinima = float.MaxValue;
                foreach (var cliente in clientes)
                {
                    if (cliente.PlayerObject != null)
                    {
                        float dist = Vector3.Distance(spawn.transform.position, cliente.PlayerObject.transform.position);
                        if (dist < distanciaMinima) distanciaMinima = dist;
                    }
                }
                return distanciaMinima;
            }).ToList();

            int puntosAElegir = Mathf.Min(spawnsSimultaneos, spawnsOrdenados.Count);
            int zombiesEnEstaOleada = Mathf.Min(puntosAElegir, zombiesPorGenerar.Value);

            for (int i = 0; i < zombiesEnEstaOleada; i++)
            {
                GenerarUnZombieEnPunto(spawnsOrdenados[i].transform);
            }

            yield return new WaitForSeconds(tiempoEntreOleadas);
        }
    }

    private void GenerarUnZombieEnPunto(Transform punto)
    {
        GameObject nuevoZombie = Instantiate(prefabZombie, punto.position, punto.rotation);
        nuevoZombie.GetComponent<NetworkObject>().Spawn(true);
        zombiesPorGenerar.Value--;
        zombiesVivos.Value++;
    }

    public void ZombieEliminado()
    {
        if (!IsServer) return;

        zombiesVivos.Value--;

        // Solo cambiamos de ronda si estamos en una ronda normal
        if (estadoActual.Value == EstadoJuego.RondaNormal || estadoActual.Value == EstadoJuego.RondaEspecial)
        {
            if (zombiesVivos.Value <= 0 && zombiesPorGenerar.Value <= 0)
            {
                StartCoroutine(IniciarSiguienteRonda());
            }
        }
    }

    // ==========================================
    // SISTEMA DE LOCKDOWN (CUARENTENA DEL ALTAR)
    // ==========================================

    public void IniciarLockdownDeSupervivencia(float duracion, List<PuntoSpawnZombie> spawnsAltar, AltarRitual altar)
    {
        if (!IsServer) return;

        altarActual = altar;
        estadoActual.Value = EstadoJuego.RondaAguantar;
        zombiesGuardadosParaLuego = zombiesPorGenerar.Value + zombiesVivos.Value;

        GameObject[] todosLosZombis = GameObject.FindGameObjectsWithTag("Zombie");
        foreach (GameObject z in todosLosZombis)
        {
            if (z.TryGetComponent(out NetworkObject netObj))
            {
                netObj.Despawn(true);
            }
        }

        zombiesVivos.Value = 0;
        zombiesPorGenerar.Value = 0;

        Debug.Log($"<color=cyan>[GameManager] ¡INICIA EL LOCKDOWN DE {duracion} SEGUNDOS!</color>");

        StartCoroutine(RutinaRelojLockdown(duracion));
        StartCoroutine(RutinaGeneradorLockdown(spawnsAltar));
    }

    // El Reloj se queda igual
    private IEnumerator RutinaRelojLockdown(float duracionRestante)
    {
        while (duracionRestante > 0)
        {
            duracionRestante -= 1f;
            yield return new WaitForSeconds(1f);
        }
        TerminarLockdown();
    }

    // CORRECCIÓN: Ahora lee la posición extrayéndola de PuntoSpawnZombie
    private IEnumerator RutinaGeneradorLockdown(List<PuntoSpawnZombie> spawns)
    {
        int topeZombisLockdown = 12;

        while (estadoActual.Value == EstadoJuego.RondaAguantar)
        {
            if (zombiesVivos.Value < topeZombisLockdown && spawns.Count > 0)
            {
                PuntoSpawnZombie spawnElegido = spawns[Random.Range(0, spawns.Count)];

                // Extraemos el transform interno del punto de spawn
                GameObject nuevoZombie = Instantiate(prefabZombie, spawnElegido.transform.position, spawnElegido.transform.rotation);
                nuevoZombie.GetComponent<NetworkObject>().Spawn(true);

                zombiesVivos.Value++;
            }
            yield return new WaitForSeconds(1.5f);
        }
    }

    private IEnumerator RutinaGeneradorLockdown(List<Transform> spawns)
    {
        // Durante el Lockdown, generamos zombies constantemente si hay menos del tope
        int topeZombisLockdown = 12;

        while (estadoActual.Value == EstadoJuego.RondaAguantar)
        {
            if (zombiesVivos.Value < topeZombisLockdown && spawns.Count > 0)
            {
                // Elegir un spawn al azar de la lista del altar
                Transform spawnElegido = spawns[Random.Range(0, spawns.Count)];

                GameObject nuevoZombie = Instantiate(prefabZombie, spawnElegido.position, spawnElegido.rotation);
                nuevoZombie.GetComponent<NetworkObject>().Spawn(true);

                zombiesVivos.Value++;
            }

            yield return new WaitForSeconds(1.5f); // Ritmo de aparición agresivo
        }
    }

    private void TerminarLockdown()
    {
        Debug.Log("<color=green>[GameManager] ¡LOCKDOWN SUPERADO!</color>");

        // 1. Limpiamos a los zombis del ritual
        GameObject[] todosLosZombis = GameObject.FindGameObjectsWithTag("Zombie");
        foreach (GameObject z in todosLosZombis)
        {
            if (z.TryGetComponent(out NetworkObject netObj)) netObj.Despawn(true);
        }

        // 2. Avisamos al altar de que abra las puertas
        if (altarActual != null) altarActual.CompletarRitual();

        // 3. Devolvemos los zombis que robamos antes del evento a la cola
        zombiesVivos.Value = 0;
        zombiesPorGenerar.Value = zombiesGuardadosParaLuego;

        // 4. Reanudamos la ronda normal
        estadoActual.Value = EstadoJuego.RondaNormal;
        StartCoroutine(RutinaGenerarZombies());
    }

    public void ComprobarEstadoEquipo() { /* Tu código de gameOver */ }
}