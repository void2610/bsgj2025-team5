using UnityEngine;
using R3;

[CreateAssetMenu(fileName = "PlayerState", menuName = "ScriptableObjects/PlayerState")]
public class PlayerState : ScriptableObject
{
    //TODO: どこからでも書き込めてしまう
    public ReactiveProperty<Vector3> Position = new(Vector3.zero);
}