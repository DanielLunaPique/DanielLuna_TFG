using UnityEngine;

[CreateAssetMenu(fileName = "NuevoPaso", menuName = "EasterEgg/Paso Basico")]
public class QuestStep : ScriptableObject
{
    public string ID;
    [TextArea] public string textoHUD;
}