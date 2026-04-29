using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class GestorPuzzleRunas : NetworkBehaviour
{
    [Header("Configuración del Puzzle")]
    public PiedraRuna[] todasLasPiedras; // Arrastra las 8 piedras aquí
    public GameObject prefabPiezaBaston; // El prefab de la pieza final
    public Transform puntoAparicionPieza;

    // Lista de símbolos rúnicos (Puedes cambiarlos por los que más te gusten)
    public readonly string[] listaSimbolos = { "ᚠ", "ᚢ", "ᚦ", "ᚨ", "ᚱ", "ᚲ", "ᚷ", "ᚹ" };

    [Header("Estado de Red")]
    public NetworkVariable<bool> puzzleCompletado = new NetworkVariable<bool>(false);

    // Variables internas del servidor
    private List<int> combinacionCorrecta = new List<int>();
    private int pasoActual = 0;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GenerarPuzzleAleatorio();
        }
    }

    private void GenerarPuzzleAleatorio()
    {
        // 1. Barajamos los índices de los símbolos (0 al 7)
        List<int> indicesDisponibles = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        var rng = new System.Random();
        indicesDisponibles = indicesDisponibles.OrderBy(a => rng.Next()).ToList();

        // 2. Asignamos un símbolo único a cada piedra en el mapa
        for (int i = 0; i < todasLasPiedras.Length; i++)
        {
            if (todasLasPiedras[i] != null)
            {
                todasLasPiedras[i].indiceSimbologia.Value = indicesDisponibles[i];
                todasLasPiedras[i].estadoBrillo.Value = 0;
            }
        }

        // 3. Elegimos 3 piedras al azar para que sean la combinación correcta
        List<PiedraRuna> piedrasBarajadas = todasLasPiedras.OrderBy(a => rng.Next()).ToList();
        combinacionCorrecta.Clear();

        for (int i = 0; i < 3; i++)
        {
            combinacionCorrecta.Add(piedrasBarajadas[i].indiceSimbologia.Value);
        }

        pasoActual = 0;

        // EL CHIVATO PARA LA CONSOLA (Para que tú sepas la clave)
        string clave = $"1º[{listaSimbolos[combinacionCorrecta[0]]}] - " +
                       $"2º[{listaSimbolos[combinacionCorrecta[1]]}] - " +
                       $"3º[{listaSimbolos[combinacionCorrecta[2]]}]";
        Debug.Log($"<color=cyan>[PUZZLE RUNAS] La combinación secreta es: {clave}</color>");
    }

    // Esta función la llama la piedra cuando le disparas
    public void ComprobarDisparoServer(PiedraRuna runaDisparada)
    {
        if (!IsServer || puzzleCompletado.Value) return;

        int simboloDisparado = runaDisparada.indiceSimbologia.Value;

        // ¿Ha acertado el paso actual?
        if (simboloDisparado == combinacionCorrecta[pasoActual])
        {
            Debug.Log($"[PUZZLE RUNAS] ¡Acierto! Paso {pasoActual + 1}/3 completado.");
            runaDisparada.estadoBrillo.Value = 1; // Se queda encendida en Dorado
            pasoActual++;

            // Aquí pondrás el sonido de "Acierto" más adelante
            // Ejemplo: AudioSource.PlayClipAtPoint(sonidoAcierto, runaDisparada.transform.position);

            if (pasoActual >= 3)
            {
                CompletarPuzzle();
            }
        }
        else
        {
            // Fallo. Reiniciamos todo.
            Debug.Log("[PUZZLE RUNAS] ¡Fallo! Secuencia incorrecta. Reiniciando...");

            // Aquí pondrás el sonido de "Error/Zumbido" más adelante

            pasoActual = 0;
            foreach (var piedra in todasLasPiedras)
            {
                if (piedra != null)
                {
                    piedra.estadoBrillo.Value = 2; // Estado 2 desencadena el parpadeo rojo
                }
            }

            // Para que no se queden en rojo para siempre, las devolvemos a estado 0 tras un breve retraso
            Invoke(nameof(ApagarTodasLasRunas), 1.0f);
        }
    }

    private void ApagarTodasLasRunas()
    {
        if (!IsServer) return;
        foreach (var piedra in todasLasPiedras)
        {
            if (piedra != null) piedra.estadoBrillo.Value = 0; // Vuelven a azul
        }
    }

    private void CompletarPuzzle()
    {
        Debug.Log("<color=green>[PUZZLE RUNAS] ¡PUZZLE COMPLETADO! Generando pieza del bastón...</color>");
        puzzleCompletado.Value = true;

        if (prefabPiezaBaston != null && puntoAparicionPieza != null)
        {
            GameObject pieza = Instantiate(prefabPiezaBaston, puntoAparicionPieza.position, Quaternion.identity);
            pieza.GetComponent<NetworkObject>().Spawn(true);
        }
    }
}