using UnityEngine;

public class SistemaEstaminaFPS : MonoBehaviour
{
    [Header("Configuración de Estamina")]
    public float estaminaMaxima = 100f;
    public float estaminaActual;

    [Header("Tasas (Por Segundo)")]
    [Tooltip("Cuánto gasta por segundo (Ej: 25 significa que corres 4 segundos)")]
    public float costeCorrer = 15f;
    [Tooltip("Cuánto recupera por segundo al caminar/estar quieto")]
    public float tasaRegeneracion = 15f;

    [HideInInspector] public bool puedeCorrer = true;
    private bool agotado = false;

    void Start()
    {
        estaminaActual = estaminaMaxima;
    }

    // Esta función la llamará el script de movimiento cuando estés corriendo
    public void ConsumirEstamina()
    {
        estaminaActual -= costeCorrer * Time.deltaTime;

        if (estaminaActual <= 0)
        {
            estaminaActual = 0;
            agotado = true;      // El jugador está asfixiado
            puedeCorrer = false; // Le cortamos el grifo del sprint
        }
    }

    // Esta función la llamará el script de movimiento cuando NO estés corriendo
    public void RegenerarEstamina()
    {
        if (estaminaActual < estaminaMaxima)
        {
            estaminaActual += tasaRegeneracion * Time.deltaTime;

            // Si estaba agotado, necesita recuperar al menos un 25% para poder volver a correr
            if (agotado && estaminaActual > estaminaMaxima * 0.25f)
            {
                agotado = false;
                puedeCorrer = true;
            }
            // Si no estaba agotado, siempre puede correr
            else if (!agotado)
            {
                puedeCorrer = true;
            }
        }
        else
        {
            estaminaActual = estaminaMaxima;
        }
    }
}