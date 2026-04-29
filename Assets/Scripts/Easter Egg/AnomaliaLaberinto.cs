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
    public GameObject prefabPuntaLanza;

    private EllersMazeGenerator mazeGen;
    private List<Vector3> rutaActual;
    private int indiceRuta = 0;
    private ulong idJugadorEscoltando;
    [HideInInspector] public bool enMovimiento = false;

    public void ArrancarEscolta(ulong idJugador)
    {
        if (!IsServer) return;

        mazeGen = EllersMazeGenerator.Instance;
        if (mazeGen == null)
        {
            GetComponent<NetworkObject>().Despawn(true);
            return;
        }

        idJugadorEscoltando = idJugador;
        Vector3 destinoCentro = mazeGen.ObtenerCentroDelLaberinto();

        try
        {
            rutaActual = AStarPathfinding.FindPath(transform.position, destinoCentro, mazeGen.GetLogicalMap(), mazeGen.ObtenerGrid());
        }
        catch
        {
            rutaActual = null;
        }

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
        if (IsServer && enMovimiento) ProcesarMovimientoYDistancia();
    }

    private void ProcesarMovimientoYDistancia()
    {
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(idJugadorEscoltando))
        {
            IniciarDesvanecimiento();
            return;
        }

        Transform transformEscoltador = NetworkManager.Singleton.ConnectedClients[idJugadorEscoltando].PlayerObject.transform;

        if (Vector3.Distance(transform.position, transformEscoltador.position) > distanciaMaximaEscolta)
        {
            IniciarDesvanecimiento();
            return;
        }

        if (indiceRuta < rutaActual.Count)
        {
            Vector3 objetivo = rutaActual[indiceRuta];
            objetivo.y = transform.position.y;

            transform.position = Vector3.MoveTowards(transform.position, objetivo, velocidadMovimiento * Time.deltaTime);

            if (Vector3.Distance(transform.position, objetivo) < 0.1f) indiceRuta++;
        }
        else CompletarEscolta();
    }

    private void IniciarDesvanecimiento()
    {
        if (!IsServer) return;
        StartCoroutine(RutinaFalloEscolta());
    }

    private IEnumerator RutinaFalloEscolta()
    {
        enMovimiento = false;
        float transcurrido = 0f;
        Vector3 escalaOriginal = transform.localScale;

        while (transcurrido < 1.0f)
        {
            transcurrido += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaOriginal, Vector3.zero, transcurrido);
            yield return null;
        }

        if (IsServer) GetComponent<NetworkObject>().Despawn(true);
    }

    private void CompletarEscolta()
    {
        enMovimiento = false;
        if (prefabPuntaLanza != null)
        {
            Vector3 pos = new Vector3(transform.position.x, transform.position.y - 0.6f, transform.position.z);
            GameObject punta = Instantiate(prefabPuntaLanza, pos, Quaternion.identity);
            punta.GetComponent<NetworkObject>().Spawn(true);
        }
        GetComponent<NetworkObject>().Despawn(true);
    }
}