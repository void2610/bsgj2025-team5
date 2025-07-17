using UnityEngine;

public class TimeDestroy : MonoBehaviour
{
    [SerializeField]
    private float destroyTime = 10f;  // Time in seconds before the object is destroyed
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, destroyTime);

    }

// Removed the empty Update() method as it is unnecessary.
}
