using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotController : MonoBehaviour
{
    [Header("Referencias")]
    public EllersMazeGenerator mazeGenerator; 
    public GridSpawner gridSpawner;          
    public float speed = 5f;            

    private LineRenderer lineRenderer;
    private List<Vector3> currentPath;
    private int currentWaypointIndex = 0;
    private bool isMoving = false;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0; // Limpiar línea al inicio
        StartCoroutine(AutoStartLag());
    }

    void Update()
    {
        // 1. Detectar Click del Jugador
        if (Input.GetMouseButtonDown(0))
        {
            HandleInput();
        }

        // 2. Mover el Bot si hay ruta
        if (isMoving && currentPath != null)
        {
            MoveAlongPath();
        }
    }

    void HandleInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Verificamos si clicamos un Hangar
            if (hit.collider.CompareTag("Hangar"))
            {
                Debug.Log("Hangar seleccionado. Calculando ruta...");
                CalculateRoute(hit.transform.position);
            }
        }
    }

    void CalculateRoute(Vector3 targetPos)
    {
        // Obtener datos necesarios
        CellData[,] map = mazeGenerator.GetLogicalMap(); 
        Custom_Grid grid = gridSpawner.grid;

        // EJECUTAR A* (Llama a nuestro script estático)
        currentPath = AStarPathfinding.FindPath(transform.position, targetPos, map, grid);

        if (currentPath != null && currentPath.Count > 0)
        {
            // Configurar Line Renderer
            DrawPath(currentPath);
            
            // Configurar movimiento
            currentWaypointIndex = 0;
            isMoving = true;
        }
        else
        {
            Debug.LogWarning("No se encontró camino al objetivo.");
        }
    }

    void CalculateRouteAuto(Vector3 targetPos)
    {
        CellData[,] map = mazeGenerator.GetLogicalMap(); 
        Custom_Grid grid = gridSpawner.grid;

        currentPath = AStarPathfinding.FindPath(transform.position, targetPos, map, grid);

        if (currentPath != null)
        {
            // Si estamos en modo automático, avisamos al colector
            GameAnalyticsCollector collector = FindObjectOfType<GameAnalyticsCollector>();
            
            if (collector != null && collector.dataCollectionMode)
            {
                // Calculamos tiempo estimado (Distancia / Velocidad)
                // Ojo: Si quieres precisión total, usa la longitud real del path
                float distance = 0; 
                for(int i=0; i<currentPath.Count-1; i++) distance += Vector3.Distance(currentPath[i], currentPath[i+1]);
                float estimatedTime = distance / speed;

                collector.RecordGameData(currentPath, estimatedTime);
                return; // Si es auto, no movemos el bot visualmente para ir rápido
            }

            DrawPath(currentPath);
            isMoving = true;
        }
    }

    void MoveAlongPath()
    {
        if (currentWaypointIndex >= currentPath.Count)
        {
            isMoving = false;
            Debug.Log("¡Destino Alcanzado!");
            return;
        }

        Vector3 targetWaypoint = currentPath[currentWaypointIndex];
        
        // Mantener la altura Y del bot para que no se hunda o vuele
        targetWaypoint.y = transform.position.y;

        // Movimiento simple sin suavizado (MoveTowards)
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint, speed * Time.deltaTime);

        // Chequear si llegamos al waypoint (con un pequeño margen de error)
        if (Vector3.Distance(transform.position, targetWaypoint) < 0.05f)
        {
            currentWaypointIndex++;
        }
    }

    IEnumerator AutoStartLag()
    {
        // Esperamos un frame para asegurar que el laberinto se generó
        yield return new WaitForEndOfFrame(); 
        
        GameAnalyticsCollector collector = FindObjectOfType<GameAnalyticsCollector>();
        if (collector != null && collector.dataCollectionMode && mazeGenerator.targetHangar != null)
        {
            // Lanzamos el bot automáticamente al objetivo
            CalculateRouteAuto(mazeGenerator.targetHangar.transform.position);
        }
    }

    void DrawPath(List<Vector3> path)
    {
        if (lineRenderer == null) return;

        // Añadimos la posición actual como inicio de la línea
        lineRenderer.positionCount = path.Count + 1;
        lineRenderer.SetPosition(0, transform.position);

        for (int i = 0; i < path.Count; i++)
        {
            // Ajustamos un poco la altura de la línea para que se vea sobre el suelo
            Vector3 point = path[i];
            point.y = transform.position.y; 
            lineRenderer.SetPosition(i + 1, point);
        }
    }
}