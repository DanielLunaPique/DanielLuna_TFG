using UnityEngine;
using Unity.Netcode;
using System;

public class SistemaPuntosFPS : NetworkBehaviour
{
    [Header("Economia del jugador")]
    public NetworkVariable<int> puntos = new NetworkVariable<int>(
        500,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public event Action<int> OnPuntosCambiados;

    public override void OnNetworkSpawn()
    {
        puntos.OnValueChanged += (valorAnterior, valorNuevo) =>
        {
            if (IsOwner)
            {
                OnPuntosCambiados?.Invoke(valorNuevo);
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
        // 1. Chivato inicial: ¿Quién ejecuta esto y cuánto dinero ve?
        //Debug.Log($"[BANCO] Intentando comprar algo de {coste} pts. Dinero actual en cuenta: {puntos.Value}. ¿Se ejecuta en el Servidor?: {IsServer}");

        // 2. Comprobamos si tiene suficiente (OJO: tiene que ser >=, no solo >)
        if (puntos.Value >= coste)
        {
            // Solo el servidor tiene permiso para restar el dinero real
            if (IsServer)
            {
                puntos.Value -= coste;
                //Debug.Log($"[BANCO] Compra APROBADA. Dinero restante: {puntos.Value}");
            }
            else
            {
                //Debug.LogWarning("[BANCO] Cuidado: Un cliente ha intentado ejecutar la resta de dinero localmente.");
            }

            return true;
        }
        else
        {
            SistemaVoces misVoces = GetComponent<SistemaVoces>();
            misVoces.ReproducirFrase(SistemaVoces.TipoVoz.SinPuntos);
            return false;
        }
    }
}
