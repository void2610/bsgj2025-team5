using System;
using UnityEngine;

/// <summary>
/// MonoBehaviourのOnEnable, OnDisable, OnDestroyをイベントとして公開するクラス
/// MonoBehaviourがアタッチされていないオブジェクトに、コードから動的にイベントを登録することができる
/// </summary>
public class EventMethodAttacher: MonoBehaviour
{
    public event Action OnEnableAction;
    public event Action OnDisableAction;
    public event Action OnDestroyAction;

    private void OnEnable() => OnEnableAction?.Invoke();
    private void OnDisable() => OnDisableAction?.Invoke();
    private void OnDestroy() => OnDestroyAction?.Invoke();
}
