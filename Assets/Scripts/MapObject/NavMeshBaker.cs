/// <summary>
/// Coliderに対してステージを敵に認識してもらう
/// </summary>
///
/// 進捗
/// 05/20:作成
using Unity.AI.Navigation;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NavMeshBaker : MonoBehaviour
{
    private NavMeshSurface surface;     //NavMeshSurfaceを使ってエリアをBakeする
    void Start()
    {

        surface = gameObject.GetComponent<NavMeshSurface>();
        // NavMeshSurfaceがなければ追加する
        if (surface == null)
        {
            surface = gameObject.AddComponent<NavMeshSurface>();
        }

        surface.BuildNavMesh(); //実行時にベイクする
    }

}
