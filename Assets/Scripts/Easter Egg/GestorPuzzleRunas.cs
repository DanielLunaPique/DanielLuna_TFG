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

    [Header("Audio del Puzzle")]
    public AudioSource audioFuentePuzzle;
    [Tooltip("Sonido cuando la piedra NO está en la combinación")]
    public AudioClip sonidoErrorAbsoluto;
    [Tooltip("Sonido cuando la piedra SÍ está, pero NO toca ahora")]
    public AudioClip sonidoOrdenIncorrecto;
    [Tooltip("Sonido cuando aciertas la piedra y el orden")]
    public AudioClip sonidoAcierto;

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

    public void ActivarPuzzle()
    {
        if (!IsServer || puzzleActivo.Value) return;

        puzzleActivo.Value = true;

        foreach (var piedra in listaPiedras)
        {
            if (piedra != null) piedra.piedraVisible.Value = true;
        }

        Debug.Log("<color=yellow>[PUZZLE RUNAS] Puzzle activado. ¡Las piedras han aparecido!</color>");
    }

    private void GenerarCombinacion()
    {
        var rng = new System.Random();
        var piedrasBarajadas = listaPiedras.OrderBy(a => rng.Next()).ToList();

        combinacionCorrecta = piedrasBarajadas.Take(3).ToList();

        Debug.Log($"<color=cyan>[PUZZLE RUNAS] Combinación: 1º[{combinacionCorrecta[0].name}], 2º[{combinacionCorrecta[1].name}], 3º[{combinacionCorrecta[2].name}]</color>");
    }

    public void ComprobarDisparoServer(PiedraRuna piedraDisparada)
    {
        if (!IsServer || !puzzleActivo.Value || puzzleCompletado.Value) return;

        // CASO 1: Acierto total (Piedra correcta en el orden correcto)
        if (piedraDisparada == combinacionCorrecta[pasoActual])
        {
            piedraDisparada.estadoBrillo.Value = 1; // Brillo Dorado
            ReproducirSonidoClientRpc(3); // Lanzamos el sonido de éxito a todos
            pasoActual++;

            if (pasoActual >= 3)
            {
                CompletarPuzzle();
            }
        }
        // CASO 2: Orden incorrecto (La piedra está en la combinación, pero no es la de ahora)
        else if (combinacionCorrecta.Contains(piedraDisparada))
        {
            ReproducirSonidoClientRpc(2); // Lanzamos el sonido de "Casi, pero no"
            FallarSecuencia();
        }
        // CASO 3: Error absoluto (La piedra ni siquiera pertenece a los 3 elegidos)
        else
        {
            ReproducirSonidoClientRpc(1); // Lanzamos el sonido de error grave
            FallarSecuencia();
        }
    }

    private void FallarSecuencia()
    {
        // 1. Reiniciamos el progreso matemático del jugador a cero
        pasoActual = 0;

        // 2. Castigo visual: Todas las piedras se ponen rojas para avisar del reinicio
        foreach (var p in listaPiedras)
        {
            if (p != null) p.estadoBrillo.Value = 2; // Brillo Rojo
        }

        // 3. Volvemos a apagarlas después de 1 segundo para que vuelvan a intentarlo
        Invoke(nameof(ResetearVisual), 1.2f);
    }

    private void ResetearVisual()
    {
        foreach (var p in listaPiedras)
        {
            if (p != null) p.estadoBrillo.Value = 0; // Vuelven a su estado apagado
        }
    }

    private void CompletarPuzzle()
    {
        puzzleCompletado.Value = true;

        if (prefabPiezaBaston != null && puntoAparicionPieza != null)
        {
            GameObject pieza = Instantiate(prefabPiezaBaston, puntoAparicionPieza.position, Quaternion.identity);
            pieza.GetComponent<NetworkObject>().Spawn(true);
        }

        foreach (var p in listaPiedras)
        {
            if (p != null) p.DesaparecerPiedraClientRpc();
        }
    }

    // --- MAGIA DE RED: El servidor ordena a los clientes que reproduzcan el sonido ---
    [ClientRpc]
    private void ReproducirSonidoClientRpc(int tipoSonido)
    {
        if (audioFuentePuzzle == null) return;

        switch (tipoSonido)
        {
            case 1:
                if (sonidoErrorAbsoluto != null) audioFuentePuzzle.PlayOneShot(sonidoErrorAbsoluto);
                break;
            case 2:
                if (sonidoOrdenIncorrecto != null) audioFuentePuzzle.PlayOneShot(sonidoOrdenIncorrecto);
                break;
            case 3:
                if (sonidoAcierto != null) audioFuentePuzzle.PlayOneShot(sonidoAcierto);
                break;
        }
    }
}