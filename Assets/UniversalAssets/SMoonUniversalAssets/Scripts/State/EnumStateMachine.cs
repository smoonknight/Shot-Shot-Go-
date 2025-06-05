using System;
using UnityEngine;

public abstract class EnumStateMachine<T, G> : StateMachine<T> where T : Component where G : Enum
{
    G latestType;
    T component;

    bool hasFirstSetState;
    public void Initialize(T component, G initialStateType)
    {
        this.component = component;
        InitializeState(component);
        SetState(initialStateType);
    }

    public void SetState(G type)
    {
        hasFirstSetState = true;
        latestType = type;
        SetState(GetState(type));
    }
    public void SetStateWhenDifference(G type)
    {
        if (!hasFirstSetState || latestType.Equals(type))
        {
            return;
        }
        SetState(type);
    }

    protected abstract void InitializeState(T component);
    protected abstract BaseState<T> GetState(G type);
}