using UnityEngine;

public class ParteDelCuerpo : MonoBehaviour
{
    [Tooltip("Arrastra aquí el objeto padre que tiene el script Zombie.cs")]
    public Zombie zombiePrincipal;

    [Tooltip("Ej: 2 para la cabeza, 1 para el pecho, 0.5 para las piernas")]
    public float multiplicadorDaño = 1f;

    // Esta función la llamará la bala cuando nos dé
    public void RecibirDisparo(int dañoBase, ulong idAtacante)
    {
        if (zombiePrincipal == null) return;

        // Calculamos el daño final redondeando al número entero más cercano
        int dañoFinal = Mathf.RoundToInt(dañoBase * multiplicadorDaño);

        // Le mandamos el daño ya multiplicado al script principal del zombie
        zombiePrincipal.TakeDamageServerRpc(dañoFinal, idAtacante);
    }
}