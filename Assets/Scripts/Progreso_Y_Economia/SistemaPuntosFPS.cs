using UnityEngine;
using Unity.Netcode;
using System;

public class SistemaPuntosFPS : NetworkBehaviour
{
    [Header("Economia del jugador")]
    public NetworkVariable<int> puntos = new NetworkVariable<int>(
        50000,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public event Action<int> OnPuntosCambiados;

    public override void OnNetworkSpawn()
    {
        puntos.OnValueChanged += (valorAnterior, valorNuevo) =>
        {
            if (IsOwner)
            {
                OnPuntosCambiados?.Invoke(valorNuevo);
                Debug.Log($"[Bolsillo] Puntos actualizados: {valorNuevo}");
            }
        };
    }

    public void SumarPuntos(int cantidad)
    {
        if (!IsOwner) return;

        puntos.Value += cantidad;
    }

    public bool IntentarComprar(int coste)
    {
        if (!IsOwner) return false;

        if(puntos.Value >= coste)
        {
            puntos.Value -= coste;
            return true;
        }

        return false;
    }
}
