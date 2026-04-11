using UnityEngine;
using System;
using System.IO;

namespace INTP.Foundation
{
    /// <summary>
    /// 存档服务 - 管理游戏存档的读写
    /// </summary>
    public class SaveService : MonoBehaviour
    {
        private static SaveService _instance;
        private string _savePath;

        public static SaveService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = new GameObject("SaveService");
                    _instance = obj.AddComponent<SaveService>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(_savePath))
                Directory.CreateDirectory(_savePath);
        }

        /// <summary>
        /// 保存游戏数据（JSON格式）
        /// </summary>
        public void SaveGame(string slotName, GameSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                string filePath = Path.Combine(_savePath, $"{slotName}.save");
                File.WriteAllText(filePath, json);
                Debug.Log($"Game saved to {filePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save game: {ex}");
            }
        }

        /// <summary>
        /// 加载游戏数据
        /// </summary>
        public GameSaveData LoadGame(string slotName)
        {
            try
            {
                string filePath = Path.Combine(_savePath, $"{slotName}.save");
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var data = JsonUtility.FromJson<GameSaveData>(json);
                    Debug.Log($"Game loaded from {filePath}");
                    return data;
                }
                else
                {
                    Debug.LogWarning($"Save file not found: {filePath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load game: {ex}");
                return null;
            }
        }

        /// <summary>
        /// 检查存档是否存在
        /// </summary>
        public bool SaveExists(string slotName)
        {
            string filePath = Path.Combine(_savePath, $"{slotName}.save");
            return File.Exists(filePath);
        }

        /// <summary>
        /// 删除存档
        /// </summary>
        public void DeleteSave(string slotName)
        {
            try
            {
                string filePath = Path.Combine(_savePath, $"{slotName}.save");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"Save deleted: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to delete save: {ex}");
            }
        }
    }

    /// <summary>
    /// 游戏存档数据结构
    /// </summary>
    [System.Serializable]
    public class GameSaveData
    {
        public string slotName;
        public long timestamp;
        public int playTimeSeconds;
        public Vector3SerializableData playerPosition;
        // TODO: 扩展更多字段（背包、梦境进度、群落状态等）
    }

    [System.Serializable]
    public struct Vector3SerializableData
    {
        public float x, y, z;

        public Vector3SerializableData(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }
}
