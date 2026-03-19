using UnityEngine;

public class EfectoFlotar : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float velocidadRotacion = 45f; // Cuánto gira por segundo
    public float amplitudFlote = 0.2f;    // Cuánto sube y baja
    public float velocidadFlote = 2f;     // Lo rápido que sube y baja

    private Vector3 posicionInicial;

    void Start()
    {
        // Guardamos dónde la has colocado en el editor para no moverla de ahí
        posicionInicial = transform.localPosition;
    }

    void Update()
    {
        // 1. Rotación suave sobre sí misma
        transform.Rotate(Vector3.up * velocidadRotacion * Time.deltaTime, Space.World);

        // 2. Flote mágico usando matemáticas (Seno)
        float nuevaY = posicionInicial.y + Mathf.Sin(Time.time * velocidadFlote) * amplitudFlote;

        // Aplicamos la nueva posición
        transform.localPosition = new Vector3(posicionInicial.x, nuevaY, posicionInicial.z);
    }
}