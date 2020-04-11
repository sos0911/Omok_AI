using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public KeyValuePair<int, int> coord; // node로 따라가면 AI가 놓게 되는 점의 좌표
    public List<Node> childlist;
    public int bestvalue; // 지금까지 찾은 min or max value(자기를 root로 하는 subtree에서)

    public Node(ControlAISettingRocks.MaxMIn kind)
    {
        // maxnode면 best=-inf로 선언.
        bestvalue = (kind == ControlAISettingRocks.MaxMIn.max ? -ControlAISettingRocks.INF : ControlAISettingRocks.INF);
        childlist = new List<Node>();
    }
}
