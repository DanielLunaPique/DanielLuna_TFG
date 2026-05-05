using UnityEngine;
using Unity.Netcode;

public class SoporteDiana : NetworkBehaviour
{
    [Header("Búsqueda Automática")]
    public string nombreDelGestor = "NombreDeTuObjetoGestor"; // Pon el nombre exacto del objeto en la jerarquía
    [HideInInspector] public GestorDianas miGestor;

    [Header("Recorrido")]
    public Transform puntoA;
    public Transform puntoB;
    public float velocidad = 3f;

    private Vector3 destinoActual;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            if (miGestor == null) miGestor = GameObject.Find(nombreDelGestor)?.GetComponent<GestorDianas>();

            if (puntoA != null)
            {
                transform.position = puntoA.position;
                destinoActual = puntoB.position;
            }
        }
    }

    void Update()
    {
        if (!IsServer || miGestor == null) return;

        // Solo se mueve si el juego está activo
        if (miGestor.juegoActivo.Value)
        {
            if (puntoA == null || puntoB == null) return;

            transform.position = Vector3.MoveTowards(transform.position, destinoActual, velocidad * Time.deltaTime);

            if (Vector3.Distance(transform.position, destinoActual) < 0.01f)
            {
                destinoActual = (destinoActual == puntoA.position) ? puntoB.position : puntoA.position;
            }
        }
    }

    public void ResetearPosicion()
    {
        if (IsServer && puntoA != null)
        {
            transform.position = puntoA.position;
            destinoActual = puntoB.position;
        }
    }
}