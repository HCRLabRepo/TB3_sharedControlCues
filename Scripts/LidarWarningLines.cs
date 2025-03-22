using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class LidarWarningLines : MonoBehaviour
    {
        [Header("References")]
        public Transform robotTransform;         // 机器人的 Transform
        public LaserScanReader laserScanReader;  // LIDAR 读取器

        [Header("Distances")]
        public float dangerDistance = 0.8f;      // 危险距离阈值
        public float safeDistance = 1.0f;        // 安全距离阈值

        [Header("Line Settings")]
        public float lineWidth = 0.01f;                          // 可调节的线宽
        public Vector3 linePositionOffset = Vector3.zero;        
        // public Vector3 lineStartPoint = Vector3.zero;            
        // public Vector3 lineEndPoint = Vector3.zero;              

        [Header("Materials")]
        public Material warningMaterial;         // 警示半弧的材质

        [Header("Arc Settings")]
        public float arcAngleRange = 60f;        // 半弧的总角度范围（±30度）
        public int arcSegments = 20;             // 定义半弧的段数

        private LineRenderer warningLine45;      // 45度方向的 LineRenderer
        private LineRenderer warningLine315; 
        private LineRenderer warningLineForward;    // 315度方向的 LineRenderer

        // LIDAR 扫描参数
        public float startAngle = 0f;            // LIDAR 扫描的起始角度
        public float angleIncrement = 1f;        // 每个 LIDAR 测量之间的角度增量（假设每度一个索引）

        void Start()
        {
            if (robotTransform == null)
            {
                Debug.LogError("Robot Transform is not assigned.");
                enabled = false;
                return;
            }

            // 初始化 LineRenderers
            warningLine45 = CreateLineRenderer(warningMaterial, "WarningLine45");
            warningLine315 = CreateLineRenderer(warningMaterial, "WarningLine315");
            warningLineForward = CreateLineRenderer(warningMaterial, "WarningLineForward");

            // 初始化固定位置的警示线（45度和315度）
            InitializeWarningLines();
        }

        void InitializeWarningLines()
        {
            // 在固定的安全距离处创建45度和315度方向的半弧点
            Vector3[] arcPoints45 = CreateSemiArc(safeDistance, 45f, arcSegments);
            Vector3[] arcPoints315 = CreateSemiArc(safeDistance, 315f, arcSegments);
            Vector3[] arcPointsForward = CreateSemiArc(safeDistance, 0f, arcSegments);

            // 将点设置到 LineRenderers
            warningLine45.positionCount = arcPoints45.Length;
            warningLine45.SetPositions(arcPoints45);
            warningLine45.enabled = false; // 初始时禁用

            warningLine315.positionCount = arcPoints315.Length;
            warningLine315.SetPositions(arcPoints315);
            warningLine315.enabled = false; // 初始时禁用

            warningLineForward.positionCount = arcPointsForward.Length;
            warningLineForward.SetPositions(arcPointsForward);
            warningLineForward.enabled = false;
        }

        void Update()
        {
            if (laserScanReader == null || robotTransform == null)
            {
                Debug.LogWarning("LaserScanReader or RobotTransform is not assigned.");
                return;
            }

            // 获取 LIDAR 数据
            float[] ranges = laserScanReader.Scan();
            if (ranges == null || ranges.Length == 0)
            {
                Debug.LogWarning("No LIDAR data received.");
                return;
            }

            // 获取45度和315度方向的距离
            // 根据 LIDAR 的 startAngle 和 angleIncrement 计算索引
            int index45 = Mathf.RoundToInt((45f - startAngle) / angleIncrement);
            int index315 = Mathf.RoundToInt((315f - startAngle) / angleIncrement);

            // 确保索引在有效范围内
            index45 = Mathf.Clamp(index45, 0, ranges.Length - 1);
            index315 = Mathf.Clamp(index315, 0, ranges.Length - 1);

            float range45 = ranges[index45];
            float range315 = ranges[index315];

            // 处理45度和315度方向
            bool isLine45 = UpdateWarningLine(warningLine315, range45);
            bool isLine315 = UpdateWarningLine(warningLine45, range315);

            if (isLine45 && isLine315)
            {
                warningLine45.enabled = false;
                warningLine315.enabled = false;
                UpdateWarningLine(warningLineForward, Mathf.Min(range45, range315));
            }
            else
            {
                // 如果不是同时有效，隐藏正前方警示线
                warningLineForward.enabled = false;
            }

            
        }
        
        bool UpdateWarningLine(LineRenderer warningLine, float distance)
        {
            if (distance > 0 && distance < safeDistance)
            {
                // 根据距离计算颜色强度（距离越近，颜色越深）
                float intensity = 1 - Mathf.Clamp01((distance - dangerDistance)/(safeDistance-dangerDistance));
                // Debug.Log(intensity);
                intensity = Mathf.Clamp(intensity, 0f, 1f);
                Color warningColor = new Color(1f, 0f, 0f, intensity); // 红色，透明度根据强度变化
                
                warningLine.material.SetColor("_Color", warningColor);
                warningLine.enabled = true;
                return true;
            }
            else
            {
                // 如果没有检测到障碍物或距离安全，则隐藏警示线
                warningLine.enabled = false;
                return false;
            }
        }
        
        Vector3[] CreateSemiArc(float radius, float directionAngle, int segments)
        {
            Vector3[] points = new Vector3[segments + 1];
            float startArc = directionAngle - (arcAngleRange / 2f);
            float endArc = directionAngle + (arcAngleRange / 2f);
            float step = (endArc - startArc) / segments;

            // Debug.Log($"Creating semi-arc: Direction Angle = {directionAngle}°, Start Arc = {startArc}°, End Arc = {endArc}°, Step = {step}°");

            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = startArc + step * i;
                float rad = currentAngle * Mathf.Deg2Rad;
                // 使用Sin和Cos来匹配Unity坐标系，0度指向正前方（Z+）
                Vector3 point = new Vector3(radius * Mathf.Sin(rad), 0, radius * Mathf.Cos(rad));
                points[i] = point; // 相对于机器人

                // Debug.Log($"Point {i}: {points[i]}");
            }

            return points;
        }

        
        LineRenderer CreateLineRenderer(Material material, string name)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.parent = robotTransform; // 作为机器人 Transform 的子对象，以便跟随机器人
            lineObject.transform.localPosition = linePositionOffset; // 警示线相对于机器人原点
            lineObject.transform.localRotation = Quaternion.identity; // 确保没有额外旋转
            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = material;
            lineRenderer.startWidth = lineWidth; // 可调节的线宽
            lineRenderer.endWidth = lineWidth;
            lineRenderer.positionCount = 0;
            lineRenderer.useWorldSpace = false; // 使用本地空间，因为它是机器人 Transform 的子对象
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.loop = false;
            lineRenderer.numCapVertices = 2;
            lineRenderer.enabled = false; // 初始时禁用
            return lineRenderer;
        }
    }
}
