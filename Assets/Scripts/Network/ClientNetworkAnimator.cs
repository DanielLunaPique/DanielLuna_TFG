using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkAnimator : NetworkAnimator
{
    // Esta es la clave: Le decimos a Unity que NO queremos que el servidor sea el jefe de las animaciones.
    // Queremos que el dueño del personaje (el cliente) mande sus propias animaciones al resto.
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}