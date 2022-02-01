using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public Func<Unit, bool> DecisionFunction;
    public Node LeftNode;
    public Node RightNode;
    public ActionType LeftReturn;
    public ActionType RightReturn;

    public Node(Func<Unit, bool> func, Node left = null, Node right = null,
        ActionType leftA = ActionType.ATTACK, ActionType rightA = ActionType.ATTACK)
    {
        DecisionFunction = func;
        LeftNode = left;
        RightNode = right;
        LeftReturn = leftA;
        RightReturn = rightA;
    }
    public ActionType Choice(Unit data)
    {
        if (DecisionFunction(data))
            if (this.RightNode == null)
                return RightReturn;
            else
                return RightNode.Choice(data);
        else
            if (this.LeftNode == null)
                return LeftReturn;
            else
                return LeftNode.Choice(data);
    }
}
