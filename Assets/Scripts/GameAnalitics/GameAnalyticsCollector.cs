using System.Collections.Generic;
using UnityEngine;
using System.IO; 
using System.Text;

public class GameAnalyticsCollector : MonoBehaviour
{
    [Header("Configuración")]
    public bool dataCollectionMode = false; 
    public int totalSimulationsToRun = 100; 
    public string fileName = "maze_analytics.csv";

    [Header("Referencias")]
    public BotController bot;
    public EllersMazeGenerator generator;
    public GridSpawner gridSpawner;

    // --- CAMBIO 1: Variable estática para sobrevivir al LoadScene ---
    private static int currentSimulationCount = 0;

    void Start()
    {
        // Solo si estamos en modo recolección
        if (dataCollectionMode)
        {
            // Si es la PRIMERA simulación (0), preparamos el archivo
            if (currentSimulationCount == 0)
            {
                string filePath = Path.Combine(Application.dataPath, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Escribimos la cabecera
                string header = "SimulationID,WallCount,HangarCount,PathLength,Turns,TimeTaken,ComplexityLabel\n";
                File.WriteAllText(filePath, header);
                
                Debug.Log($"Archivo creado en: {filePath}");
            }
        }
    }

    public void RecordGameData(List<Vector3> path, float estimatedTime)
    {
        if (!dataCollectionMode) return;

        // 1. Recolectar Métricas
        int wallCount = CountWalls();
        int hangarCount = generator.generatedHangars.Count;
        float pathLength = CalculatePathLength(path);
        int turns = CalculateTurns(path);
        
        string label = "Medium";
        if (estimatedTime < 10f) label = "Easy";
        else if (estimatedTime > 25f) label = "Hard";

        // 2. Formatear línea FORZANDO EL PUNTO DECIMAL (InvariantCulture)
        string newLine = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3:F2},{4},{5:F2},{6}\n",
            currentSimulationCount,
            wallCount,
            hangarCount,
            pathLength,
            turns,
            estimatedTime,
            label
        );
        
        // 3. Escribir al archivo
        string filePath = Path.Combine(Application.dataPath, fileName);
        File.AppendAllText(filePath, newLine);

        currentSimulationCount++;
        Debug.Log($"Simulación {currentSimulationCount}/{totalSimulationsToRun} guardada.");

        if (currentSimulationCount < totalSimulationsToRun)
        {
            RestartSimulation();
        }
        else
        {
            Debug.Log("<b>RECOLECCIÓN DE DATOS FINALIZADA</b>");
            currentSimulationCount = 0; 
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }

    void RestartSimulation()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    int CountWalls()
    {
        CellData[,] map = generator.GetLogicalMap();
        int walls = 0;
        for(int x=0; x< map.GetLength(0); x++)
            for(int z=0; z< map.GetLength(1); z++)
            {
                if(map[x,z].wallTop) walls++;
                if(map[x,z].wallBottom) walls++;
            }
        return walls;
    }

    float CalculatePathLength(List<Vector3> path)
    {
        float dist = 0;
        for (int i = 0; i < path.Count - 1; i++)
            dist += Vector3.Distance(path[i], path[i+1]);
        return dist;
    }

    int CalculateTurns(List<Vector3> path)
    {
        int turns = 0;
        if (path.Count < 3) return 0;
        Vector3 prevDir = (path[1] - path[0]).normalized;
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 currDir = (path[i+1] - path[i]).normalized;
            if (Vector3.Dot(prevDir, currDir) < 0.99f) turns++;
            prevDir = currDir;
        }
        return turns;
    }
}