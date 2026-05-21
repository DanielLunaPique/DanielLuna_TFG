using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement; // <-- IMPORTANTE: Necesario para cambiar al menú

public class GameManager : NetworkBehaviour
{
    public enum EstadoJuego
    {
        EsperandoJugadores,
        Preparacion,
        RondaNormal,
        RondaEspecial,
        RondaAsedio,
        RondaAguantar,
        Derrota // <-- NUEVO ESTADO AÑADIDO
    }

    [Header("Control de partida")]
    public NetworkVariable<EstadoJuego> estadoActual = new NetworkVariable<EstadoJuego>();
    public NetworkVariable<int> rondaActual = new NetworkVariable<int>();

    [Header("ContadorDeZombies")]
    public NetworkVariable<int> zombiesVivos = new NetworkVariable<int>();
    public NetworkVariable<int> zombiesPorGenerar = new NetworkVariable<int>();

    [Header("Ajustes de Spawns")]
    public float tiempoPreparacion = 10f;
    public int spawnsSimultaneos = 5;
    public float tiempoEntreOleadas = 4f;
    [Tooltip("El tiempo que tarda entre que sale un zombie y el siguiente de la misma oleada")]
    public float tiempoEntreSpawnsAlternados = 0.5f;

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
    private AltarRitual altarActual;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer) StartCoroutine(IniciarPrimeraRonda());
    }

    private int CalcularZombiesPorRonda(int ronda)
    {
        int numJugadores = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (numJugadores <= 0) numJugadores = 1;

        float curvaExponencial = Mathf.Pow(ronda, 1.5f);
        int total = Mathf.FloorToInt(6 + (ronda * 2) + curvaExponencial) * numJugadores;

        return Mathf.Min(total, 500);
    }

    private IEnumerator IniciarPrimeraRonda()
    {
        estadoActual.Value = EstadoJuego.Preparacion;
        yield return new WaitForSeconds(2f);

        rondaActual.Value++;
        zombiesPorGenerar.Value = CalcularZombiesPorRonda(rondaActual.Value);
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
        zombiesPorGenerar.Value = CalcularZombiesPorRonda(rondaActual.Value);
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
                yield return new WaitForSeconds(tiempoEntreSpawnsAlternados);
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
            if (z.TryGetComponent(out NetworkObject netObj)) netObj.Despawn(true);
        }

        zombiesVivos.Value = 0;
        zombiesPorGenerar.Value = 0;

        Debug.Log($"<color=cyan>[GameManager] ¡INICIA EL LOCKDOWN DE {duracion} SEGUNDOS!</color>");

        StartCoroutine(RutinaRelojLockdown(duracion));
        StartCoroutine(RutinaGeneradorLockdown(spawnsAltar));
    }

    private IEnumerator RutinaRelojLockdown(float duracionRestante)
    {
        while (duracionRestante > 0)
        {
            duracionRestante -= 1f;
            yield return new WaitForSeconds(1f);
        }

        StartCoroutine(RutinaTerminarLockdown());
    }

    private IEnumerator RutinaGeneradorLockdown(List<PuntoSpawnZombie> spawns)
    {
        int topeZombisLockdown = 24;

        while (estadoActual.Value == EstadoJuego.RondaAguantar)
        {
            if (zombiesVivos.Value < topeZombisLockdown && spawns.Count > 0)
            {
                int cantidadASpawnear = Mathf.Min(2, topeZombisLockdown - zombiesVivos.Value);

                for (int i = 0; i < cantidadASpawnear; i++)
                {
                    PuntoSpawnZombie spawnElegido = spawns[Random.Range(0, spawns.Count)];
                    GameObject nuevoZombie = Instantiate(prefabZombie, spawnElegido.transform.position, spawnElegido.transform.rotation);

                    if (nuevoZombie.TryGetComponent(out ZombieIA ia))
                    {
                        ia.velocidadBaseInicial = ia.velocidadMaxima;
                    }

                    nuevoZombie.GetComponent<NetworkObject>().Spawn(true);
                    zombiesVivos.Value++;

                    yield return new WaitForSeconds(tiempoEntreSpawnsAlternados);
                }
            }
            yield return new WaitForSeconds(1.0f);
        }
    }

    private IEnumerator RutinaTerminarLockdown()
    {
        Debug.Log("<color=green>[GameManager] ¡LOCKDOWN SUPERADO! Limpiando almas...</color>");

        if (altarActual != null) altarActual.CompletarRitual();

        GameObject[] todosLosZombis = GameObject.FindGameObjectsWithTag("Zombie");
        foreach (GameObject z in todosLosZombis)
        {
            if (z.TryGetComponent(out Zombie scriptZombie))
            {
                scriptZombie.TakeDamageServerRpc(99999, 0, false);
            }
        }

        yield return new WaitForSeconds(4f);

        zombiesVivos.Value = 0;
        zombiesPorGenerar.Value = zombiesGuardadosParaLuego;

        estadoActual.Value = EstadoJuego.RondaNormal;
        StartCoroutine(RutinaGenerarZombies());
    }

    // ==========================================
    // SISTEMA DE DERROTA Y CÁMARA CINEMATOGRÁFICA
    // ==========================================

    /// <summary>
    /// Esta función la debe llamar el script de vida del jugador cuando muere.
    /// Pásale la posición del jugador en el momento de morir.
    /// </summary>
    public void ComprobarEstadoEquipo(Vector3 posicionMuerte)
    {
        if (!IsServer) return;

        // Si ya hemos perdido, no ejecutamos esto 20 veces
        if (estadoActual.Value == EstadoJuego.Derrota) return;

        // Comprobamos si el equipo entero ha muerto. 
        // (Asumo que esta función solo se llama cuando ya no quedan vivos, 
        // o si prefieres, añade aquí un bucle for que cuente la vida de todos).

        estadoActual.Value = EstadoJuego.Derrota;

        // Le mandamos a TODOS los jugadores la señal para ejecutar la animación
        MostrarAnimacionDerrotaClientRpc(posicionMuerte);
    }

    [ClientRpc]
    private void MostrarAnimacionDerrotaClientRpc(Vector3 posicionMuerte)
    {
        StartCoroutine(RutinaAnimacionDerrota(posicionMuerte));
    }

    private IEnumerator RutinaAnimacionDerrota(Vector3 posicionInicial)
    {
        // 1. Apagamos todas las cámaras de los jugadores para que no interfieran
        Camera[] camaras = FindObjectsOfType<Camera>();
        foreach (Camera cam in camaras)
        {
            cam.enabled = false;
            // Apagamos también los audífonos para que Unity no se queje de "2 AudioListeners"
            if (cam.gameObject.TryGetComponent(out AudioListener al)) al.enabled = false;
        }

        // 2. Creamos una nueva cámara de la nada en el punto de la muerte
        GameObject camaraCinematica = new GameObject("CamaraDerrota_Cinematica");
        camaraCinematica.AddComponent<Camera>();
        camaraCinematica.AddComponent<AudioListener>();

        // Empezamos 1 metro por encima del cuerpo, mirando hacia abajo para el dramatismo
        Vector3 posicionInicio = posicionInicial + Vector3.up * 5f;
        camaraCinematica.transform.position = posicionInicio;
        camaraCinematica.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // 3. Animamos la cámara subiendo lentamente durante 4.5 segundos
        float duracionAnimacion = 7f;
        float tiempoPasado = 0f;

        // Destino: 3 metros por encima de donde empezó
        Vector3 posicionDestino = posicionInicio + Vector3.up * 3f;

        while (tiempoPasado < duracionAnimacion)
        {
            camaraCinematica.transform.position = Vector3.Lerp(posicionInicio, posicionDestino, tiempoPasado / duracionAnimacion);
            tiempoPasado += Time.deltaTime;
            yield return null;
        }

        // 4. Se acabó el show. Apagamos los servidores de forma limpia
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // 5. Destruimos al propio GameManager para evitar problemas de duplicados al reconectar
        Destroy(gameObject);

        // 6. Volvemos al menú principal.
        // OJO: Asegúrate de que tu escena de menú se llame EXACTAMENTE así en el Build Settings
        SceneManager.LoadScene("Main Menu");
    }
}