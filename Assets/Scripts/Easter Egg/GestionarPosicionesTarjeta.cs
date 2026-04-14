using UnityEngine;

public class GestorPosicionesTarjeta : MonoBehaviour
{
    public static GestorPosicionesTarjeta Instance;
    public GameObject objetoTarjetaFisica; // El prefab de la tarjeta ya puesto en la escena
    public Transform[] puntosDeSpawn; // Arrastra aquí los 4 Transforms (sitios)

    private void Awake() { Instance = this; }

    public void AparecerTarjetaAleatoria()
    {
        int indice = Random.Range(0, puntosDeSpawn.Length);
        objetoTarjetaFisica.transform.position = puntosDeSpawn[indice].position;
        objetoTarjetaFisica.transform.rotation = puntosDeSpawn[indice].rotation;
        objetoTarjetaFisica.SetActive(true);

        Debug.Log($"[EasterEgg] Tarjeta generada en ubicación: {indice}");
    }
}