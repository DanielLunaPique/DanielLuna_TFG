using System.Text;
using Unity.Netcode;
using UnityEngine;

public class ServerConnectionHandler : MonoBehaviour
{
    private void Start()
    {
        // Solo el Host o Servidor debe gestionar las aprobaciones de conexión
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        }
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // CAMBIO AQUÍ: Usamos .Payload en lugar de .ConnectionData
        byte[] connectionData = request.Payload;

        if (connectionData == null || connectionData.Length == 0)
        {
            // Si alguien intenta entrar sin nombre, lo rechazamos
            response.Approved = false;
            response.Reason = "Identificación militar inválida (Sin nombre).";
            return;
        }

        // 2. Traducir los bytes de vuelta a un String (El nombre del jugador)
        string clientNickname = Encoding.ASCII.GetString(connectionData);
        Debug.Log($"[SERVIDOR] El jugador '{clientNickname}' está intentando unirse con ID de red: {request.ClientNetworkId}");

        // 3. Control de límite de jugadores (Máximo 4)
        if (NetworkManager.Singleton.ConnectedClients.Count >= 4)
        {
            response.Approved = false;
            response.Reason = "El escuadrón ya está completo (Máximo 4 jugadores).";
            return;
        }

        // 4. Si todo está correcto, aprobamos la conexión
        response.Approved = true;
        response.CreatePlayerObject = false; // Spawnea el prefab del jugador automáticamente
    }

    private void OnDestroy()
    {
        // Limpieza al destruir el objeto o cambiar de escena
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback = null;
        }
    }
}