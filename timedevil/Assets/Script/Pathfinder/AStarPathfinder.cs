using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Manhattan 거리를 휴리스틱 값으로 하여 경로를 탐색하는 A* 알고리즘
*/
public class AStarPathfinder
{
    public static readonly Vector2Int ERROR_SIGNAL = new(int.MaxValue, int.MaxValue);

    private readonly Vector2Int[] STRAIGHT_VECTORS =
    {
        new(0,1), new(-1, 0), new(0,-1), new(1,0),
    };

    private readonly Vector2Int[] DIAGONAL_VECTORS =
    {
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

    public IEnumerator FindPath(Vector2 startPos, Vector2 targetPos, List<Vector2Int> res) {
        Vector2Int targetOffset = PositionToOffset(startPos, targetPos);

        Dictionary<Vector2Int, PathTile> openTiles = new();
        Dictionary<Vector2Int, PathTile> closeTiles = new();

        int h = ManhattanDistance(Vector2Int.zero, targetOffset);
        openTiles[Vector2Int.zero] = new PathTile(h, 0, h, Vector2Int.zero, null);

        PathTile AbnormalTile = new(int.MaxValue, 0, 0, Vector2Int.zero, null);

        bool[] diagonalCheckAllowed = new bool[4];

        while(openTiles.Count!=0) {
            PathTile currentTile = AbnormalTile;
            Vector2Int currentOffset = Vector2Int.zero;
            foreach(KeyValuePair<Vector2Int, PathTile> kv in openTiles) {
                if(kv.Value.f<currentTile.f) {
                    currentTile = kv.Value;
                    currentOffset = kv.Key;
                }
            }

            openTiles.Remove(currentOffset);
            closeTiles[currentOffset] = currentTile;

            bool[] isObstacle = new bool[4];
            for(int i=0; i < STRAIGHT_VECTORS.Length; i++) {
                Vector2Int searchOffset = currentOffset + STRAIGHT_VECTORS[i];

                if(!IsValidTile(startPos, searchOffset)) {
                    isObstacle[i] = true;
                    closeTiles[searchOffset] = null;
                    continue;
                }

                if(closeTiles.ContainsKey(searchOffset)) {
                    continue;
                }

                if(searchOffset==targetOffset) {
                    closeTiles[searchOffset] = new(0, 0, 0, Vector2Int.zero, currentTile);
                    goto sortpath;
                }

                int searchH = ManhattanDistance(targetOffset, searchOffset);
                int searchG = currentTile.g + 10;
                int searchF = searchG + searchH;

                if(openTiles.ContainsKey(searchOffset)) {
                    if(openTiles[searchOffset].g > searchG) {
                        openTiles[searchOffset].g = searchG;
                        openTiles[searchOffset].f = searchF;
                        openTiles[searchOffset].parent = currentTile;
                    }
                } else {
                    openTiles[searchOffset] = new PathTile(searchF, searchG, searchH, searchOffset, currentTile);
                }
            }

            diagonalCheckAllowed[0] = !(isObstacle[3] || isObstacle[0]);
            diagonalCheckAllowed[1] = !(isObstacle[0] || isObstacle[1]);
            diagonalCheckAllowed[2] = !(isObstacle[1] || isObstacle[2]);
            diagonalCheckAllowed[3] = !(isObstacle[2] || isObstacle[3]);

            for(int i = 0; i < DIAGONAL_VECTORS.Length; i++) {
                if(!diagonalCheckAllowed[i]) {
                    continue;
                }
                Vector2Int searchOffset = currentOffset + DIAGONAL_VECTORS[i];

                if(closeTiles.ContainsKey(searchOffset)) {
                    continue;
                }

                if(!IsValidTile(startPos, searchOffset)) {
                    closeTiles[searchOffset] = null;
                    continue;
                }

                if(searchOffset == targetOffset) {
                    closeTiles[searchOffset] = new(0, 0, 0, Vector2Int.zero, currentTile);
                    goto sortpath;
                }

                int searchH = ManhattanDistance(targetOffset, searchOffset);
                int searchG = currentTile.g + 14;
                int searchF = searchG + searchH;

                if(openTiles.ContainsKey(searchOffset)) {
                    if(openTiles[searchOffset].g > searchG) {
                        openTiles[searchOffset].g = searchG;
                        openTiles[searchOffset].f = searchF;
                        openTiles[searchOffset].parent = currentTile;
                    }
                } else {
                    openTiles[searchOffset] = new PathTile(searchF, searchG, searchH, searchOffset, currentTile);
                }
            }

            yield return null;
        }

    sortpath:

        if(closeTiles.ContainsKey(targetOffset)) {
            res.Add(targetOffset);
            PathTile tile = closeTiles[targetOffset];
            //Vector2Int directionTendency = tile.parent.offset - tile.offset, nextDirection;
            while(tile.parent != null) {
                //nextDirection = (tile.parent.parent!=null) ? tile.parent.parent.offset - tile.parent.offset : new(int.MaxValue,int.MaxValue);
                //if(directionTendency!=nextDirection) {
                    res.Insert(0, tile.parent.offset);
                    //directionTendency = nextDirection;
                //}

                tile = tile.parent;
            }
        } else {
            res.Add(ERROR_SIGNAL);
        }
    }

    private Vector2Int PositionToOffset(Vector2 origin, Vector2 target) {
        Vector2Int targetOffset = new(
        (int)Mathf.Round((target.x - origin.x) / nodeSize),
        (int)Mathf.Round((target.y - origin.y) / nodeSize)
        );

        return targetOffset;
    }

    private int Distance(Vector2Int a, Vector2Int b) {
        return OctileDistance(a, b);
    }

    private int ManhattanDistance(Vector2Int a, Vector2Int b) {
        return ((int)Mathf.Abs(a.x - b.x) + (int)Mathf.Abs(a.y - b.y))*10;
    }

    private int OctileDistance(Vector2Int a, Vector2Int b) {
        int dx = (int)Mathf.Abs(a.x - b.x);
        int dy = (int)Mathf.Abs(a.y - b.y);

        return Mathf.Max(dx, dy) * 10 + Mathf.Min(dx, dy) * 4;
    }

    private bool IsValidTile(Vector2 startPos, Vector2Int tileOffset) {
        Collider2D[] results = Physics2D.OverlapBoxAll(startPos + (Vector2)tileOffset * nodeSize, collider.bounds.size, 0);
        if(results.Length == 0)
            return true;
        else if(results.Length >= 2)
            return false;
        else if(results[0] == collider)
            return true;
        else
            return false;
    }
}
