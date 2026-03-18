using UnityEngine;
using UnityEngine.Events; // Necesario para los eventos

public class SistemaSaludFPS : MonoBehaviour
{
    [Header("Configuración de Salud")]
    public float saludMaxima = 100f;
    public float saludActual;

    [Header("Eventos")]
    [Tooltip("Aquí podrás arrastrar cosas que pasen cuando mueras (Ej: Mostrar UI)")]
    public UnityEvent alMorir;

    private bool estaMuerto = false;

    void Start()
    {
        // Empezamos con la vida a tope
        saludActual = saludMaxima;
    }

    // Cualquier enemigo o bala que te golpee llamará a esta función
    public void RecibirDano(float dano)
    {
        if (estaMuerto) return; // Si ya estás muerto, ignoramos el daño

        saludActual -= dano;
        Debug.Log($"¡Ah! Me han dado. Salud restante: {saludActual}");

        if (saludActual <= 0)
        {
            saludActual = 0;
            Morir();
        }
    }

    public void Curar(float cantidad)
    {
        if (estaMuerto) return;

        saludActual += cantidad;
        if (saludActual > saludMaxima) saludActual = saludMaxima;
    }

    private void Morir()
    {
        estaMuerto = true;
        Debug.Log("¡El jugador ha muerto!");

        // Dispara todos los eventos que configures en el Inspector
        alMorir.Invoke();
    }
}