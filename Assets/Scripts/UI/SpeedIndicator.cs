using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class SpeedIndicator : MonoBehaviour
{
    [Header("Speed Level Sprites")]
    [Tooltip("速度レベル0-4に対応するスプライト（5つ）")]
    [SerializeField] private List<Sprite> speedSprites = new ();
    
    private Image _speedImage;

    private void Start()
    {
        _speedImage = this.GetComponent<Image>();

        // 初期スプライトを設定
        UpdateSpeedDisplay(0);

        // プレイヤーの速度レベルを購読
        var player = GameManager.Instance.Player;
        player.PlayerItemCountInt.Subscribe(UpdateSpeedDisplay).AddTo(this);
    }

    private void UpdateSpeedDisplay(int speedLevel)
    {
        // 速度レベルを0-4の範囲にクランプ
        speedLevel = Mathf.Clamp(speedLevel, 0, 4);
        
        // 対応するスプライトに変更
        _speedImage.sprite = speedSprites[speedLevel];
    }
}