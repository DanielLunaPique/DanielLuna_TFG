using UnityEngine;

public class Custom_Grid : MonoBehaviour
{
    private int width;
    private int height;
    private int cellsize;
    private Vector3 originPosition;

    private int[,] gridArray;

    public Custom_Grid(int width, int height, int cellsize, Vector3 originPosition)
    {
        this.width = width;
        this.height = height;
        this.cellsize = cellsize;
        this.originPosition = originPosition;

        gridArray = new int[width, height];

        for(int i = 0; i < gridArray.GetLength(0); i++)
        {
            for(int j = 0; j < gridArray.GetLength(1); j++)
            {
                //Debug.DrawLine(GetWorldPosition(i, j), GetWorldPosition(i+1, j), Color.white, 1000f);
                //Debug.DrawLine(GetWorldPosition(i, j), GetWorldPosition(i, j+1), Color.white, 1000f);
            }
        }
    }

    public int GetCellSize()
    {
        return cellsize;
    }

    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellsize + originPosition;
    }

    public int[] GetXZ(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition - originPosition).x / cellsize);
        int z = Mathf.FloorToInt((worldPosition - originPosition).z / cellsize);

        return new int[] {x, z};
    }
}
