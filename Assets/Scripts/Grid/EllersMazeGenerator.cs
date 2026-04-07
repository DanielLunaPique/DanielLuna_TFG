using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

static class Extensions
{
    public static IEnumerable<(int, T)> Enumerate<T>(
        this IEnumerable<T> input,
        int start = 0
    )
    {
        int i = start;
        foreach(var t in input)
            yield return (i++, t);
    }
}

public struct DeadEndCandidate
{
    public int x;
    public int z;
    public CellData cellData;

    public DeadEndCandidate(int x, int z, CellData cellData)
    {
        this.x = x;
        this.z = z;
        this.cellData = cellData;
    }

}

public class EllersMazeGenerator : MonoBehaviour
{
    [SerializeField] GridSpawner gridSpawner;

    private Transform wallsContainer;
    private Transform propsContainer;

    [Range(0, 100)] [SerializeField] int WallSpawnPercentage = 0;
    [Range(0, 100)] [SerializeField] int B_WallSpawnPercentage = 0;

    [SerializeField] GameObject wall;
    [SerializeField] GameObject hangar;
    [HideInInspector] public GameObject targetHangar;
    [HideInInspector] public List<Transform> generatedHangars = new List<Transform>();

    [Header("Decoración Procedural")]
    [SerializeField] GameObject[] barrelPrefabs; 
    [SerializeField] GameObject streetLampPrefab;
    [Range(0, 100)] [SerializeField] int decorationChance = 15;

    [Header("Spawns de Zombis")]
    [SerializeField] GameObject zonaZombiesPrefab; // Un prefab vacío que tenga tu script ZonaZombies
    [SerializeField] GameObject puntoSpawnPrefab;  // El prefab que tiene el script PuntoSpawnZombie
    [SerializeField] int maxZombies = 6;
    [SerializeField] float minZombieDistance = 3.0f;
    
    [Header("Conexion con el mapa")]
    public PuertaDesbloqueable puertaDesbloqueable;

    private CellData[,] logicalMap;


    private Custom_Grid gridXZ;
    private int max_set_val = 1;
    private float yValue = 5.57f;

    void Start()
    {
        gridXZ = gridSpawner.grid; 
        
        logicalMap = new CellData[gridSpawner.width, gridSpawner.height];

        for(int i = 0; i < gridSpawner.width; i++)
        {
            for(int j = 0; j < gridSpawner.height; j++)
            {
                logicalMap[i, j] = gameObject.AddComponent<CellData>();
            }
        }

        wallsContainer = new GameObject("--- WALLS ---").transform;
        propsContainer = new GameObject("--- PROPS ---").transform;

        SpawnEllersMaze();

        ScanForDeadEnds();

        DecorateMaze();
    }

    private void SpawnEllersMaze()
    {
        List<(int[], int)> first_row = new List<(int[], int)>();

        for(int i = 0; i < gridSpawner.width; i++)
        {
            int[] coords = new int[] {i, 0};
            var cellTup = (coords, max_set_val);

            first_row.Add(cellTup);

            max_set_val += 1;
        }

        //WorldText_Row(first_row, Color.white, 0);

        PlaceWalls_SameDir(first_row, Vector3.right);

        List<(int[], int)> row = first_row;
        List<int[]> sameSetWalls = new List<int[]>();

        for(int i = 0; i < gridSpawner.height; i++)
        {
            if(i == gridSpawner.height - 1)
            {
                PlaceWalls_SameDir(IncreasedRow(1, row), Vector3.right);
                var last_row = JoinRow_Last(row);
                //WorldText_Row(last_row, Color.red, 1);
                break;
            }

            var joined_row = JoinRow_VerticalWalls(row, sameSetWalls);
            //WorldText_Row(joined_row, Color.red, 1); 

            BottomWalls(joined_row, out List<int[]> b_wall_loc);

            row = IncreasedRow(1, EmptyCellsNewRow(joined_row, b_wall_loc));
        }
    }

    private List<(int[], int)> JoinRow_VerticalWalls(List<(int[], int)> row, List<int[]> sameSetWalls)
    {
        var row_copy = new List<(int[], int)>(row);

        PlaceWall_XZ(Vector3.forward, row_copy[0].Item1[0], row_copy[0].Item1[1]);
        for(int i = 0; i < row.Count; i++)
        {
            if(i + 1 < row_copy.Count)
            {
                var curr_cell = row_copy[i];
                var next_cell = row_copy[i+1];
                bool join = false;
                bool sameSet = false;

                if(curr_cell.Item2 == next_cell.Item2)
                {
                    join = false;
                    sameSet = true;
                }
                else
                {
                    int rand_num = UnityEngine.Random.Range(0, 101);

                    if(inRange_Inclusive(rand_num, 0, WallSpawnPercentage))
                    {
                        join = false;
                    }
                    else
                    {
                        join = true;
                    }

                    if (!join)
                    {
                        var coord = next_cell.Item1;

                        PlaceWall_XZ(Vector3.forward, coord[0], coord[1]);

                        if (sameSet)
                        {
                            sameSetWalls.Add(new int[] { coord[0], coord[1] +1});
                        }
                    }
                    else
                    {
                        var newCellTup = (next_cell.Item1, curr_cell.Item2);
                        row_copy[i + 1] = newCellTup;
                    }
                }
            }
        }

        PlaceWall_XZ(Vector3.forward, row_copy[row_copy.Count -1].Item1[0] +1, row_copy[row_copy.Count -1].Item1[1]);
        return row_copy;
    }

    private void BottomWalls(List<(int[], int)> row, out List<int[]> bottomWallLocations)
    {
        var row_copy = new List<(int[], int)>(row);

        var bwall_cells = new List<(int[], int)>();
        var no_bwall_cells = new List<(int[], int)>();

        foreach(var set_list in RowSortedBySet(row_copy))
        {
            var shuffledSets = set_list.OrderBy(a => rng.Next()).ToList();

            foreach(var (i, cell) in shuffledSets.Enumerate())
            {
                if(i == 0)
                {
                    no_bwall_cells.Add(cell);
                    continue;
                }
                int rand_num = UnityEngine.Random.Range(0, 101);

                if(inRange_Inclusive(rand_num, 0, B_WallSpawnPercentage))
                {
                    bwall_cells.Add(cell);
                }
                else
                {
                    no_bwall_cells.Add(cell);
                }
            }
        }

        List<int[]> bWalls = new List<int[]>();

        foreach(var cell in bwall_cells)
        {
            bWalls.Add(cell.Item1);
        }
        bottomWallLocations = bWalls;

        foreach(var cellTup in IncreasedRow(1, bwall_cells))
        {
            var coords = cellTup.Item1;
            PlaceWall_XZ(Vector3.right, coords[0], coords[1]);
        }
    }

    private List<(int[], int)> EmptyCellsNewRow(List<(int[], int)> row, List<int[]> empty_cells)
    {
        var new_row = new List<(int[], int)>(row);
        foreach(var (i, cell) in row.Enumerate())
        {
            int cellX = cell.Item1[0];
            int cellZ = cell.Item1[1];

            foreach(var coords in empty_cells)
            {
                int eCellX = coords[0];
                int eCellZ = coords[1];

                if(cellX == eCellX && cellZ == eCellZ)
                {
                    var new_cell_tup = (cell.Item1, max_set_val);
                    new_row[i] = new_cell_tup;
                    max_set_val += 1;
                }
            }
        }
        return new_row;
    }

    private List<(int[], int)> JoinRow_Last(List<(int[], int)> row)
    {
        var row_copy = new List<(int[], int)>(row);

        PlaceWall_XZ(Vector3.forward, row_copy[0].Item1[0], row_copy[0].Item1[1]);
        for(int i = 0; i < row.Count; i++)
        {
            if(i + 1 < row_copy.Count)
            {
                var curr_cell = row_copy[i];
                var next_cell = row_copy[i+1];
                
                if(curr_cell.Item2 != next_cell.Item2)
                {
                    // Different sets: Must join (remove wall)
                    var newCellTup = (next_cell.Item1, curr_cell.Item2);
                    row_copy[i + 1] = newCellTup;
                }
                else
                {
                    // Same set: Must place wall
                    var coord = next_cell.Item1;
                    PlaceWall_XZ(Vector3.forward, coord[0], coord[1]);
                }
            }
        }

        PlaceWall_XZ(Vector3.forward, row_copy[row_copy.Count -1].Item1[0] +1, row_copy[row_copy.Count -1].Item1[1]);
        return row_copy;
    }

    
    #region HelperFunctions

    private static System.Random rng = new System.Random();

    private List<(int[], int)> IncreasedRow(int add_amount, List<(int[], int)> row)
    {
        var new_row = new List<(int[], int)>();

        foreach(var cellTup in row)
        {
            var coords = cellTup.Item1;
            var set = cellTup.Item2;

            int x = coords[0];
            int z = coords[1] + add_amount;

            int[] new_coords = {x,z};

            var new_cell_tup = (new_coords, set);

            new_row.Add(new_cell_tup);
        }
        return new_row;
    }

    private List<List<(int[], int)>> RowSortedBySet(List<(int[], int)> row)
    {
        return row
            .Select((x) => new{Value = x})
            .GroupBy(x => x.Value.Item2)
            .Select(x => x.Select(v => v.Value).ToList())
            .ToList();
    }

    private void PlaceWalls_SameDir(List<(int[], int)> row, Vector3 dir)
    {
        foreach(var cellTup in row)
        {
            var coords = cellTup.Item1;

            PlaceWall_XZ(dir, coords[0], coords[1]);
        }
    }

    private void PlaceWall_XZ(Vector3 dir, int x, int z)
    {
        if (dir == Vector3.right && z == gridSpawner.height)
        {
            // Calculamos cuáles son las dos casillas del centro
            int centroDerecha = gridSpawner.width / 2;
            int centroIzquierda = centroDerecha - 1;

            // Si la coordenada X coincide con alguna de las dos centrales...
            if (x == centroDerecha || x == centroIzquierda)
            {
                // ¡Abortamos la función! 
                // Al hacer 'return', no se instancia el muro visual y, lo más importante,
                // no se actualiza el logicalMap, por lo que el A* sabrá que ahí NO hay pared (puerta abierta).
                return;
            }
        }

        Vector3 position = gridXZ.GetWorldPosition(x, z);
        position.y = yValue;

        if(x <= gridSpawner.width && z <= gridSpawner.height)
        {
            var w =GameObject.Instantiate(wall, position, Quaternion.LookRotation(dir));
            w.transform.parent = wallsContainer;
        }

        if(dir == Vector3.forward)
        {
            if(IsInBounds(x, z)) logicalMap[x,z].wallLeft = true;

            if(IsInBounds(x-1, z)) logicalMap[x-1, z].wallRight = true;
        }
        else if(dir == Vector3.right)
        {
            if(IsInBounds(x, z)) logicalMap[x, z].wallBottom = true;

            if(IsInBounds(x, z-1)) logicalMap[x, z-1].wallTop = true;
        }
    }

    private bool inRange_Inclusive(int num, int minRange, int maxRange)
    {
        if(minRange <= num && num <= maxRange)
        {
            return true;
        }

        else
        {
            return false;
        }
    }

    private void WorldText_Row(List<(int[], int)> row, Color textColor, int yVal)
    {
        GameObject rowObj = new GameObject();
        foreach(var cellTup in row)
        {
            var coords = cellTup.Item1;
            var set = cellTup.Item2;

            var text = new GameObject();
            var mesh = text.AddComponent<TextMesh>();
            mesh.text = set.ToString();
            mesh.color = textColor;

            Vector3 gridPos = gridXZ.GetWorldPosition(coords[0], coords[1]);
            gridPos = new Vector3(gridPos.x + gridXZ.GetCellSize() / 2, yVal, gridPos.z + gridXZ.GetCellSize() / 2);
            text.transform.position = gridPos;
            text.transform.parent = rowObj.transform;
        }
    }


    private bool IsInBounds(int x, int z)
    {
        return x >= 0 && x < gridSpawner.width && z >= 0 && z < gridSpawner.height;
    }

    private void ScanForDeadEnds()
    {
        generatedHangars.Clear();
        targetHangar = null;

        List<DeadEndCandidate> allCandidates = new List<DeadEndCandidate>();
        for(int x = 0; x < gridSpawner.width; x++)
        {
            for(int z = 0; z < gridSpawner.height; z++)
            {
                CellData cell = logicalMap[x, z];

                int wallCount = 0;
                if(cell.wallTop) wallCount++;
                if(cell.wallBottom) wallCount++;
                if(cell.wallLeft) wallCount++;
                if(cell.wallRight) wallCount++;

                if(wallCount == 3)
                {    
                    allCandidates.Add(new DeadEndCandidate(x, z, cell));
                }
            }
        }

        if (allCandidates.Count == 0) 
        {
            Debug.LogWarning("No se encontraron Dead Ends en el laberinto.");
            return;
        }

        var rng = new System.Random();
        allCandidates = allCandidates.OrderBy(a => rng.Next()).ToList();

        List<DeadEndCandidate> selectedHangars = new List<DeadEndCandidate>();
        int maxHangars = 4;

        float minDistanceBetweenHangars = 4.0f;

        foreach(var candidate in allCandidates)
        {
            if(selectedHangars.Count >= maxHangars) break;

            bool isFarEnough = true;

            foreach(var selected in selectedHangars)
            {
                float dist = Vector2.Distance(new Vector2(candidate.x, candidate.z), new Vector2(selected.x, selected.z));
                if(dist < minDistanceBetweenHangars)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough)
            {
                selectedHangars.Add(candidate);
                SpawnHangar(candidate.x, candidate.z, candidate.cellData);
            }
        }
        
        if(selectedHangars.Count < 4 && allCandidates.Count >= 4)
        {
            foreach(var candidate in allCandidates)
            {
                if(selectedHangars.Count >= maxHangars) break;
                
                if(!selectedHangars.Exists(h => h.x == candidate.x && h.z == candidate.z))
                {
                    selectedHangars.Add(candidate);
                    SpawnHangar(candidate.x, candidate.z, candidate.cellData);
                }
            }
        }

        if (generatedHangars.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, generatedHangars.Count);
            targetHangar = generatedHangars[randomIndex].gameObject;
        }

        // --- Filtrar los Dead Ends sobrantes para los Zombis ---
        List<DeadEndCandidate> leftoverCandidates = new List<DeadEndCandidate>();

        foreach (var candidate in allCandidates)
        {
            // Comprobamos si este candidato YA fue usado para un hangar
            bool isUsedForHangar = selectedHangars.Exists(h => h.x == candidate.x && h.z == candidate.z);

            if (!isUsedForHangar)
            {
                leftoverCandidates.Add(candidate);
            }
        }

        // Llamamos a la función para instanciar los spawns
        SpawnZombiePoints(leftoverCandidates);
    }

    private void SpawnZombiePoints(List<DeadEndCandidate> candidates)
    {
        if (zonaZombiesPrefab == null || puntoSpawnPrefab == null) return;

        // 1. Instanciar el contenedor principal de la zona
        GameObject zonaObj = Instantiate(zonaZombiesPrefab, Vector3.zero, Quaternion.identity);
        zonaObj.SetActive(true);
        ZonaZombies zonaScript = zonaObj.GetComponent<ZonaZombies>();

        // 2. Barajar los candidatos sobrantes
        var rng = new System.Random();
        candidates = candidates.OrderBy(a => rng.Next()).ToList();
        List<DeadEndCandidate> selectedSpawns = new List<DeadEndCandidate>();

        // 3. Filtrado por distancia
        foreach (var candidate in candidates)
        {
            if (selectedSpawns.Count >= maxZombies) break;

            bool isFarEnough = true;
            foreach (var selected in selectedSpawns)
            {
                float dist = Vector2.Distance(new Vector2(candidate.x, candidate.z), new Vector2(selected.x, selected.z));
                if (dist < minZombieDistance)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough)
            {
                selectedSpawns.Add(candidate);

                // Calcular posición real en el mundo
                Vector3 position = gridXZ.GetWorldPosition(candidate.x, candidate.z);
                float cellSize = gridXZ.GetCellSize();
                Vector3 centerPos = new Vector3(position.x + cellSize / 2, yValue, position.z + cellSize / 2);

                // --- Calcular la rotación hacia la salida del Dead End ---
                Quaternion rotation = Quaternion.identity;
                CellData cell = candidate.cellData;

                if (!cell.wallBottom) // Abierto ABAJO (Sur) -> Zombi mira al Sur
                    rotation = Quaternion.Euler(0, 180, 0);
                else if (!cell.wallTop) // Abierto ARRIBA (Norte) -> Zombi mira al Norte
                    rotation = Quaternion.Euler(0, 0, 0);
                else if (!cell.wallLeft) // Abierto IZQUIERDA (Oeste) -> Zombi mira al Oeste
                    rotation = Quaternion.Euler(0, -90, 0);
                else if (!cell.wallRight) // Abierto DERECHA (Este) -> Zombi mira al Este
                    rotation = Quaternion.Euler(0, 90, 0);

                // 4. Instanciar el punto de spawn con la ROTACIÓN calculada
                Instantiate(puntoSpawnPrefab, centerPos, rotation, zonaObj.transform);
            }
        }

        // 5. Autocompletar la lista del script ZonaZombies
        if (zonaScript != null)
        {
            zonaScript.AutocompletarSpawns();
        }

        if (puertaDesbloqueable != null)
        {
            // Le asignamos la zona a la puerta automáticamente por código
            puertaDesbloqueable.zonasADesbloquear = new ZonaZombies[] { zonaScript };
        }
    }

    private void SpawnHangar(int x, int z, CellData cell)
    {
        if (hangar == null) return;

        Vector3 position = gridXZ.GetWorldPosition(x, z);
        float cellSize = gridXZ.GetCellSize();
        Vector3 centerPos = new Vector3(position.x + cellSize/2, yValue, position.z + cellSize/2);

        Quaternion rotation = Quaternion.identity;
        
        if (!cell.wallBottom) // Abierto ABAJO (Sur) -> Hangar debe mirar al Sur
            rotation = Quaternion.Euler(0, 180, 0); 
        else if (!cell.wallTop) // Abierto ARRIBA (Norte) -> Hangar debe mirar al Norte
            rotation = Quaternion.Euler(0, 0, 0);
        else if (!cell.wallLeft) // Abierto IZQUIERDA (Oeste) -> Hangar debe mirar al Oeste
            rotation = Quaternion.Euler(0, -90, 0);
        else if (!cell.wallRight) // Abierto DERECHA (Este) -> Hangar debe mirar al Este
            rotation = Quaternion.Euler(0, 90, 0);

        GameObject newHangar = GameObject.Instantiate(hangar, centerPos, rotation);

        generatedHangars.Add(newHangar.transform);
    }

    private void DecorateMaze()
    {
        float cellSize = gridXZ.GetCellSize();
        // Distancia desde el centro hacia la pared (ajusta según el tamaño de tu celda y props)
        // Si tu celda mide 10, offset 4 deja el prop pegado al borde.
        float offset = (cellSize / 2) * 0.8f; 

        for (int x = 0; x < gridSpawner.width; x++)
        {
            for (int z = 0; z < gridSpawner.height; z++)
            {
                // 1. Evitar poner cosas donde ya hay Hangares
                // Comprobamos si esta celda coincide con algún hangar generado
                bool isHangarSpot = false;
                foreach(Transform h in generatedHangars)
                {
                    // Comprobación simple de distancia
                    if (Vector3.Distance(gridXZ.GetWorldPosition(x, z), h.position) < cellSize)
                    {
                        isHangarSpot = true;
                        break;
                    }
                }
                if (isHangarSpot) continue;

                // 2. Probabilidad Aleatoria
                if (Random.Range(0, 100) > decorationChance) continue;

                CellData cell = logicalMap[x, z];
                Vector3 centerPos = gridXZ.GetWorldPosition(x, z) + new Vector3(cellSize/2, yValue, cellSize/2);

                // 3. Lógica de Colocación (Prioridad a Farolas en esquinas)
                
                // CASO A: Esquina (Muro Arriba y Derecha) -> Farola
                if (cell.wallTop && cell.wallRight && streetLampPrefab != null)
                {
                    Vector3 pos = centerPos + new Vector3(offset, 4, offset);
                    // Rotamos la farola para que mire hacia el centro (aprox -135 grados o ajusta a tu modelo)
                    Instantiate(streetLampPrefab, pos, Quaternion.Euler(0, 225, 0), propsContainer);
                    continue; // Si ponemos farola, no ponemos barril
                }
                
                // CASO B: Esquina (Muro Abajo y Izquierda) -> Farola
                if (cell.wallBottom && cell.wallLeft && streetLampPrefab != null)
                {
                    Vector3 pos = centerPos + new Vector3(-offset, 4, -offset);
                    Instantiate(streetLampPrefab, pos, Quaternion.Euler(0, 45, 0), propsContainer);
                    continue;
                }

                // CASO C: Pared Suelta -> Barril/Caja
                if (barrelPrefabs != null && barrelPrefabs.Length > 0)
                {
                    GameObject propToSpawn = barrelPrefabs[Random.Range(0, barrelPrefabs.Length)];
                    
                    if (cell.wallTop) // Pegado arriba
                    {
                        Vector3 pos = centerPos + new Vector3(Random.Range(-2f, 2f), 0, offset); // Un poco de variación lateral
                        GameObject.Instantiate(propToSpawn, pos, Quaternion.identity, propsContainer);
                    }
                    else if (cell.wallBottom) // Pegado abajo
                    {
                        Vector3 pos = centerPos + new Vector3(Random.Range(-2f, 2f), 0, -offset);
                        GameObject.Instantiate(propToSpawn, pos, Quaternion.identity, propsContainer);
                    }
                    else if (cell.wallLeft) // Pegado izquierda
                    {
                        Vector3 pos = centerPos + new Vector3(-offset, 0, Random.Range(-2f, 2f));
                        GameObject.Instantiate(propToSpawn, pos, Quaternion.identity, propsContainer);
                    }
                    else if (cell.wallRight) // Pegado derecha
                    {
                        Vector3 pos = centerPos + new Vector3(offset, 0, Random.Range(-2f, 2f));
                        GameObject.Instantiate(propToSpawn, pos, Quaternion.identity, propsContainer);
                    }
                }
            }
        }
    }

    public CellData[,] GetLogicalMap()
    {
        return logicalMap;
    }
        
    #endregion
}
