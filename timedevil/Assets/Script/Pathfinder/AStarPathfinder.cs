using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Manhattan 거리를 휴리스틱 값으로 하여 경로를 탐색하는 A* 알고리즘
*/
public class AStarPathfinder
{
    private readonly Vector2Int[] DIRECTION_VECTORS =
    {
        new(0,1), new(-1, 0), new(0,-1), new(1,0),
        new(1,1), new(-1,1), new(-1,-1), new(1,-1)
    };

    private Collider2D collider;
    private float nodeSize;

    public AStarPathfinder(Collider2D collider, float nodeSize) {
        this.collider = collider;
        if(nodeSize<=0) {
            nodeSize = Physics2D.defaultContactOffset;
        }

        this.nodeSize = nodeSize;
    }

    public IEnumerator FindPath(Vector2 startPos, Vector2 targetPos, Stack<Vector2Int> res) {
        Vector2Int targetOffset = PositionToOffset(startPos, targetPos);
        Debug.Log(targetOffset);
        Debug.Log(nodeSize);

        Dictionary<Vector2Int, PathTile> openTiles = new();
        Dictionary<Vector2Int, PathTile> closeTiles = new();

        openTiles[Vector2Int.zero] = new PathTile(0, 0, ManhattanDistance(Vector2Int.zero, targetOffset), Vector2Int.zero, null);

        while(openTiles.Count!=0) {
            PathTile currentTile = new(int.MaxValue, 0, 0, Vector2Int.zero, null);
            Vector2Int currentOffset = new(0,0);
            foreach(KeyValuePair<Vector2Int, PathTile> kv in openTiles) {
                if(kv.Value.f<currentTile.f) {
                    currentTile = kv.Value;
                    currentOffset = kv.Key;
                }
            }

            openTiles.Remove(currentOffset);
            closeTiles[currentOffset] = currentTile;
            Debug.Log(currentOffset);

            for(int i=0; i<DIRECTION_VECTORS.Length; i++) {
                Vector2Int searchOffset = currentOffset + DIRECTION_VECTORS[i];
                if(closeTiles.ContainsKey(searchOffset)) {
                    continue;
                }

                if(!IsValidTile(startPos, searchOffset)) {
                    closeTiles[searchOffset] = new PathTile(-1,-1,-1, Vector2Int.zero, null);
                    continue;
                }

                if(searchOffset==targetOffset) {
                    closeTiles[searchOffset] = new(0, 0, 0, Vector2Int.zero, currentTile);
                    break;
                }

                int searchH = ManhattanDistance(targetOffset, searchOffset);
                int searchG = currentTile.g + 10 + (int)Mathf.Abs(DIRECTION_VECTORS[i].x * DIRECTION_VECTORS[i].y)*4;
                int searchF = searchG + searchH;

                if(openTiles.ContainsKey(searchOffset) && openTiles[searchOffset].f>searchF) {
                    openTiles[searchOffset].g = searchG;
                    openTiles[searchOffset].f = searchF;
                    openTiles[searchOffset].parent = currentTile;
                } else {
                    openTiles[searchOffset] = new PathTile(searchF, searchG, searchH, searchOffset, currentTile);
                }
            }

            yield return null;
        }

        if(closeTiles.ContainsKey(targetOffset)) {
            res.Push(targetOffset);
            PathTile tile = closeTiles[targetOffset];
            while(tile.parent!=null) {
                res.Push(tile.parent.offset);
                tile = tile.parent;
            }
        }

        Debug.Log("dkfjdkf");
    }

    private Vector2Int PositionToOffset(Vector2 origin, Vector2 target) {
        Vector2Int targetOffset = new(
        (int)Mathf.Round((target.x - origin.x) / nodeSize),
        (int)Mathf.Round((target.y - origin.y) / nodeSize)
        );

        return targetOffset;
    }

    private int ManhattanDistance(Vector2Int a, Vector2Int b) {
        return (int)Mathf.Abs(a.x - b.x) + (int)Mathf.Abs(a.y - b.y);
    }

    private bool IsValidTile(Vector2 startPos, Vector2Int tileOffset) {
        Collider2D[] results = Physics2D.OverlapBoxAll(startPos + (Vector2)tileOffset * nodeSize, collider.bounds.size, 0);
        if(results.Length == 0)
            return true;
        else if(results.Length >= 2)
            return false;
        else if(results.Length == 1 && results[0] == collider)
            return true;
        else
            return false;
    }
}
