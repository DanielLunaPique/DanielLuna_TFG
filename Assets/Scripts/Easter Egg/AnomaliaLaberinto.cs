using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AnomaliaLaberinto : NetworkBehaviour
{
    [Header("Configuración de Escolta")]
    public float velocidadMovimiento = 2.5f;
    public float distanciaMaximaEscolta = 6f;

    [Header("Recompensa")]
    public GameObject prefabPuntaLanza; // El prefab de la pieza del arma (debe estar en NetworkPrefabs)

    // Variables Internas
    private EllersMazeGenerator mazeGen;
    private GridSpawner gridSpwn;
    private List<Vector3> rutaActual;
    private int indiceRuta = 0;
    private ulong idJugadorEscoltando;
    [HideInInspector] public bool enMovimiento = false;

    // Al nacer, busca los scripts del mapa automáticamente
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            mazeGen = FindObjectOfType<EllersMazeGenerator>();
            gridSpwn = FindObjectOfType<GridSpawner>();
        }
    }

    // Llamado por el Hangar
    public void ArrancarEscolta(ulong idJugador)
    {
        if (!IsServer) return;

        // ESCUDO 1: Si OnNetworkSpawn llegó tarde, buscamos las referencias a la fuerza
        if (mazeGen == null) mazeGen = FindObjectOfType<EllersMazeGenerator>();
        if (gridSpwn == null) gridSpwn = FindObjectOfType<GridSpawner>();

        // ESCUDO 2: Si aun así no existen, abortamos sin crashear el juego
        if (mazeGen == null || gridSpwn == null)
        {
            Debug.LogError("[Orbe] Error Crítico: No se encontraron los gestores del laberinto.");
            GetComponent<NetworkObject>().Despawn(true);
            return;
        }

        idJugadorEscoltando = idJugador;
        Vector3 destinoCentro = mazeGen.ObtenerCentroDelLaberinto();

        // Calculamos la ruta
        rutaActual = AStarPathfinding.FindPath(transform.position, destinoCentro, mazeGen.GetLogicalMap(), gridSpwn.grid);

        if (rutaActual != null && rutaActual.Count > 0)
        {
            indiceRuta = 0;
            enMovimiento = true;
        }
        else
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    private void Update()
    {
        if (IsServer && enMovimiento)
        {
            ProcesarMovimientoYDistancia();
        }
    }

    private void ProcesarMovimientoYDistancia()
    {
        // 1. ¿El jugador sigue en la partida?
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(idJugadorEscoltando))
        {
            IniciarDesvanecimiento();
            return;
        }

        Transform transformEscoltador = NetworkManager.Singleton.ConnectedClients[idJugadorEscoltando].PlayerObject.transform;

        // 2. Control de distancia
        if (Vector3.Distance(transform.position, transformEscoltador.position) > distanciaMaximaEscolta)
        {
            // Podrías añadir un ClientRpc aquí para hacer un sonido de "Fallo"
            IniciarDesvanecimiento();
            return;
        }

        // 3. Mover por los puntos de A*
        if (indiceRuta < rutaActual.Count)
        {
            Vector3 objetivo = rutaActual[indiceRuta];
            objetivo.y = transform.position.y;

            transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidadMovimiento * Time.deltaTime);

            if (Vector3.Distance(transform.position, objetivo) < 0.1f)
            {
                indiceRuta++;
            }
        }
        else
        {
            CompletarEscolta();
        }
    }

    private void IniciarDesvanecimiento()
    {
        if (!IsServer) return;

        // En lugar de destruir, iniciamos la secuencia visual
        StartCoroutine(RutinaFalloEscolta());
    }

    private IEnumerator RutinaFalloEscolta()
    {
        enMovimiento = false; // Detenemos el A*

        float tiempoEfecto = 1.0f;
        float transcurrido = 0f;
        Vector3 escalaOriginal = transform.localScale;

        while (transcurrido < tiempoEfecto)
        {
            transcurrido += Time.deltaTime;
            float porcentaje = transcurrido / tiempoEfecto;

            // Efecto de encogimiento (Shrink)
            transform.localScale = Vector3.Lerp(escalaOriginal, Vector3.zero, porcentaje);

            yield return null;
        }

        // Una vez terminado el efecto visual, entonces sí, lo borramos de la red
        if (IsServer)
        {
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    private void CompletarEscolta()
    {
        enMovimiento = false;

        if (prefabPuntaLanza != null)
        {
            // Calculamos la posición con el offset de -0.6 en el eje Y
            Vector3 posicionAjustada = new Vector3(transform.position.x, transform.position.y - 0.6f, transform.position.z);

            GameObject nuevaPunta = Instantiate(prefabPuntaLanza, posicionAjustada, Quaternion.identity);
            nuevaPunta.GetComponent<NetworkObject>().Spawn(true);
        }

        GetComponent<NetworkObject>().Despawn(true);
    }
}