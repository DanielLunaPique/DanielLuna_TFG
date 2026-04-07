using UnityEngine;
using Unity.Netcode;

public class ManoZombie : MonoBehaviour
{
    public Collider hitboxMano;
    public int dañoAtaque = 20;

    [Tooltip("Arrastra aquí el objeto principal que tiene el script Zombie.cs")]
    public Zombie zombiePrincipal;

    void Start()
    {
        // Nos aseguramos de que el zombie nazca con las manos "inofensivas"
        if (hitboxMano != null) hitboxMano.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // REGLA DE ORO MULTIJUGADOR: Solo el Servidor decide si el golpe ha dado o no.
        if (zombiePrincipal == null || !zombiePrincipal.IsServer) return;

        // Si la mano choca contra el Jugador
        if (other.CompareTag("Player"))
        {
            // Buscamos su script de salud y le pasamos el daño
            SaludJugador salud = other.GetComponent<SaludJugador>();
            if (salud != null && !salud.estaMuerto)
            {
                salud.RecibirDaño(dañoAtaque);
                Debug.Log("<color=red>¡ZASCA! El zombie te ha dado un manotazo.</color>");
            }

            // Apagamos la mano instantáneamente para no hacerle daño 2 veces seguidas
            hitboxMano.enabled = false;
        }
    }
}