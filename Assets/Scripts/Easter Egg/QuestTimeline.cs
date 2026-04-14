using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ElementoTimeline
{
    public enum TipoElemento { Fijo, Aleatorio }
    public TipoElemento tipo;

    [Tooltip("Si es Fijo, arrastra la misión obligatoria aquí")]
    public QuestStep pasoFijo;

    [Tooltip("Si es Aleatorio, arrastra aquí las posibles misiones para esta casilla")]
    public List<QuestStep> poolAleatorio;

    [Range(0, 100)]
    public float probabilidadDeActivacion = 100f; // Por si quieres que a veces se salte esta casilla
}

[CreateAssetMenu(fileName = "NuevaLineaTiempo", menuName = "EasterEgg/Linea de Tiempo")]
public class QuestTimeline : ScriptableObject
{
    public List<ElementoTimeline> secuencia;
}