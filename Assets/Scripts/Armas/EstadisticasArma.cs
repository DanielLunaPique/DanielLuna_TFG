using UnityEngine;


[CreateAssetMenu(fileName = "NuevaArma", menuName = "Arsenal/Estadisticas de arma")]
public class EstadisticasArma : ScriptableObject
{
    [Header("Informacion General")]
    public string nombreArma = "Arma Desconocida";
    public GameObject prefabArma;
    public GameObject prefabVisualCaja;

    [Header("Efectos Visuales")]
    [Tooltip("El sistema de partículas que sale del cañón al disparar")]
    public GameObject efectoFogonazo;

    [Header("Estadisticas de Combate")]
    public int daño = 25;
    public float cadenciaDisparo = 0.1f;

    [Header("Municion y Recarga")]
    public int balasCargador = 30;
    public float tiempoRecarga = 2.0f;

    [Header("Modo de Disparo")]
    public TipoDisparo modoDisparo = TipoDisparo.Automatico;
    public int balasPorRafaga = 3;

    [Header("Alcance")]
    public int alcance = 0;

    [Header("Armas Especiales (Proyectiles)")]
    [Tooltip("El Prefab de la bola de energía")]
    public GameObject prefabProyectil;
    [Tooltip("La velocidad a la que vuela la bola")]
    public float velocidadProyectil = 40f;
    [Tooltip("El tamaño de la explosión al chocar (en metros)")]
    public float radioExplosion = 4f;

    [Header("Position")]
    public Vector3 position;

    [Header("Position")]
    public Vector3 rotation;

    public enum TipoDisparo
    {
        Automatico,
        Semiautomatico,
        Rafaga, 
        ProyectilFisico
    }
}
