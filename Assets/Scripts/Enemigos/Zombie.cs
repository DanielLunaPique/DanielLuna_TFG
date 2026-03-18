using UnityEngine;
using Unity.Netcode;


public class Zombie : MonoBehaviour
{
    [Header("Estadisticas")]
    public NetworkVariable<int> salud = new NetworkVariable<int>(100);

    public int puntosPorImpacto = 10;
    public int puntosPorMuerte = 70;

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ulong idAtacante)
    {
        if (salud.Value <= 0) return;

        salud.Value -= damage;

        OtorgarPuntosClienteRpc(puntosPorImpacto, idAtacante);

        if(salud.Value <= 0)
        {
            OtorgarPuntosClienteRpc(puntosPorMuerte, idAtacante);

            GameManager.Instance.ZombieEliminado();

            GetComponent<NetworkObject>().Despawn();
        }
    }

    [ClientRpc]
    private void OtorgarPuntosClienteRpc (int cantidad, ulong idAtacante)
    {
        if(NetworkManager.Singleton.LocalClientId == idAtacante)
        {
            var miJugador = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if(miJugador != null)
            {
                var bolsillo = miJugador.GetComponent<SistemaPuntosFPS>();
                if(bolsillo != null)
                {
                    bolsillo.SumarPuntos(cantidad);
                }
            }
        }
    }
}
