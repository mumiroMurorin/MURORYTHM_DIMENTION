using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold の3D編集のカメラの動き (Unity のシーンビューと同じ考え方)
    ///
    /// 見ている中心 (Pivot) のまわりを、横の角度 (Yaw) と縦の角度 (Pitch) で回り、中心からの距離 (Distance) で近づく・離れる。
    /// 角度 0, 0 で、奥 (+z。時間が進む向き) を向く (プレイヤーの視点)。
    /// 平行投影のときも、距離で映す広さを決める (カメラそのものは、ずっと後ろに下げて、奥行きの長い帯が切れないようにする)。
    /// </summary>
    public class SpaceHold3DCameraRig
    {
        public const float FIELD_OF_VIEW = 45f;
        const float MIN_DISTANCE = 0.5f;
        const float MAX_DISTANCE = 20000f;
        const float MAX_PITCH = 89f;
        // 平行投影のときに、カメラを中心から下げる距離
        const float ORTHOGRAPHIC_BACK_OFFSET = 4000f;

        public Vector3 Pivot { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Distance { get; set; } = 60f;
        public bool IsOrthographic { get; set; }

        public Quaternion Rotation => Quaternion.Euler(Pitch, Yaw, 0f);

        public Vector3 Forward => Rotation * Vector3.forward;

        /// <summary>
        /// 見ている向きを決める (よく使う向き)
        /// </summary>
        public void SetAngles(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = Mathf.Clamp(pitch, -MAX_PITCH, MAX_PITCH);
        }

        public void Orbit(Vector2 deltaPixels)
        {
            const float DEGREES_PER_PIXEL = 0.3f;
            Yaw += deltaPixels.x * DEGREES_PER_PIXEL;
            Pitch = Mathf.Clamp(Pitch - deltaPixels.y * DEGREES_PER_PIXEL, -MAX_PITCH, MAX_PITCH);
        }

        /// <summary>
        /// 見ている中心を、画面の上でずらす (カーソルに画面が付いてくるように)
        /// </summary>
        /// <param name="viewportHeightPixels">映している範囲の高さ (ピクセル)</param>
        public void Pan(Vector2 deltaPixels, float viewportHeightPixels)
        {
            float worldPerPixel = 2f * Distance * Mathf.Tan(FIELD_OF_VIEW * 0.5f * Mathf.Deg2Rad) / Mathf.Max(1f, viewportHeightPixels);
            Quaternion rotation = Rotation;
            Pivot -= rotation * Vector3.right * (deltaPixels.x * worldPerPixel) + rotation * Vector3.up * (deltaPixels.y * worldPerPixel);
        }

        public void Zoom(float wheel)
        {
            Distance = Mathf.Clamp(Distance * Mathf.Pow(0.87f, wheel), MIN_DISTANCE, MAX_DISTANCE);
        }

        /// <summary>
        /// 範囲 (中心と、半径) が、画面に収まるように近づく・離れる
        /// </summary>
        public void Frame(Vector3 center, float radius, float aspect)
        {
            Pivot = center;

            float halfVertical = FIELD_OF_VIEW * 0.5f * Mathf.Deg2Rad;
            float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * Mathf.Max(0.1f, aspect));
            float half = Mathf.Min(halfVertical, halfHorizontal);
            Distance = Mathf.Clamp(Mathf.Max(radius, 1f) / Mathf.Sin(half) * 1.05f, MIN_DISTANCE, MAX_DISTANCE);
        }

        /// <summary>
        /// カメラに、今の位置と向きを入れる
        /// </summary>
        public void Apply(Camera camera)
        {
            Quaternion rotation = Rotation;
            Vector3 forward = rotation * Vector3.forward;

            camera.orthographic = IsOrthographic;
            camera.fieldOfView = FIELD_OF_VIEW;
            camera.orthographicSize = Distance * Mathf.Tan(FIELD_OF_VIEW * 0.5f * Mathf.Deg2Rad);

            if (IsOrthographic)
            {
                camera.transform.SetPositionAndRotation(Pivot - forward * (Distance + ORTHOGRAPHIC_BACK_OFFSET), rotation);
                camera.nearClipPlane = 1f;
                camera.farClipPlane = Distance + ORTHOGRAPHIC_BACK_OFFSET * 2f + 2000f;
            }
            else
            {
                camera.transform.SetPositionAndRotation(Pivot - forward * Distance, rotation);
                camera.nearClipPlane = Mathf.Max(0.02f, Distance * 0.002f);
                camera.farClipPlane = Distance * 20f + 2000f;
            }
        }
    }
}
