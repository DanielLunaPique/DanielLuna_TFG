using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinding
{
    // Clase auxiliar para los nodos del algoritmo
    public class Node
    {
        public int x;
        public int z;
        public int gCost;
        public int hCost;
        public Node parent;
        public bool wallTop, wallBottom, wallLeft, wallRight;

        public int FCost { get { return gCost + hCost; } }

        public Node(int x, int z, CellData cellData)
        {
            this.x = x;
            this.z = z;
            // Copiamos datos de paredes para saber por dónde NO podemos pasar
            this.wallTop = cellData.wallTop;
            this.wallBottom = cellData.wallBottom;
            this.wallLeft = cellData.wallLeft;
            this.wallRight = cellData.wallRight;
        }
    }

    // Método principal: Calcula el camino
    public static List<Vector3> FindPath(Vector3 startWorldPos, Vector3 targetWorldPos, CellData[,] map, Custom_Grid grid)
    {
        if (grid == null || map == null)
        {
            Debug.LogWarning("A*: Abortado. El Grid o el Mapa son nulos.");
            return null;
        }

        int[] start = grid.GetXZ(startWorldPos);
        int[] target = grid.GetXZ(targetWorldPos);

        if (start == null || target == null || start.Length < 2 || target.Length < 2)
        {
            Debug.LogWarning($"A*: Abortado. Coordenadas GetXZ fallaron. Start: {(start != null ? "OK" : "NULL")}, Target: {(target != null ? "OK" : "NULL")}");
            return null;
        }

        int width = map.GetLength(0);
        int height = map.GetLength(1);

        // Chivato 1: ¿Estamos fuera del mapa?
        if (start[0] < 0 || start[0] >= width || start[1] < 0 || start[1] >= height)
        {
            Debug.LogWarning($"A*: Abortado. El INICIO ({start[0]},{start[1]}) está fuera de los límites del mapa ({width}x{height})");
            return null;
        }
        if (target[0] < 0 || target[0] >= width || target[1] < 0 || target[1] >= height)
        {
            Debug.LogWarning($"A*: Abortado. El DESTINO ({target[0]},{target[1]}) está fuera de los límites del mapa ({width}x{height})");
            return null;
        }

        // Chivato 2: ¿Nacemos en un agujero vacío?
        if (map[start[0], start[1]] == null)
        {
            Debug.LogWarning($"A*: Abortado. La celda de INICIO es nula (no hay suelo lógico ahí).");
            return null;
        }

        Node startNode = new Node(start[0], start[1], map[start[0], start[1]]);
        Node targetNode = new Node(target[0], target[1], map[target[0], target[1]]);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        Node[,] allNodes = new Node[width, height];
        for (int i = 0; i < width; i++)
            for (int j = 0; j < height; j++)
                if (map[i, j] != null)
                    allNodes[i, j] = new Node(i, j, map[i, j]);

        openSet.Add(allNodes[start[0], start[1]]);

        int frenoDeEmergencia = 0;

        while (openSet.Count > 0)
        {
            frenoDeEmergencia++;
            if (frenoDeEmergencia > 3000)
            {
                Debug.LogError("A*: BUCLE INFINITO. Destino inalcanzable, forzando parada de emergencia.");
                return null;
            }

            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost || (openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode.x == target[0] && currentNode.z == target[1])
            {
                return RetracePath(allNodes[start[0], start[1]], currentNode, grid);
            }

            foreach (Node neighbor in GetNeighbors(currentNode, allNodes, width, height))
            {
                if (closedSet.Contains(neighbor)) continue;

                int newMovementCostToNeighbor = currentNode.gCost + 10;
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // Chivato 3: Se exploró todo pero no hay salida
        Debug.LogWarning("A*: Se exploraron todos los caminos posibles y NO HAY RUTA al destino. (Laberinto bloqueado o sin conexión).");
        return null;
    }

    private static List<Node> GetNeighbors(Node node, Node[,] allNodes, int width, int height)
    {
        List<Node> neighbors = new List<Node>();

        // Arriba (Z + 1) - Solo si NO hay pared TOP en mi celda actual
        if (!node.wallTop && IsInBounds(node.x, node.z + 1, width, height))
            neighbors.Add(allNodes[node.x, node.z + 1]);

        // Abajo (Z - 1) - Solo si NO hay pared BOTTOM en mi celda actual
        if (!node.wallBottom && IsInBounds(node.x, node.z - 1, width, height))
            neighbors.Add(allNodes[node.x, node.z - 1]);

        // Izquierda (X - 1) - Solo si NO hay pared LEFT en mi celda actual
        if (!node.wallLeft && IsInBounds(node.x - 1, node.z, width, height))
            neighbors.Add(allNodes[node.x - 1, node.z]);

        // Derecha (X + 1) - Solo si NO hay pared RIGHT en mi celda actual
        if (!node.wallRight && IsInBounds(node.x + 1, node.z, width, height))
            neighbors.Add(allNodes[node.x + 1, node.z]);

        return neighbors;
    }

    private static List<Vector3> RetracePath(Node startNode, Node endNode, Custom_Grid grid)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            // Convertir nodo (x,z) a posición de mundo CENTRADA
            Vector3 worldPos = grid.GetWorldPosition(currentNode.x, currentNode.z);
            float halfCell = grid.GetCellSize() * 0.5f;
            // Asumimos Y=0 o mantenemos la Y del mundo, ajusta la Y según necesites
            path.Add(new Vector3(worldPos.x + halfCell, 0, worldPos.z + halfCell));
            
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    private static int GetDistance(Node nodeA, Node nodeB)
    {
        // Distancia Manhattan
        int dstX = Mathf.Abs(nodeA.x - nodeB.x);
        int dstY = Mathf.Abs(nodeA.z - nodeB.z);
        return dstX + dstY;
    }

    private static bool IsInBounds(int x, int z, int width, int height)
    {
        return x >= 0 && x < width && z >= 0 && z < height;
    }
}