using UnityEngine;

namespace INTP.Data.ConfigSO
{
    /// <summary>
    /// 游戏配置 ScriptableObject
    /// 用于配置游戏参数，可通过编辑器直接调整
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "INTP/Config/GameConfig")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Player Settings")]
        [SerializeField] public float playerWalkSpeed = 5f;
        [SerializeField] public float playerSprintSpeed = 10f;
        [SerializeField] public float playerAcceleration = 10f;

        [Header("Vehicle Settings")]
        [SerializeField] public float vehicleMoveSpeed = 15f;
        [SerializeField] public float vehicleLateralSpeed = 10f;
        [SerializeField] public float vehicleAcceleration = 15f;

        [Header("Inventory Settings")]
        [SerializeField] public int inventorySlotCount = 12;
        [SerializeField] public float maxPlacementDistance = 50f;

        [Header("Placement Validation")]
        [SerializeField] public float maxSurfaceAngle = 45f;
        [SerializeField] public float minDistanceFromOther = 2f;
        [SerializeField] public LayerMask planetLayerMask = LayerMask.GetMask("Planet");

        [Header("Camera Settings")]
        [SerializeField] public float mouseSensitivity = 1f;
        [SerializeField] public float fov = 60f;

        [Header("Dream Settings")]
        [SerializeField] public float dreamCaptureWindow = 2f;

        [Header("Debug")]
        [SerializeField] public bool enableDebugLogging = true;
    }
}
