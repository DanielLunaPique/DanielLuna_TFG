using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ZonaZombies : MonoBehaviour
{
    [Header("Estado de la zona")]
    public bool esZonaInicial = false;
    public bool estaActiva = false;

    [Header("Spawns de esta zona")]
    public List<PuntoSpawnZombie> puntosDeSpawn = new List<PuntoSpawnZombie>();

    private void Start()
    {
        if (esZonaInicial)
        {
            estaActiva = true;
        }
    }

    [ContextMenu("Autocompletar Zona")]
    public void AutocompletarSpawns()
    {
        puntosDeSpawn = new List<PuntoSpawnZombie>(GetComponentsInChildren<PuntoSpawnZombie>());
        Debug.Log($"Se han encontrado {puntosDeSpawn.Count} puntos de spawn en {gameObject.name}.");
    }
}
