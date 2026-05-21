using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class PlayerNameTag : NetworkBehaviour
{
    [Tooltip("Arrastra aquí el componente TextMeshPro que está sobre la cabeza")]
    public TextMeshProUGUI NameText;

    // Variable de red que guarda el nombre sincronizado
    public NetworkVariable<FixedString32Bytes> NetworkedName = new NetworkVariable<FixedString32Bytes>(
        "",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    void Start()
    {
        NetworkedName.OnValueChanged += OnNameChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Si somos el dueño, leemos nuestro nombre y lo subimos a la red
            string savedName = PlayerPrefs.GetString("PlayerName", "Player");
            NetworkedName.Value = new FixedString32Bytes(savedName);

            // Apagamos nuestro propio nombre para que no nos tape la vista en Primera Persona
            if (NameText != null) NameText.gameObject.SetActive(false);
        }
        else
        {
            // Si es otro jugador, mostramos su nombre
            UpdateNameTag(NetworkedName.Value.ToString());
        }
    }

    public override void OnNetworkDespawn()
    {
        NetworkedName.OnValueChanged -= OnNameChanged;
    }

    void OnNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        UpdateNameTag(newValue.ToString());
    }

    void UpdateNameTag(string newName)
    {
        if (NameText != null)
        {
            NameText.text = newName;
        }
    }

    // --- EFECTO BILLBOARD (Mirar a la cámara) ---
    void LateUpdate()
    {
        if (NameText != null && NameText.gameObject.activeInHierarchy && Camera.main != null)
        {
            // Hacemos que el cartel mire exactamente a la cámara, invirtiendo la dirección 
            // para que veamos la parte frontal de las letras y no la trasera invisible.
            NameText.transform.LookAt(
                NameText.transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up
            );
        }
    }
}