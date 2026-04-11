# INTP 项目总体概览

## 🎮 项目简介

**INTP**（Intelligent Niche Thriving Platform）是一个基于Unity引擎的科研教育互动平台。项目采用 **状态驱动 + 数据驱动 + 事件总线** 架构，为群落模拟、梦境交互和物品管理等系统提供了完整的框架基础。

## 📊 项目统计

### 代码统计
- **脚本层数**：4层架构（Foundation → Core → Gameplay → Data）
- **核心脚本数**：22个系统类
- **命名空间**：
  - `INTP.Foundation` - 基础服务
  - `INTP.Core` - 核心系统
  - `INTP.Gameplay` - 游戏玩法
  - `INTP.Data` - 数据配置

### 系统架构
```
Foundation Layer (基础服务)
    ↓
Core Layer (核心系统：状态机、输入管理)
    ↓
Gameplay Layer (游戏玩法：玩家、背包、梦境)
    ↓
Data Layer (数据配置)
```

## 🏗️ 已完成的主要系统

### 1. **基础服务层 (Foundation)**
| 类名 | 功能 | 备注 |
|------|------|------|
| `EventBus` | 全局事件总线 | 支持发布/订阅模式 |
| `TimeService` | 时间管理 | 支持游戏加速/减速/暂停 |
| `SettingsService` | 设置管理 | 音频、画面、控制等 |
| `SaveService` | 存档管理 | 支持游戏存读 |

### 2. **核心系统层 (Core)**

#### 状态机系统
| 状态机 | 状态 | 用途 |
|-------|------|------|
| `GameFlowStateMachine` | Boot → Playing → Paused → DreamOverlay | 管理游戏主流程 |
| `PlayerModeStateMachine` | WalkFPS ↔ VehicleFPS | 管理玩家行动模式 |
| `InteractionStateMachine` | Normal → BackpackOpen/DraggingPlaceable/DreamSelecting | 管理交互状态 |

#### 输入系统
| 组件 | 功能 |
|------|------|
| `InputContextRouter` | 根据GameFlowState自动切换ActionMap |
| `ModeSwitchGuard` | 防止非法模式切换 |
| `PlayerInputManager` | PlayerInput统一管理和初始化 |
| `InputActionAssetLoader` | InputAction资源加载（Resources/Inspector) |

#### 启动系统
| 组件 | 功能 |
|------|------|
| `SystemBootstrap` | 单例启动器，初始化所有系统 |
| `GameFlowController` | 游戏流程控制器 |

### 3. **游戏玩法层 (Gameplay)**

#### 玩家系统
| 组件 | 功能 |
|------|------|
| `PlayerLocomotionController` | 位移控制（步行/冲刺） |
| `PlayerMovementInputHandler` | 移动输入处理 |
| `VehiclePilotController` | 飞行器驾驶控制 |

#### 背包系统
| 组件 | 功能 |
|------|------|
| `InventoryService` | 物品和槽位管理（支持堆叠） |
| `PlacementValidator` | 拖拽摆放验证（距离、碰撞、地表） |
| `PlanetRaycastService` | 星球地表射线检测 |

#### 梦境系统
| 组件 | 功能 |
|------|------|
| `DreamSystemController` | 梦境视频播放和元素捕捉 |

### 4. **数据层 (Data)**
| 组件 | 功能 |
|------|------|
| `GameConfigSO` | 游戏配置ScriptableObject |

## 🎯 核心特性

### ✅ 已实现
- [x] 三层状态机系统（游戏流程 → 玩家模式 → 交互模式）
- [x] 事件总线解耦系统
- [x] InputSystem集成和ActionMap自动切换
- [x] 模式切换守卫（防止非法切换）
- [x] 物品定义和背包槽位管理
- [x] 拖拽摆放的距离、碰撞、地表验证
- [x] 梦境系统基础框架
- [x] 玩家移动和飞行器控制框架

### 🔄 进行中
- [ ] 背包UI界面
- [ ] 梦境选择UI
- [ ] 完整拖拽摆放反馈（虚影、粒子效果）

### 📋 待实现
- [ ] 群落模拟系统（Aging/饥饿/繁殖/疾病）
- [ ] Gsplat 集成和VFX反馈
- [ ] RuleGraph 参数体系
- [ ] 设置菜单扩展
- [ ] 性能优化和配置稳定化

## 📁 项目文件结构

```
INTP/
├── Assets/
│   ├── Scripts/                    # 脚本系统
│   │   ├── Foundation/             # 基础服务层
│   │   ├── Core/                   # 核心系统层
│   │   ├── Gameplay/               # 游戏玩法层
│   │   ├── Data/                   # 数据配置层
│   │   ├── GameManager.cs          # 游戏管理器（预留）
│   │   └── README.md               # 脚本详细文档
│   ├── Resources/                  # 资源文件
│   ├── Scenes/                     # 场景文件
│   ├── Arts/                       # 美术资源
│   ├── gsplat-unity-vfx/          # Gsplat VFX包
│   └── TextMesh Pro/               # TMP资源
├── Packages/                       # Unity包
├── ProjectSettings/                # 项目设置
├── plan.mb                         # 项目规划文档
├── INTP.sln                        # Visual Studio解决方案
└── PROJECT_OVERVIEW.md             # 本文档
```

## 🚀 快速开始指南

### 环境要求
- Unity 版本：2022 LTS 或更高
- 依赖包：
  - InputSystem
  - TextMeshPro
  - URP (可选)

### 初始化步骤

1. **创建启动场景**
   ```
   - 在 Assets/Scenes 下创建 "Main" 场景
   - 创建一个空GameObject命名为 "Bootstrap"
   ```

2. **添加启动系统**
   - 添加 `SystemBootstrap` 组件到 Bootstrap GameObject
   - 添加 `InputContextRouter` 组件
   - 添加 `ModeSwitchGuard` 组件
   - 添加 `PlayerInputManager` 组件

3. **配置InputSystem**
   - 在 Inspector 中的 `PlayerInputManager` 将 `InputSystem_Actions.inputactions` 分配给 `_inputActions` 字段
   - 确保包含以下ActionMap：
     - `Player`：Move, Look, Sprint, SwitchMode, OpenBackpack, DreamHold, Pause
     - `UI`：Point, Click, Navigate, Submit, Cancel

4. **创建玩家**
   - 创建玩家GameObject，添加：
     - `CharacterController`
     - `PlayerLocomotionController`
     - `PlayerMovementInputHandler`
     - `VehiclePilotController`
     - `Rigidbody`

5. **创建配置**
   - 在 `Assets/Data/ConfigSO` 创建 GameConfig ScriptableObject
   - 根据需要调整参数

6. **运行游戏**
   - 按 Play 按钮启动游戏

## 📖 文档导航

| 文档 | 位置 | 内容 |
|------|------|------|
| **项目规划** | `/plan.mb` | 完整的架构设计和需求分析 |
| **脚本文档** | `/Assets/Scripts/README.md` | 脚本系统详细说明 |
| **项目概览** | `/PROJECT_OVERVIEW.md` | 本文档 |
| **脚本注释** | 各脚本文件 | 详细的方法注释和伪代码 |

## 🔌 扩展机制

### 1. 添加新的游戏事件
```csharp
// 1. 定义事件类
public class CustomGameEvent : IGameEvent
{
    public string eventData;
}

// 2. 发布事件
EventBus.Instance.Publish(new CustomGameEvent { eventData = "..." });

// 3. 订阅事件
EventBus.Instance.Subscribe<CustomGameEvent>(e => {
    // 处理事件
});
```

### 2. 扩展状态机
```csharp
// 1. 添加新状态到枚举
public enum CustomState { StateA, StateB, StateC }

// 2. 创建状态机
public class CustomStateMachine : MonoBehaviour
{
    private CustomState _currentState;
    
    public void Transition(CustomState newState)
    {
        // 状态转换逻辑
        OnStateChanged?.Invoke(_currentState, newState);
        _currentState = newState;
    }
}
```

### 3. 添加新的输入Action
```
1. 在 InputSystem_Actions.inputactions 中添加新Action
2. 在 PlayerMovementInputHandler 或其他处理器中订阅该Action
3. 注册回调处理
```

## ⚡ 性能考虑

- **单例模式**：所有基础服务使用单例，避免重复创建
- **事件系统**：使用事件总线而不是直接引用，减少耦合
- **对象池预留**：在 Gameplay 层预留对象池机制位置
- **配置驱动**：使用ScriptableObject驱动参数，便于调整而不需要修改代码

## 🐛 已知问题和注意事项

1. **InputSystem 配置**
   - 确保 InputSystem_Actions.inputactions 文件被正确加载
   - 提供三种加载方式保证兼容性

2. **Layer 设置**
   - 创建 "Planet" layer 用于射线检测
   - 背包中的物品需要设置正确的collider

3. **生命周期管理**
   - SystemBootstrap 具有 DontDestroyOnLoad，场景切换时保留
   - 其他单例服务自动创建，避免重复创建

## 🔄 版本历史

| 版本 | 日期 | 内容 |
|------|------|------|
| v1.0 | 2026-04-10 | 框架搭建完成，22个核心系统类 |
| - | 2026-04-09 | 初始框架搭建 |

## 📞 支持

- **文档**: 查看 `/Assets/Scripts/README.md` 和各脚本中的注释
- **示例**: 查看 `InputActionsExample.cs` 了解事件使用示例
- **配置**: 通过 GameConfigSO 进行全局配置

---

**最后更新**：2026-04-10  
**维护者**：INTP 项目团队  
**许可证**：Internal Use Only
