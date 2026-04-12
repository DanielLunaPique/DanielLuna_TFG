using UnityEngine;

// Esto añade un botón en el menú de click derecho de Unity para crear la misión
[CreateAssetMenu(fileName = "NuevoPasoEnergia", menuName = "EasterEgg/Paso Energía")]
public class PasoEnergia : QuestStep
{
    // Al ser el paso de energía básico, no necesita lógica extra por ahora.
    // Solo hereda el nombre, el texto del HUD y la capacidad de completarse.
}