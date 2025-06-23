using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Collections.Generic;
using TMPro;
using LitMotion;
using LitMotion.Extensions;

public class ItemUI : MonoBehaviour
{
    [SerializeField] private List<Image> itemIcons = new ();
    [SerializeField] private TextMeshProUGUI itemCountText;
    
    [Header("アニメーション設定")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float initialRotation = 45f; // 初期回転角度
    [SerializeField] private float scaleOvershoot = 1.3f;
    
    private readonly Color _activeColor = new (1f, 1f, 1f, 1f); // アクティブ状態の色（白）
    private readonly Color _inactiveColor = new (1f, 1f, 1f, 0f); // 非アクティブ状態の色（半透明）
    private int _previousItemCount = 0;
    private Vector3 _originalIconScale; // アイコンの初期スケールをキャッシュ
    
    private void Start()
    {
        // 最初のアイコンの初期スケールをキャッシュ（全て同じサイズという前提）
        if (itemIcons.Count > 0 && itemIcons[0] != null)
        {
            _originalIconScale = itemIcons[0].transform.localScale;
        }
        else
        {
            _originalIconScale = Vector3.one;
        }
        
        UpdateItemDisplay(0);
        // GameManagerのアイテム数を購読
        GameManager.Instance.ItemCount.Subscribe(UpdateItemDisplay).AddTo(this);
    }
    
    private void UpdateItemDisplay(int itemCount)
    {
        for (var i = 0; i < itemIcons.Count; i++)
        {
            if (itemIcons[i])
            {
                if (i < itemCount)
                {
                    // 新しくアクティブになるアイテムアイコンにアニメーションを適用
                    if (i >= _previousItemCount)
                    {
                        AnimateItemAppear(itemIcons[i]);
                    }
                    else
                    {
                        itemIcons[i].color = _activeColor;
                    }
                }
                else
                {
                    itemIcons[i].color = _inactiveColor;
                }
            }
        }
        
        _previousItemCount = itemCount;
        itemCountText.text = $"{itemCount} / {itemIcons.Count}";
    }
    
    private void AnimateItemAppear(Image itemIcon)
    {
        // 初期状態を設定
        itemIcon.transform.localScale = Vector3.zero;
        itemIcon.transform.localRotation = Quaternion.Euler(0, 0, initialRotation); // 初期回転を設定
        itemIcon.color = _activeColor;
        
        // 回転アニメーション（初期回転から0度へ）
        LMotion.Create(initialRotation, 0f, animationDuration * 0.3f)
            .WithEase(Ease.OutBack)
            .BindToLocalEulerAnglesZ(itemIcon.transform);
        
        // スケールアニメーション
        LMotion.Create(Vector3.zero, _originalIconScale, animationDuration)
            .WithEase(Ease.OutElastic)
            .BindToLocalScale(itemIcon.transform);
    }
}
