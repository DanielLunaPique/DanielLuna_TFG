using UnityEngine;

public class DatosArma : MonoBehaviour
{
    [Header("Estadisticas Base")]
    public EstadisticasArma estadisticas;

    [Header("Posiciones de IK (Agarres reales del arma)")]
    public Transform agarreManoIzquierda;
    //public Transform agarreManoDerecha; // Por si en el futuro lo usas

    [Header("Posición de Apuntado (ADS)")]
    public Transform puntoDeMira;

    [Header("Posición de Esprintar")]
    [Tooltip("Cómo se coloca el arma cuando corres (ej. apuntando un poco hacia abajo y al lado)")]
    public Vector3 posicionSprint = new Vector3(0.05f, 0.1f, 0.1f);
    public Vector3 rotacionSprint = new Vector3(10f, -10f, -5f);

    [Header("Estado Actual (No tocar en el editor)")]
    public int balasActuales;
    public int balasReserva;

    [Header("Retroceso (Kick Visual)")]
    [Tooltip("Cuánto salta el arma hacia atrás (Z negativo)")]
    public float retrocesoZ = -0.5f;
    [Tooltip("Cuánto sube el cañón hacia arriba (X negativo)")]
    public float retrocesoRotacionX = -1f;
    [Tooltip("Inclinación lateral aleatoria máxima para darle realismo")]
    public float retrocesoRotacionYAleatoria = 0.2f;


    [Header("Precisión y Dispersión")]
    [Tooltip("Dispersión base desde la cadera. (Ej: 0.02 es bastante preciso, 0.1 es muy impreciso)")]
    public float dispersionBase = 0.05f;

    public float recoilCamaraArriba = 1.5f; 
    public float recoilCamaraLado = 0.3f;

    [Header("Comportamiento del Retroceso (Cámara)")]
    [Tooltip("Lo rápido y agresivo que pega el latigazo visual")]
    public float velocidadRetrocesoCamara = 10f;
    [Tooltip("El tope máximo de grados que puede subir en una ráfaga continua")]
    public float topeRetrocesoVertical = 15f;
    [Tooltip("La fuerza del imán al soltar el gatillo (0 para desactivar)")]
    public float fuerzaTironRegreso = 6f;

    // Esta función la llamaremos cuando el arma nazca para que se llene de balas
    private void Awake()
    {
        if (estadisticas != null)
        {
            balasActuales = estadisticas.balasCargador;
            balasReserva = estadisticas.balasCargador * 9; // Empezamos con 3 cargadores de repuesto
        }
    }
}