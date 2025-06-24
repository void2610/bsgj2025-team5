using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Collections.Generic;
using TMPro;
using Coffee.UIEffects;
using LitMotion;
using LitMotion.Extensions;

public class ItemUI : MonoBehaviour
{
    [SerializeField] private List<GameObject> itemIcons = new ();
    [SerializeField] private TextMeshProUGUI itemCountText;
    
    [Header("アニメーション設定")]
    [SerializeField] private float animationDuration = 0.5f;
    
    private int _previousItemCount = 0;
    
    private void Start()
    {
        UpdateItemDisplay(0);
        // GameManagerのアイテム数を購読
        GameManager.Instance.ItemCount.Subscribe(UpdateItemDisplay).AddTo(this);
    }
    
    private void UpdateItemDisplay(int itemCount)
    {
        for (var i = 0; i < itemIcons.Count; i++)
        {
            if (i < itemCount)
            {
                // 新しくアクティブになるアイテムアイコンにアニメーションを適用
                if (i >= _previousItemCount) AnimateItemAppear(itemIcons[i]);
            }
        }
        
        _previousItemCount = itemCount;
        itemCountText.text = $"{itemCount} / {itemIcons.Count}";
    }
    
    /// <summary>
    /// アイテム取得時のエフェクトアニメーション
    /// </summary>
    /// <param name="icon">アニメーションするアイコン</param>
    private void AnimateItemAppear(GameObject icon)
    {
        var uiEffect = icon.GetComponent<UIEffect>();
        if (uiEffect)
        {
            // トランジション値を1から0にアニメーション（Blazeエフェクトを徐々に消す）
            LMotion.Create(1f, 0f, animationDuration)
                .WithOnComplete(() => {
                    // アニメーション完了後、エフェクトを無効化
                    uiEffect.transitionRate = 0f;
                })
                .Bind(value => uiEffect.transitionRate = value);
        }
    }
}
