using UnityEngine;

public class ParteDelCuerpo : MonoBehaviour
{
    public Zombie zombiePrincipal;
    public float multiplicadorDaño = 1f;

    [Header("Headshot")]
    [Tooltip("Marca esto SOLO en el collider de la cabeza")]
    public bool esCabeza = false; // <--- NUEVA CASILLA

    // Tu función que recibe el daño desde el arma
    public void RecibirDisparo(int dañoBase, ulong idAtacante)
    {
        if (zombiePrincipal != null)
        {
            int dañoFinal = Mathf.RoundToInt(dañoBase * multiplicadorDaño);

            // AHORA le pasamos también el chivato de si es la cabeza
            zombiePrincipal.TakeDamageServerRpc(dañoFinal, idAtacante, esCabeza);
        }
    }
}