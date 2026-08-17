using UnityEngine;

public class GameObjectMemo : MonoBehaviour
{
    [Header("GameObjectメモ")]
    [TextArea(3, 10)]
    public string memo = "ここにメモを入力してください";
}