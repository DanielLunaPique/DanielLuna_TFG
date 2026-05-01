using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class GestorPuzzleRunas : NetworkBehaviour
{
    [Header("Configuración")]
    public List<PiedraRuna> listaPiedras = new List<PiedraRuna>();
    public GameObject prefabPiezaBaston;
    public Transform puntoAparicionPieza;

    private List<PiedraRuna> combinacionCorrecta = new List<PiedraRuna>();
    private int pasoActual = 0;

    public NetworkVariable<bool> puzzleActivo = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> puzzleCompletado = new NetworkVariable<bool>(false);

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GenerarCombinacion();
        }
    }

    // Llama a esta función desde el script de tu Easter Egg cuando toque este paso
    public void ActivarPuzzle()
    {
        if (!IsServer || puzzleActivo.Value) return;

        puzzleActivo.Value = true;

        // Hacemos visibles todas las piedras en la red
        foreach (var piedra in listaPiedras)
        {
            if (piedra != null) piedra.piedraVisible.Value = true;
        }

        Debug.Log("<color=yellow>[PUZZLE RUNAS] Puzzle activado. ¡Las piedras han aparecido!</color>");
    }

    private void GenerarCombinacion()
    {
        // Elegimos 3 piedras al azar de la lista como la solución correcta
        var rng = new System.Random();
        var piedrasBarajadas = listaPiedras.OrderBy(a => rng.Next()).ToList();

        combinacionCorrecta = piedrasBarajadas.Take(3).ToList();

        // Chivato en consola (usa el nombre del GameObject de la piedra para que sepas cuáles son)
        Debug.Log($"<color=cyan>[PUZZLE RUNAS] La combinación correcta es: 1º[{combinacionCorrecta[0].gameObject.name}], 2º[{combinacionCorrecta[1].gameObject.name}], 3º[{combinacionCorrecta[2].gameObject.name}]</color>");
    }

    public void ComprobarDisparoServer(PiedraRuna piedraDisparada)
    {
        if (!IsServer || !puzzleActivo.Value || puzzleCompletado.Value) return;

        // Comprobamos si la piedra disparada es la que toca en el paso actual
        if (piedraDisparada == combinacionCorrecta[pasoActual])
        {
            piedraDisparada.estadoBrillo.Value = 1; // Brillo Dorado (Acierto)
            pasoActual++;

            if (pasoActual >= 3)
            {
                CompletarPuzzle();
            }
        }
        else
        {
            // Error: Reiniciamos la secuencia
            pasoActual = 0;
            foreach (var p in listaPiedras)
            {
                if (p != null) p.estadoBrillo.Value = 2; // Brillo Rojo (Error)
            }
            Invoke(nameof(ResetearVisual), 1f);
        }
    }

    private void ResetearVisual()
    {
        foreach (var p in listaPiedras)
        {
            if (p != null) p.estadoBrillo.Value = 0; // Vuelven a la normalidad
        }
    }

    private void CompletarPuzzle()
    {
        puzzleCompletado.Value = true;

        // 1. Damos la recompensa
        if (prefabPiezaBaston != null && puntoAparicionPieza != null)
        {
            GameObject pieza = Instantiate(prefabPiezaBaston, puntoAparicionPieza.position, Quaternion.identity);
            pieza.GetComponent<NetworkObject>().Spawn(true);
        }

        // 2. Activamos el efecto de desaparición en todas las piedras
        foreach (var p in listaPiedras)
        {
            if (p != null) p.DesaparecerPiedraClientRpc();
        }
    }
}