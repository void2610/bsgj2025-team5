using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    [SerializeField] private PlayerState playerState;
    private void FixedUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            this.GetComponent<Rigidbody2D>().AddForce(Vector2.up * 5f, ForceMode2D.Impulse);

        playerState.SetPosition(this.transform.position);
    }
}
