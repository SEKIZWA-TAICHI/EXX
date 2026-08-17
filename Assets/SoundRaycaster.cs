using UnityEngine;

public class SoundRaycaster : MonoBehaviour
{
    [Header("反射の設定")]
    public int maxBounces = 3;       // 反射回数の上限（壁に何回跳ね返すか）
    public float maxDistance = 20f;  // 1回あたりのレイの最大飛距離

    void Update()
    {
        Vector3 currentPos = transform.position;
        Vector3 currentDir = transform.forward;

        // 指定した回数分、壁に当たるまで反射計算を繰り返す
        for (int i = 0; i < maxBounces; i++)
        {
            Ray ray = new Ray(currentPos, currentDir);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                // 反射回数に応じて線の色を変更（1回目:赤、2回目:黄、3回目:緑）
                Color lineColor = Color.red;
                if (i == 1) lineColor = Color.yellow;
                if (i == 2) lineColor = Color.green;

                // 現在地から衝突点まで描画
                Debug.DrawLine(currentPos, hit.point, lineColor);

                // 次のループのための準備（衝突地点を次のスタート地点にし、反射方向を計算）
                currentPos = hit.point;
                currentDir = Vector3.Reflect(currentDir, hit.normal);

                Debug.Log($"[{i + 1}回目の反射] 衝突先: {hit.collider.name}");
            }
            else
            {
                // 何にも当たらなくなったら空中に線を伸ばしてループを終了
                Debug.DrawRay(currentPos, currentDir * maxDistance, Color.gray);
                break;
            }
        }
    }
}