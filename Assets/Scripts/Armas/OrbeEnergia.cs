using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class OrbeEnergia : MonoBehaviour
{
    [HideInInspector] public int dañoAoE;
    [HideInInspector] public float radio;
    [HideInInspector] public ulong idAtacante;

    [Header("Efectos Visuales")]
    [Tooltip("Arrastra aquí el Prefab del efecto de explosión en el suelo")]
    public GameObject efectoExplosion;

    private bool yaExplotado = false;

    void Start()
    {
        // Seguro de vida: Si disparamos al cielo, la bola se destruye a los 6 segundos
        Destroy(gameObject, 6f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // En cuanto toca cualquier cosa sólida (suelo, pared, zombi), explota
        Explotar();
    }

    private void Explotar()
    {
        if (yaExplotado) return;
        yaExplotado = true;

        // 1. Efecto visual de la explosión
        if (efectoExplosion != null)
        {
            GameObject explosionObj = Instantiate(efectoExplosion, transform.position, Quaternion.identity);
            // Destruimos el objeto a los 2.5 segundos (ajusta este tiempo a lo que dure tu humo/fuego)
            Destroy(explosionObj, 1f);
        }

        // 2. ESCÁNER DE DAÑO: Creamos una esfera invisible y pillamos todo lo que hay dentro
        Collider[] objetosAlcanzados = Physics.OverlapSphere(transform.position, radio);

        // Usamos una lista para recordar a quién le hemos pegado.
        // Si un zombi tiene hitbox en pierna, brazo y cabeza, no queremos matarlo 3 veces seguidas.
        HashSet<int> zombiesDañados = new HashSet<int>();

        foreach (Collider col in objetosAlcanzados)
        {
            ParteDelCuerpo hitbox = col.GetComponent<ParteDelCuerpo>();
            if (hitbox != null)
            {
                // Obtenemos el ID único del zombi "padre"
                int zombieID = hitbox.transform.root.GetInstanceID();

                // Si no le hemos pegado ya en esta explosión, le clavamos el daño
                if (!zombiesDañados.Contains(zombieID))
                {
                    // Le mandamos el daño a la hitbox. (Usa daño plano al pecho para evitar multiplicadores rotos por AoE)
                    hitbox.RecibirDisparo(dañoAoE, idAtacante);
                    zombiesDañados.Add(zombieID);
                }
            }
        }

        // 3. Borramos la bola de energía
        Destroy(gameObject);
    }

    // Dibujamos el radio en la escena para ayudarte a calibrar el tamaño
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}