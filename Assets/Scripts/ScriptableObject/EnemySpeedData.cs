/// <summary>
/// 敵のアイテム数に応じたスピードの設定のデータを持つクラス
/// </summary>
/// 
/// 開発進捗
/// 05/19:作成

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "EnemySpeedData", menuName = "AI/EnemySpeedData")]
public class EnemySpeedData : ScriptableObject
{
    public List<SpeedSettings> speedSettings;

    [System.Serializable]
    public class SpeedSettings
    {
        public int itemCount;
        public float speed;
    }


    public float GetSpeed(int itemCount)
    {
        return speedSettings.FirstOrDefault(s => s.itemCount == itemCount)?.speed ?? 2f;
    }

}
