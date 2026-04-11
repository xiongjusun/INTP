using System;
using System.Collections.Generic;
using UnityEngine;
using INTP.Foundation;

namespace INTP.Core.StateMachine
{
    /// <summary>
    /// 游戏流程状态定义
    /// </summary>
    public enum GameFlowState
    {
        Boot,            // 启动阶段
        Playing,         // 正常游戏
        DreamOverlay,    // 梦境模式
        Paused           // 暂停
    }

    /// <summary>
    /// 游戏流程状态机
    /// </summary>
    public class GameFlowStateMachine : MonoBehaviour
    {
        private GameFlowState _currentState = GameFlowState.Boot;
        private Dictionary<GameFlowState, BaseGameFlowState> _states = new();
        private EventBus _eventBus;

        public GameFlowState CurrentState => _currentState;

        public delegate void StateChangedDelegate(GameFlowState newState);
        public event StateChangedDelegate OnStateChanged;

        public event Action OnEnterPlaying;
        public event Action OnExitPlaying;
        public event Action OnEnterPaused;
        public event Action OnExitPaused;
        public event Action OnEnterDreamOverlay;
        public event Action OnExitDreamOverlay;

        private void Awake()
        {
            _eventBus = EventBus.Instance;
            InitializeStates();
        }

        private void InitializeStates()
        {
            _states[GameFlowState.Boot] = new BootState(this);
            _states[GameFlowState.Playing] = new PlayingState(this);
            _states[GameFlowState.Paused] = new PausedState(this);
            _states[GameFlowState.DreamOverlay] = new DreamOverlayState(this);
        }

        /// <summary>
        /// 转换到指定状态
        /// </summary>
        public bool TransitionTo(GameFlowState newState)
        {
            if (newState == _currentState)
                return false;

            var oldState = _currentState;
            _currentState = newState;

            Debug.Log($"GameFlow: {oldState} -> {newState}");

            // 触发相应的退出事件
            switch (oldState)
            {
                case GameFlowState.Playing:
                    OnExitPlaying?.Invoke();
                    break;
                case GameFlowState.Paused:
                    OnExitPaused?.Invoke();
                    break;
                case GameFlowState.DreamOverlay:
                    OnExitDreamOverlay?.Invoke();
                    break;
            }

            // 触发相应的进入事件
            switch (newState)
            {
                case GameFlowState.Playing:
                    OnEnterPlaying?.Invoke();
                    break;
                case GameFlowState.Paused:
                    OnEnterPaused?.Invoke();
                    break;
                case GameFlowState.DreamOverlay:
                    OnEnterDreamOverlay?.Invoke();
                    break;
            }

            OnStateChanged?.Invoke(newState);
            _eventBus.Publish(new GameFlowStateChangedEvent { newState = newState, oldState = oldState });

            return true;
        }
    }

    // 基础状态类
    public abstract class BaseGameFlowState
    {
        protected GameFlowStateMachine _stateMachine;

        public BaseGameFlowState(GameFlowStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public virtual void OnEnter() { }
        public virtual void OnExit() { }
        public virtual void OnUpdate() { }
    }

    public class BootState : BaseGameFlowState
    {
        public BootState(GameFlowStateMachine stateMachine) : base(stateMachine) { }

        public override void OnEnter()
        {
            Debug.Log("Entering Boot state");
            // 这里之后会放系统初始化逻辑
        }
    }

    public class PlayingState : BaseGameFlowState
    {
        public PlayingState(GameFlowStateMachine stateMachine) : base(stateMachine) { }

        public override void OnEnter()
        {
            Debug.Log("Entering Playing state");
            TimeService.Instance.Resume();
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Playing state");
        }
    }

    public class PausedState : BaseGameFlowState
    {
        public PausedState(GameFlowStateMachine stateMachine) : base(stateMachine) { }

        public override void OnEnter()
        {
            Debug.Log("Entering Paused state");
            TimeService.Instance.Pause();
        }

        public override void OnExit()
        {
            Debug.Log("Exiting Paused state");
            TimeService.Instance.Resume();
        }
    }

    public class DreamOverlayState : BaseGameFlowState
    {
        public DreamOverlayState(GameFlowStateMachine stateMachine) : base(stateMachine) { }

        public override void OnEnter()
        {
            Debug.Log("Entering DreamOverlay state");
            TimeService.Instance.Pause();
        }

        public override void OnExit()
        {
            Debug.Log("Exiting DreamOverlay state");
            TimeService.Instance.Resume();
        }
    }

    /// <summary>
    /// 游戏流程状态改变事件
    /// </summary>
    public class GameFlowStateChangedEvent : IGameEvent
    {
        public GameFlowState oldState;
        public GameFlowState newState;
    }
}
