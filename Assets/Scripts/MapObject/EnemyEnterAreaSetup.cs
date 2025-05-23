/// <summary>
/// Coliderに対してステージを敵に認識してもらう
/// </summary>
///
/// 進捗
/// 05/20:作成
/// 05.22:Agentを取得する前にBakeする必要があるため、Awakeで処理するようにした



using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

[RequireComponent(typeof(NavMeshSurface))]
public class EnemyEnterAreaSetup : MonoBehaviour
{
    private NavMeshSurface _surface;

    [SerializeField] private LayerMask bakeLayerMask;
    [SerializeField] private NavMeshCollectGeometry geometryType = NavMeshCollectGeometry.RenderMeshes;

    private void Awake()
    {
        bakeLayerMask = 72;
        
        _surface = GetComponent<NavMeshSurface>();

        // 設定をスクリプトで変更
        _surface.layerMask = bakeLayerMask;
        _surface.collectObjects = CollectObjects.Children;
        _surface.useGeometry = geometryType;
        _surface.overrideTileSize = true;
        _surface.tileSize = 64;
        _surface.overrideVoxelSize = true;
        _surface.voxelSize = 0.1f;
        _surface.BuildNavMesh();


    }
}