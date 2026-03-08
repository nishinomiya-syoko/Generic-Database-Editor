using System.Collections.Generic;
using UnityEngine;
public class FlowFieldMap
{
    public int width;
    public int height;
    public Vector2Int destination;
    private FlowFieldNode[,] nodes;

    public FlowFieldMap(int width, int height, Vector2Int destination)
    {
        this.width = width;
        this.height = height;
        this.destination = destination;
        nodes = new FlowFieldNode[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                nodes[x, y] = new FlowFieldNode(x, y);
            }
        }
    }
    public FlowFieldNode GetNode(int x, int y)
    {
        return nodes[x, y];
    }
}
public class FlowFieldNode
{
    public readonly int x, y;
    public int integrationCost;
    public Vector2 bestDirection;
    public FlowFieldNode(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}