using UnityEngine;

public class EventosAnimacionZombie : MonoBehaviour
{
    [Tooltip("Arrastra aquí el Sphere Collider de la mano derecha")]
    public Collider hitboxMano;

    // Esta función la llamará la animación justo cuando pega el zarpazo
    public void ActivarHitboxMano()
    {
        if (hitboxMano != null) hitboxMano.enabled = true;
    }

    // Esta función la llamará la animación cuando termine el golpe
    public void DesactivarHitboxMano()
    {
        if (hitboxMano != null) hitboxMano.enabled = false;
    }
}