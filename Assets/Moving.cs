using System.Collections;
using UnityEngine;

public class DogfightAudioController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform playerTransform; // 自機（AudioListenerがあるオブジェクト）
    [SerializeField] private AudioSource enemyEngineAudio; // 敵機のエンジン音

    [Header("30秒間のドッグファイト軌道パラメータ")]
    [SerializeField] private float duration = 30f;

    // 自機からの距離（小＝接近、大＝遠ざかる）
    [SerializeField] private AnimationCurve distanceCurve = AnimationCurve.Linear(0, 50, 30, 100);
    // 自機周りの水平角度（度数法：0=前方, 90=右, 180=後方, 360=1周）
    [SerializeField] private AnimationCurve angleCurve = AnimationCurve.Linear(0, 180, 30, 540);
    // 自機に対する相対的な高度（Y軸）
    [SerializeField] private AnimationCurve heightCurve = AnimationCurve.Constant(0, 30, 0);

    public void StartDogfightSequence()
    {
        StartCoroutine(DogfightRoutine());
    }

    private IEnumerator DogfightRoutine()
    {
        enemyEngineAudio.Play();
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // カーブからパラメータを取得
            float currentDistance = distanceCurve.Evaluate(elapsedTime);
            float currentAngle = angleCurve.Evaluate(elapsedTime);
            float currentHeight = heightCurve.Evaluate(elapsedTime);

            // 角度から方向ベクトルを計算（自機の向きを基準にする）
            Quaternion rotation = Quaternion.Euler(0, currentAngle, 0);
            Vector3 offsetDirection = rotation * playerTransform.forward;

            // 最終的な敵機音源の位置を計算
            Vector3 targetPosition = playerTransform.position 
                                   + (offsetDirection * currentDistance) 
                                   + (Vector3.up * currentHeight);

            // 補間（SmoothDampやTransform直接代入）で移動させる
            transform.position = targetPosition;

            yield return null;
        }
    }
}