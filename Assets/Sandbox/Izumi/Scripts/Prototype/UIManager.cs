using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;

namespace Izumi.Prototype
{
    public class UIManager : SingletonMonoBehaviour<UIManager>
    {
        [SerializeField] private TextMeshProUGUI itemCountText;
        [SerializeField] private TextMeshProUGUI playerSpeedText;

        private void Start()
        {
            GameManager.Instance.ItemCount.Subscribe(v => itemCountText.text = $"Item: {v}/5").AddTo(this);
            GameManager.Instance.Player.PlayerSpeedInt.Subscribe(v => playerSpeedText.text = $"Speed: {v}/4").AddTo(this);
        }
    }
}