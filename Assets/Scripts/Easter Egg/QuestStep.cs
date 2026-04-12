using UnityEngine;

// No es un MonoBehaviour, es una clase de la que heredarán otros scripts
public abstract class QuestStep : ScriptableObject
{
    [Header("Información de la Misión")]
    public string nombreMision;
    [TextArea]
    public string textoHUD; // Lo que lee el jugador en la esquina ("Objetivo: Activar la energía")

    // Variables de control (solo las lee/escribe el Servidor)
    [HideInInspector] public bool estaCompletada = false;

    // 1. Lo que ocurre justo al empezar esta misión
    public virtual void EmpezarMision()
    {
        estaCompletada = false;
        Debug.Log($"[QuestStep] Empezando misión: {nombreMision}");
    }

    // 2. Aquí va la lógica interna que el servidor comprobará cada frame (si hace falta)
    public virtual void ActualizarMision()
    {
        // Se sobrescribe en las misiones complejas (ej. Sobrevivir 60 segundos)
    }

    // 3. Lo que ocurre al superar la misión
    public virtual void CompletarMision()
    {
        estaCompletada = true;
        Debug.Log($"[QuestStep] Misión completada: {nombreMision}");
    }
}