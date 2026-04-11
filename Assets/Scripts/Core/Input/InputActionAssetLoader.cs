using UnityEngine;
using UnityEngine.InputSystem;

namespace INTP.Core.Input
{
    /// <summary>
    /// InputActionAsset 加载器 - 确保 InputSystem_Actions 被正确加载
    /// 这是一个辅助工具，用于解决资源加载问题
    /// </summary>
    public class InputActionAssetLoader : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset _inputActionAsset;

        public static InputActionAsset LoadInputActionAsset()
        {
            // 方法 1: 检查是否已有实例
            var loader = FindObjectOfType<InputActionAssetLoader>();
            if (loader != null && loader._inputActionAsset != null)
            {
                Debug.Log("[InputActionAssetLoader] Found InputActionAsset from InputActionAssetLoader component");
                return loader._inputActionAsset;
            }

            // 方法 2: 从 Resources 加载
            var asset = Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (asset != null)
            {
                Debug.Log("[InputActionAssetLoader] Loaded InputSystem_Actions from Resources folder");
                return asset;
            }

            // 方法 3: 在所有已加载的资源中查找
            var allAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            foreach (var a in allAssets)
            {
                if (a.name.Contains("InputSystem") || a.name == "InputSystem_Actions")
                {
                    Debug.Log($"[InputActionAssetLoader] Found InputActionAsset: {a.name}");
                    return a;
                }
            }

            // 方法 4: 通过 ScriptableObject 查找
            var scriptableObjects = Resources.FindObjectsOfTypeAll<ScriptableObject>();
            foreach (var obj in scriptableObjects)
            {
                if (obj is InputActionAsset inputAction && (obj.name.Contains("InputSystem") || obj.name == "InputSystem_Actions"))
                {
                    Debug.Log($"[InputActionAssetLoader] Found via ScriptableObject: {obj.name}");
                    return inputAction;
                }
            }

            Debug.LogError("[InputActionAssetLoader] Could not find InputSystem_Actions in any location!");
            return null;
        }

        /// <summary>
        /// 获取或加载 InputActionAsset
        /// </summary>
        public static InputActionAsset GetOrLoadInputActionAsset()
        {
            return LoadInputActionAsset();
        }
    }
}
