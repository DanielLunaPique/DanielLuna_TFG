using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public int width;
    public int height;
    public int cellsize;
    public GameObject gridSpawnPos;
    [HideInInspector] public Custom_Grid grid;

    void Awake()
    {
        grid = new Custom_Grid(width, height, cellsize, gridSpawnPos.transform.position);
    }
}
