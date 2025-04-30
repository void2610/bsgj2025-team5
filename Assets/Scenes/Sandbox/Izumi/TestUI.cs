using TMPro;
using UnityEngine;
using R3;

public class TestUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerAltitudeText;
    [SerializeField] private PlayerState playerState;

    private void Start()
    {
        playerState.Position.Subscribe(pos =>
             playerAltitudeText.text = $"Altitude: {pos.y:F2}"
        ).AddTo(this);
    }
}
