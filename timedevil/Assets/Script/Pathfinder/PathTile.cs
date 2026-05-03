using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathTile
{
    public int f;
    public int g;
    public int h;
    public Vector2Int offset;
    public PathTile parent;

    public PathTile(int f, int g, int h, Vector2Int offset, PathTile parent) {
        this.f = f;
        this.g = g;
        this.h = h;
        this.offset = offset;
        this.parent = parent;
    }
}
