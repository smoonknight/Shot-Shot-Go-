using System;
using UnityEngine;

public abstract class EnumStateMachine<T, G> : StateMachine<T> where T : Component where G : Enum
{
    T component;
    public void Initialize(T component, G initialStateType)
    {
        this.component = component;
        InitializeState(component);
        SetState(initialStateType);
    }

    public void SetState(G type) => SetState(GetState(type));

    protected abstract void InitializeState(T component);
    protected abstract BaseState<T> GetState(G type);
}