using UnityEngine;
using System.Collections.Generic;

namespace INTP.Foundation
{
    /// <summary>
    /// 设置服务 - 管理游戏设置（音频、画面、控制等）
    /// </summary>
    public class SettingsService : MonoBehaviour
    {
        private static SettingsService _instance;
        private Dictionary<string, object> _settings = new();
        private const string PREFS_PREFIX = "INTP_Settings_";

        public static SettingsService Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = new GameObject("SettingsService");
                    _instance = obj.AddComponent<SettingsService>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }

        private void Start()
        {
            InitializeDefaultSettings();
            LoadSettings();
        }

        /// <summary>
        /// 初始化默认设置
        /// </summary>
        private void InitializeDefaultSettings()
        {
            SetDefault("Master_Volume", 1f);
            SetDefault("Music_Volume", 0.8f);
            SetDefault("SFX_Volume", 1f);
            SetDefault("UI_Volume", 1f);
            SetDefault("Mouse_Sensitivity", 1f);
            SetDefault("Mouse_Invert_Y", false);
            SetDefault("FOV", 60f);
            SetDefault("Screen_Brightness", 1f);
            SetDefault("Quality_Level", 2);
        }

        /// <summary>
        /// 设置默认值
        /// </summary>
        private void SetDefault(string key, object value)
        {
            if (!_settings.ContainsKey(key))
                _settings[key] = value;
        }

        /// <summary>
        /// 获取设置值
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            if (_settings.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }

        /// <summary>
        /// 设置值
        /// </summary>
        public void SetSetting(string key, object value)
        {
            _settings[key] = value;
            PlayerPrefs.SetString(PREFS_PREFIX + key, value.ToString());
        }

        /// <summary>
        /// 保存所有设置
        /// </summary>
        public void SaveSettings()
        {
            foreach (var kvp in _settings)
            {
                PlayerPrefs.SetString(PREFS_PREFIX + kvp.Key, kvp.Value.ToString());
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 加载所有设置
        /// </summary>
        public void LoadSettings()
        {
            // 从PlayerPrefs恢复设置（这里保持简单，生产环境可能需要更复杂的序列化）
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 重置为默认设置
        /// </summary>
        public void ResetToDefaults()
        {
            _settings.Clear();
            InitializeDefaultSettings();
            SaveSettings();
        }
    }
}
