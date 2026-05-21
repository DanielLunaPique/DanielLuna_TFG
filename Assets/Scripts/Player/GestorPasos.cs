using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GestorPasosFPS : MonoBehaviour
{
    [Header("Configuración de Audio")]
    public AudioSource audioSource;
    public float volumenPasos = 0.5f;

    [Header("Sonidos por Material")]
    public AudioClip[] pasosAsfalto;
    public AudioClip[] pasosTierra;
    public AudioClip[] pasosLodo;

    [Header("Detección de Suelo")]
    public Transform puntoPies; // Un objeto vacío en los pies del jugador
    public LayerMask capaSuelo; // La capa donde está todo tu suelo/terrain

    // Índices de textura del Terrain (Míralos en tu Terrain -> Paint Texture)
    // El orden en el que las añadiste. Ej: Si la tierra fue la primera (0) y el lodo la segunda (1).
    public int indiceTexturaTierra = 4;
    public int indiceTexturaLodo = 1;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    // Esta función la llamarás cada vez que el jugador dé un paso
    public void ReproducirSonidoPaso()
    {
        // 1. Tiramos un rayo hacia abajo para ver qué pisamos
        if (Physics.Raycast(puntoPies.position, Vector3.down, out RaycastHit hit, 2f, capaSuelo))
        {
            PhysicsMaterial matFisico = hit.collider.sharedMaterial;

            if (matFisico != null)
            {
                // Comparamos por el nombre del archivo que creamos
                if (matFisico.name == "Mat_Asfalto")
                {
                    ReproducirClipAleatorio(pasosAsfalto);
                }
            }
            // 3. ¿Hemos pisado el Terrain?
            else
            {
                Terrain terrain = hit.collider.GetComponent<Terrain>();
                int texturaDominante = ObtenerTexturaDominanteTerrain(terrain, hit.point);

                if (texturaDominante == indiceTexturaTierra)
                {
                    ReproducirClipAleatorio(pasosTierra);
                }
                else if (texturaDominante == indiceTexturaLodo)
                {
                    ReproducirClipAleatorio(pasosLodo);
                }
                else
                {
                    // Por si pisas una textura que no tienes controlada, que suene a tierra por defecto
                    ReproducirClipAleatorio(pasosTierra);
                }
            }
        }
    }

    private void ReproducirClipAleatorio(AudioClip[] arrayClips)
    {
        if (arrayClips.Length > 0)
        {
            // Alteramos levemente el pitch para que no suene repetitivo (¡Game Feel!)
            audioSource.pitch = Random.Range(0.9f, 1.1f);

            AudioClip clipElegido = arrayClips[Random.Range(0, arrayClips.Length)];
            audioSource.PlayOneShot(clipElegido, volumenPasos);
        }
    }

    // ==========================================
    // MAGIA NEGRA PARA LEER EL TERRAIN
    // ==========================================
    private int ObtenerTexturaDominanteTerrain(Terrain terrain, Vector3 posicionImpacto)
    {
        TerrainData terrainData = terrain.terrainData;
        Vector3 posicionTerrain = terrain.transform.position;

        // Calculamos en qué punto del mapa alfa (Splatmap) estamos pisando
        int mapX = (int)(((posicionImpacto.x - posicionTerrain.x) / terrainData.size.x) * terrainData.alphamapWidth);
        int mapZ = (int)(((posicionImpacto.z - posicionTerrain.z) / terrainData.size.z) * terrainData.alphamapHeight);

        // Obtenemos los pesos (fuerza) de todas las texturas en ese punto exacto 1x1
        float[,,] datosSplatmap = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

        float mezclaMaxima = 0f;
        int indiceMaximo = 0;

        // Comparamos para ver qué textura tiene más "pintura" en este punto
        for (int i = 0; i < datosSplatmap.GetUpperBound(2) + 1; i++)
        {
            if (datosSplatmap[0, 0, i] > mezclaMaxima)
            {
                indiceMaximo = i;
                mezclaMaxima = datosSplatmap[0, 0, i];
            }
        }

        return indiceMaximo;
    }
}