using System.Collections.Generic;
using UnityEngine;

public class VideoTimelineTrajectoryPlayer : MonoBehaviour
{
    public enum MovementType
    {
        Linear,     // 直線移動
        Arc,        // 二次ベジェ曲線（山なりなど）
        Circular    // 円運動（中心点基準の旋回）
    }

    [System.Serializable]
    public struct PathKeyframe
    {
        [Header("動画の時刻（秒）")]
        public float time;

        [Header("その時刻での位置 (X, Y, Z)")]
        public Vector3 position; // Linear/Arcでは終点、Circularでは円運動の終点

        [Header("その時刻での角度 (Pitch, Yaw, Roll)")]
        public Vector3 rotation;

        [Header("次のキーフレームまでの速度変化")]
        public AnimationCurve easingCurve;

        [Header("次のキーフレームまでの軌道タイプ")]
        public MovementType movementType;

        [Header("【Arc用】カーブオフセット")]
        public Vector3 arcOffset;

        [Header("【Circular用】回転の中心点")]
        public Vector3 orbitCenter;

        [Header("【Circular用】回転の軸（通常は0,1,0で水平）")]
        public Vector3 orbitAxis; // デフォルト (0, 1, 0)

        [Header("【Circular用】時計回り（ON）/ 反時計回り（OFF）")]
        public bool isClockwise;
    }

    [Header("動画に合わせたキーフレーム一覧")]
    [SerializeField] private List<PathKeyframe> keyframes = new List<PathKeyframe>();

    [Header("再生コントロール")]
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool isLooping = false;

    private float currentTime = 0f;
    private bool isPlaying = false;

    private void Start()
    {
    if (playOnStart) Play();
    }

    public void Play()
    {
        currentTime = 0f;
        isPlaying = true;
    }

    private void Update()
    {
        if (!isPlaying || keyframes == null || keyframes.Count < 2) return;

        currentTime += Time.deltaTime;
        float maxTime = keyframes[keyframes.Count - 1].time;

        if (currentTime >= maxTime)
        {
            if (isLooping) currentTime %= maxTime;
            else { currentTime = maxTime; isPlaying = false; }
        }

        // 現在の区間を特定して位置・回転を計算
        for (int i = 0; i < keyframes.Count - 1; i++)
        {
            if (currentTime >= keyframes[i].time && currentTime <= keyframes[i + 1].time)
            {
                float startTime = keyframes[i].time;
                float endTime = keyframes[i + 1].time;
                
                // 時間の進捗率 (0.0 ～ 1.0)
                float rawT = (currentTime - startTime) / (endTime - startTime);

                // 速度変化（イージング）の適用
                AnimationCurve curve = keyframes[i].easingCurve;
                float t = (curve != null && curve.length > 0) ? curve.Evaluate(rawT) : rawT;

                // 位置の計算
                Vector3 startPos = keyframes[i].position;
                Vector3 endPos = keyframes[i + 1].position;

                switch (keyframes[i].movementType)
                {
                    case MovementType.Linear:
                        transform.position = Vector3.Lerp(startPos, endPos, t);
                        break;

                    case MovementType.Arc:
                        // 二次ベジェ曲線
                        Vector3 controlPoint = (startPos + endPos) * 0.5f + keyframes[i].arcOffset;
                        float oneMinusT = 1f - t;
                        transform.position = oneMinusT * oneMinusT * startPos + 
                                             2f * oneMinusT * t * controlPoint + 
                                             t * t * endPos;
                        break;

                    case MovementType.Circular:
                        // 円運動 (オービット)
                        transform.position = CalculateOrbitPosition(
                            startPos, 
                            keyframes[i].orbitCenter, 
                            keyframes[i].orbitAxis, 
                            keyframes[i].isClockwise, 
                            endPos, 
                            t);
                        break;
                }

                // 回転の計算 (Slerp)
                Quaternion startRot = Quaternion.Euler(keyframes[i].rotation);
                Quaternion endRot = Quaternion.Euler(keyframes[i + 1].rotation);
                transform.rotation = Quaternion.Slerp(startRot, endRot, t);

                break;
            }
        }
    }

    // 円運動の位置計算関数
    private Vector3 CalculateOrbitPosition(Vector3 start, Vector3 center, Vector3 axis, bool clockwise, Vector3 end, float t)
    {
        if (axis == Vector3.zero) axis = Vector3.up;
        // 中心からのベクトル
        Vector3 vStart = start - center;
        Vector3 vEnd = end - center;

        // 初期半径と最終半径（半径が変化する場合に対応）
        float rStart = vStart.magnitude;
        float rEnd = vEnd.magnitude;

        // 平面上のベクトルに正規化
        Vector3 vStartNorm = vStart.normalized;
        Vector3 vEndNorm = vEnd.normalized;

        // 回転軸と直交する平面上での開始・終了角度（ラジアン）を求める
        // ここでは簡易的に、axisを上向きとした平面での角度を計算
        float angleStart = Mathf.Atan2(Vector3.Dot(vStartNorm, Vector3.right), Vector3.Dot(vStartNorm, Vector3.forward));
        float angleEnd = Mathf.Atan2(Vector3.Dot(vEndNorm, Vector3.right), Vector3.Dot(vEndNorm, Vector3.forward));

        // 角度差を計算
        float diffAngle = angleEnd - angleStart;

        // atan2の特性（-π～π）による不連続性を補正し、最短距離ではなく、指定方向への回転角度を求める
        if (clockwise) // 時計回り
        {
            if (diffAngle > 0) diffAngle -= 2f * Mathf.PI;
        }
        else // 反時計回り
        {
            if (diffAngle < 0) diffAngle += 2f * Mathf.PI;
        }

        // 現在の角度と半径を補間
        float currentAngle = angleStart + diffAngle * t;
        float currentRadius = Mathf.Lerp(rStart, rEnd, t);

        // 新しい位置を計算
        Quaternion rot = Quaternion.AngleAxis(currentAngle * Mathf.Rad2Deg, axis);
        // 初期ベクトルを開始時の向きに合わせて回転させる必要がある
        // シンプルに、axis平面上の基本ベクトル（例:Forward）を回転させる方式に変更
        Vector3 baseDir = axis == Vector3.up ? Vector3.forward : Vector3.Cross(axis, Vector3.right).normalized;
        
        // baseDir基準だとangleStartがずれるため、vStartNorm自体をaxis周りに回転させる
        // まず、vStartNormから軸方向の成分を除外
        Vector3 planeVStart = Vector3.ProjectOnPlane(vStartNorm, axis).normalized;
        
        // 指定角度分、軸周りに回転させたベクトル
        Vector3 currentDir = Quaternion.AngleAxis(diffAngle * t * Mathf.Rad2Deg, axis) * planeVStart;
        
        // 高度（軸方向の移動）の補間を追加
        Vector3 startHeight = Vector3.Project(vStart, axis);
        Vector3 endHeight = Vector3.Project(vEnd, axis);
        Vector3 currentHeight = Vector3.Lerp(startHeight, endHeight, t);

        return center + currentDir * currentRadius + currentHeight;
    }
}