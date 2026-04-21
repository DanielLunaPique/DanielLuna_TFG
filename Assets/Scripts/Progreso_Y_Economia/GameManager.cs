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
        RondaEspecial, //Perros
        RondaAsedio,   //Zombies electricos origins
        RondaAguantar  //Para sobrevivir en una zona
    }

    [Header("Control de partida")]
    public NetworkVariable<EstadoJuego> estadoActual = new NetworkVariable<EstadoJuego> ();
    public NetworkVariable<int> rondaActual = new NetworkVariable<int> ();

    [Header("ContadorDeZombies")]
    public NetworkVariable<int> zombiesVivos = new NetworkVariable<int> (); 
    public NetworkVariable<int> zombiesPorGenerar = new NetworkVariable<int> ();

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
    public NetworkVariable<bool> pataDeCabraDesbloqueada = new NetworkVariable<bool> ();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(IniciarPrimeraRonda());
        }
    }

    private IEnumerator IniciarPrimeraRonda()
    {
        estadoActual.Value = EstadoJuego.Preparacion;
        yield return new WaitForSeconds(2f);

        rondaActual.Value++;
        Debug.Log($"[GameManager] ¡Empieza la Ronda {rondaActual.Value}! Zombies a generar: {zombiesPorGenerar.Value}");

        zombiesPorGenerar.Value = rondaActual.Value * 5;

        //Aqui se decide si la ronda es especial, etc
        estadoActual.Value = EstadoJuego.RondaNormal;

        StartCoroutine(RutinaGenerarZombies());
    }

    private IEnumerator IniciarSiguienteRonda()
    {
        estadoActual.Value = EstadoJuego.Preparacion;
        if (audioSourceUI != null && sonidoCambioRonda != null)
        {
            audioSourceUI.PlayOneShot(sonidoCambioRonda);
        }
        yield return new WaitForSeconds(tiempoPreparacion);

        rondaActual.Value++;
        Debug.Log($"[GameManager] ¡Empieza la Ronda {rondaActual.Value}! Zombies a generar: {zombiesPorGenerar.Value}");

        zombiesPorGenerar.Value = rondaActual.Value * 5;

        //Aqui se decide si la ronda es especial, etc
        estadoActual.Value = EstadoJuego.RondaNormal;

        StartCoroutine(RutinaGenerarZombies());
    }

    private IEnumerator RutinaGenerarZombies()
    {
        while ((estadoActual.Value == EstadoJuego.RondaNormal || estadoActual.Value == EstadoJuego.RondaEspecial)
               && zombiesPorGenerar.Value > 0)
        {
            // 1. Conseguir a todos los jugadores conectados
            var clientes = NetworkManager.Singleton.ConnectedClientsList;
            if (clientes.Count == 0)
            {
                yield return new WaitForSeconds(1f); // Esperar si no hay nadie
                continue;
            }

            // 2. Buscar todas las Zonas activas y sus spawns
            ZonaZombies[] todasLasZonas = FindObjectsOfType<ZonaZombies>();
            List<PuntoSpawnZombie> spawnsValidos = new List<PuntoSpawnZombie>();

            foreach (ZonaZombies zona in todasLasZonas)
            {
                if (zona.estaActiva) spawnsValidos.AddRange(zona.puntosDeSpawn);
            }

            if (spawnsValidos.Count == 0)
            {
                Debug.LogWarning("[GameManager] No hay spawns activos.");
                yield return new WaitForSeconds(2f);
                continue;
            }

            // 3. LA MAGIA: Ordenar los spawns por cercanía al jugador más cercano
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

            // 4. Coger solo los 5 más cercanos (o menos si el mapa tiene menos de 5)
            int puntosAElegir = Mathf.Min(spawnsSimultaneos, spawnsOrdenados.Count);

            // 5. Ver cuántos zombies nos quedan por generar. 
            // Si nos quedan 3, no vamos a generar 5 de golpe.
            int zombiesEnEstaOleada = Mathf.Min(puntosAElegir, zombiesPorGenerar.Value);

            // 6. ¡Generar simultáneamente!
            for (int i = 0; i < zombiesEnEstaOleada; i++)
            {
                GenerarUnZombieEnPunto(spawnsOrdenados[i].transform);
            }

            // 7. Pausa antes de la siguiente oleada (Delay para el mismo punto)
            yield return new WaitForSeconds(tiempoEntreOleadas);
        }
    }

    private void GenerarUnZombieEnPunto(Transform punto)
    {
        // Instanciamos y conectamos a la red
        GameObject nuevoZombie = Instantiate(prefabZombie, punto.position, punto.rotation);
        nuevoZombie.GetComponent<NetworkObject>().Spawn(true);

        // Actualizamos números
        zombiesPorGenerar.Value--;
        zombiesVivos.Value++;
    }

    public void ZombieEliminado()
    {
        if (!IsServer) return;

        zombiesVivos.Value--;
        if(zombiesVivos.Value <= 0 && zombiesPorGenerar.Value <= 0)
        {
            StartCoroutine(IniciarSiguienteRonda());
        }
    }

    // ==========================================
    // SISTEMA DE MUERTE Y ESPECTADOR
    // ==========================================
    public void ComprobarEstadoEquipo()
    {
        // Solo el Servidor es el juez absoluto
        if (!IsServer) return;

        int jugadoresVivos = 0;

        // Recorremos todos los clientes conectados
        foreach (var cliente in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (cliente.PlayerObject != null)
            {
                SaludJugador salud = cliente.PlayerObject.GetComponent<SaludJugador>();
                if (salud != null && !salud.estaMuerto)
                {
                    jugadoresVivos++;
                }
            }
        }

        if (jugadoresVivos == 0)
        {
            Debug.Log("<color=red>[GameManager] GAME OVER - TODOS HAN MUERTO</color>");
            // Aquí pondremos la rutina para cargar el menú principal
            // StartCoroutine(RutinaGameOver());
        }
        else
        {
            Debug.Log($"<color=yellow>[GameManager] Quedan {jugadoresVivos} jugadores vivos. La partida continúa.</color>");
        }
    }
}
