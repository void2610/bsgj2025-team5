using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// パーティクルを一括管理するシングルトンマネージャー
/// プールにオブジェクトを格納することで存在するパーティクルの数を制限できる
/// プールのサイズを超えて生成できないので、パフォーマンスを圧迫することがない
/// ParticleDataを入力することでパーティクルの生成を行う
/// </summary>
public class ParticleManager : SingletonMonoBehaviour<ParticleManager>
{ 
    [SerializeField] private int maxParticleCount = 100;

    private readonly List<GameObject> _particlePool = new();
    
    public bool IsFull => _particlePool.Count >= maxParticleCount;

    [CanBeNull]
    public GameObject CreateParticle(ParticleData particleData, Vector3 position, Quaternion rotation = default, bool important = false)
    {
        if (!important && IsFull)
        {
            Debug.LogWarning("パーティクルプールがいっぱいです。");
            return null;
        }

        if (important && IsFull)
        {
            Destroy(_particlePool[0]);
            _particlePool.RemoveAt(0);
        }
        
        var p = Instantiate(particleData.particlePrefab, position, rotation, this.transform);
        
        p.transform.position = position;
        p.transform.rotation = rotation;
        p.SetActive(true);
        _particlePool.Add(p);
        p.AddComponent<EventMethodAttacher>().OnDestroyAction += () => _particlePool.Remove(p);
        
        return p;
    }
}