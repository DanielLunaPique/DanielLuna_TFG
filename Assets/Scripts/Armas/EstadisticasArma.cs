using UnityEngine;


[CreateAssetMenu(fileName = "NuevaArma", menuName = "Arsenal/Estadisticas de arma")]
public class EstadisticasArma : ScriptableObject
{
    [Header("Informacion General")]
    public string nombreArma = "Arma Desconocida";
    public GameObject prefabArma;

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

    [Header("Position")]
    public Vector3 position;

    [Header("Position")]
    public Vector3 rotation;

    public enum TipoDisparo
    {
        Automatico,
        Semiautomatico,
        Rafaga
    }
}
