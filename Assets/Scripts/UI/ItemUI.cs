using UnityEngine;
using UnityEngine.UI;
using R3;
using System.Collections.Generic;
using TMPro;

public class ItemUI : MonoBehaviour
{
    [SerializeField] private List<Image> itemIcons = new ();
    [SerializeField] private TextMeshProUGUI itemCountText;
    
    private readonly Color _activeColor = new (1f, 1f, 1f, 1f); // アクティブ状態の色（白）
    private readonly Color _inactiveColor = new (1f, 1f, 1f, 0f); // 非アクティブ状態の色（半透明）
    
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
            if (itemIcons[i])
            {
                itemIcons[i].color = i < itemCount ? _activeColor : _inactiveColor;
            }
        }
        itemCountText.text = $"{itemCount} / {itemIcons.Count}";
    }
}
