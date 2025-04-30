using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using R3;

[CreateAssetMenu(fileName = "PlayerState", menuName = "ScriptableObjects/PlayerState")]
public sealed class PlayerState : ScriptableObject
{
    public ReadOnlyReactiveProperty<Vector3> Position => _position;
    
    private readonly ReactiveProperty<Vector3> _position = new(Vector3.zero);
    private const string WRITE_ALLOWED_SCENE = "TestPlayer";
    
    public void SetPosition(in Vector3 v)
    {
        var current = SceneManager.GetActiveScene().name;
        if (current != WRITE_ALLOWED_SCENE)
            throw new InvalidOperationException($"PlayerState.SetPosition は '{WRITE_ALLOWED_SCENE}' シーン専用です");
        
        _position.Value = v;
    }
}