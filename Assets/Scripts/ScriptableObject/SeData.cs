using UnityEngine;

[CreateAssetMenu(fileName = "SeData", menuName = "ScriptableObject/SeData")]
public class SeData : ScriptableObject
{
    public AudioClip audioClip;
    public float volume = 1.0f;
}
