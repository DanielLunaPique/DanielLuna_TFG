using UnityEngine;

public class PuntoSpawnZombie : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}
