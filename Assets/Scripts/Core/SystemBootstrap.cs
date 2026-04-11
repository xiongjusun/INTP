﻿﻿using UnityEngine;
using INTP.Foundation;
using INTP.Core.StateMachine;
using INTP.Core.Input;

namespace INTP.Core
{
    /// <summary>
    /// 系统启动器 - 初始化所有系统和管理器
    /// 参考plan.mb第3章节
    /// </summary>
    public class SystemBootstrap : MonoBehaviour
    {
        [SerializeField] private bool _dontDestroyOnLoad = true;
        
        /// <summary>
        /// 初始运行时的玩家模式（可在Inspector中设置）
        /// FPS3DWalk - 第一人称3D步行（鼠标控制镜头）
        /// FPS3DShip - 第一人称3D飞船模拟器
        /// Plane2DCharacter - 2D平面小人
        /// </summary>
        [SerializeField] private PlayerModeState _initialPlayerMode = PlayerModeState.FPS3DWalk;
        
        /// <summary>
        /// 初始运行时的游戏流程状态（通常应该是Playing）
        /// </summary>
        [SerializeField] private GameFlowState _initialGameFlowState = GameFlowState.Playing;

        // 系统管理器引用
        private EventBus _eventBus;
        private TimeService _timeService;
        private SettingsService _settingsService;
        private SaveService _saveService;
        private GameFlowStateMachine _gameFlowStateMachine;
        private PlayerModeStateMachine _playerModeStateMachine;
        private InteractionStateMachine _interactionStateMachine;
        private InputContextRouter _inputContextRouter;
        private ModeSwitchGuard _modeSwitchGuard;

        private static SystemBootstrap _instance;

        public static SystemBootstrap Instance
        {
            get { return _instance; }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;

            if (_dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            InitializeSystems();
        }

        /// <summary>
        /// 初始化所有系统（按照plan.mb中建议的初始化顺序）
        /// </summary>
        private void InitializeSystems()
        {
            Debug.Log("=== SystemBootstrap: Initializing all systems ===");

            // 1. 加载配置（这里先预留，后续接入ScriptableObject配置）
            LoadConfigurations();

            // 2. 注册基础服务
            InitializeFoundationServices();

            // 3. 初始化核心系统
            InitializeCoreSystem();

            // 4. 进入Playing状态
            TransitionToPlaying();

            Debug.Log("=== SystemBootstrap: All systems initialized ===");
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfigurations()
        {
            Debug.Log("Loading configurations...");
            // TODO: 从资源文件加载ScriptableObject配置
        }

        /// <summary>
        /// 初始化Foundation层服务
        /// </summary>
        private void InitializeFoundationServices()
        {
            Debug.Log("Initializing Foundation services...");

            // 事件总线
            _eventBus = EventBus.Instance;
            Debug.Log("✓ EventBus initialized");

            // 时间服务
            _timeService = TimeService.Instance;
            Debug.Log("✓ TimeService initialized");

            // 设置服务
            _settingsService = SettingsService.Instance;
            Debug.Log("✓ SettingsService initialized");

            // 存档服务
            _saveService = SaveService.Instance;
            Debug.Log("✓ SaveService initialized");
        }

        /// <summary>
        /// 初始化Core层系统
        /// </summary>
        private void InitializeCoreSystem()
        {
            Debug.Log("Initializing Core systems...");

            // 获取或创建状态机
            _gameFlowStateMachine = FindObjectOfType<GameFlowStateMachine>();
            if (_gameFlowStateMachine == null)
            {
                var obj = new GameObject("GameFlowStateMachine");
                _gameFlowStateMachine = obj.AddComponent<GameFlowStateMachine>();
            }
            Debug.Log("✓ GameFlowStateMachine initialized");

            _playerModeStateMachine = FindObjectOfType<PlayerModeStateMachine>();
            if (_playerModeStateMachine == null)
            {
                var obj = new GameObject("PlayerModeStateMachine");
                _playerModeStateMachine = obj.AddComponent<PlayerModeStateMachine>();
            }
            Debug.Log("✓ PlayerModeStateMachine initialized");

            _interactionStateMachine = FindObjectOfType<InteractionStateMachine>();
            if (_interactionStateMachine == null)
            {
                var obj = new GameObject("InteractionStateMachine");
                _interactionStateMachine = obj.AddComponent<InteractionStateMachine>();
            }
            Debug.Log("✓ InteractionStateMachine initialized");

            // 初始化输入系统
            _inputContextRouter = FindObjectOfType<InputContextRouter>();
            if (_inputContextRouter == null)
            {
                var obj = new GameObject("InputContextRouter");
                _inputContextRouter = obj.AddComponent<InputContextRouter>();
                Debug.Log("Created InputContextRouter");
            }
            Debug.Log("✓ InputContextRouter initialized");

            // 初始化模式切换守卫
            _modeSwitchGuard = FindObjectOfType<ModeSwitchGuard>();
            if (_modeSwitchGuard == null)
            {
                var obj = new GameObject("ModeSwitchGuard");
                _modeSwitchGuard = obj.AddComponent<ModeSwitchGuard>();
            }
            Debug.Log("✓ ModeSwitchGuard initialized");
        }

        /// <summary>
        /// 转换到Playing状态
        /// </summary>
        private void TransitionToPlaying()
        {
            _gameFlowStateMachine.TransitionTo(GameFlowState.Playing);
        }

        /// <summary>
        /// 获取事件总线
        /// </summary>
        public EventBus GetEventBus() => _eventBus;

        /// <summary>
        /// 获取时间服务
        /// </summary>
        public TimeService GetTimeService() => _timeService;

        /// <summary>
        /// 获取设置服务
        /// </summary>
        public SettingsService GetSettingsService() => _settingsService;

        /// <summary>
        /// 获取存档服务
        /// </summary>
        public SaveService GetSaveService() => _saveService;

        /// <summary>
        /// 获取游戏流程状态机
        /// </summary>
        public GameFlowStateMachine GetGameFlowStateMachine() => _gameFlowStateMachine;

        /// <summary>
        /// 获取玩家模式状态机
        /// </summary>
        public PlayerModeStateMachine GetPlayerModeStateMachine() => _playerModeStateMachine;

        /// <summary>
        /// 获取交互状态机
        /// </summary>
        public InteractionStateMachine GetInteractionStateMachine() => _interactionStateMachine;

        /// <summary>
        /// 获取模式切换守卫
        /// </summary>
        public ModeSwitchGuard GetModeSwitchGuard() => _modeSwitchGuard;
    }
}
