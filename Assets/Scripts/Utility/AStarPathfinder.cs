using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
    private class Node
    {
        public Vector2Int position;
        public Node parent;
        public int gCost;
        public int hCost;
        public int fCost => gCost + hCost;

        public Node(Vector2Int pos)
        {
            position = pos;
        }
    }

    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        List<Node> openList = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Node startNode = new Node(start);
        Node targetNode = new Node(target);

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node current = openList[0];

            for (int i = 1; i < openList.Count; i++)
            {
                if (openList[i].fCost < current.fCost ||
                   (openList[i].fCost == current.fCost &&
                    openList[i].hCost < current.hCost))
                {
                    current = openList[i];
                }
            }

            openList.Remove(current);
            closedSet.Add(current.position);

            if (current.position == target)
                return RetracePath(startNode, current);

            foreach (Vector2Int neighbour in GetNeighbours(current.position))
            {
                if (!GridManager.Instance.IsInsideGrid(neighbour))
                    continue;

                if (GridManager.Instance.IsTileOccupied(neighbour) &&
                    neighbour != target)
                    continue;

                if (closedSet.Contains(neighbour))
                    continue;

                int newCost = current.gCost + 1;

                Node neighbourNode = openList.Find(n => n.position == neighbour);

                if (neighbourNode == null)
                {
                    neighbourNode = new Node(neighbour);
                    neighbourNode.gCost = newCost;
                    neighbourNode.hCost = GetDistance(neighbour, target);
                    neighbourNode.parent = current;
                    openList.Add(neighbourNode);
                }
                else if (newCost < neighbourNode.gCost)
                {
                    neighbourNode.gCost = newCost;
                    neighbourNode.parent = current;
                }
            }
        }

        return null;
    }

    private static List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node current = endNode;

        while (current != startNode)
        {
            path.Add(current.position);
            current = current.parent;
        }

        path.Reverse();
        return path;
    }

    private static List<Vector2Int> GetNeighbours(Vector2Int pos)
    {
        return new List<Vector2Int>
        {
            pos + Vector2Int.up,
            pos + Vector2Int.down,
            pos + Vector2Int.left,
            pos + Vector2Int.right
        };
    }

    private static int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}