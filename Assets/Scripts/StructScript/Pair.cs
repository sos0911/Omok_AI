using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// struct 역할을 하는 pair<> 클래스이다.
/// </summary>
public class Pair
{
    public int y, x;

    public Pair(int _y, int _x)
    {
        y = _y;
        x = _x;
    }

    public static bool operator ==(Pair a, Pair b)
    {
        return a.y == b.y && a.x == b.x;
    }

    public static bool operator !=(Pair a, Pair b)
    {
        return a.y != b.y || a.x != b.x;
    }
}
