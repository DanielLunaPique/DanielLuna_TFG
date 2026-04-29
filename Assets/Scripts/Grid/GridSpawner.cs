using UnityEngine;

public class GridSpawner : MonoBehaviour
{
    public int width;
    public int height;
    public int cellsize;
    public GameObject gridSpawnPos;

    // Renombramos la variable interna para ocultarla
    private Custom_Grid _grid;

    // Esta es la "Propiedad Inteligente" a la que accederán los demás scripts
    public Custom_Grid grid
    {
        get
        {
            if (_grid == null)
            {
                Vector3 pos = gridSpawnPos != null ? gridSpawnPos.transform.position : Vector3.zero;
                
                _grid = new Custom_Grid(width, height, cellsize, pos);
            }
            return _grid;
        }
        set 
        {
            _grid = value;
        }
    }

    void Awake()
    {
        // Lo creamos al principio como siempre, pero ahora estamos protegidos por el 'get'
        if (_grid == null && gridSpawnPos != null)
        {
            _grid = new Custom_Grid(width, height, cellsize, gridSpawnPos.transform.position);
        }
    }
}