# 🎮 Player Mode 系统 - 三种独立 Player 模式

## 📋 概览

项目现在支持 **3 个独立的 Player 对象**，可以随时切换。每个 Player 都有独特的控制方式和视角设置。

---

## 🎯 三种 Player Mode 详解

### 1️⃣ **FPS3D 步行模式** (`PlayerModeState.FPS3DWalk`)

**控制器：** `FPS3DWalkController`

**特点：**
- 第一人称3D视角
- 鼠标控制镜头旋转（上下左右）
- WASD 移动前后左右
- Shift 冲刺加速

**按键映射：**
```
W - 向前移动
S - 向后移动
A - 向左移动
D - 向右移动
Shift - 冲刺
鼠标移动 - 控制镜头
```

**应用场景：**
- 探索日常环境
- 收集物品
- 与NPC交互
- 室内场景导航

**参数设置（Inspector）：**
- `Move Speed` - 正常移动速度（默认5）
- `Sprint Speed` - 冲刺速度（默认10）
- `Acceleration` - 加速度（默认10）
- `Mouse Sensitivity` - 鼠标敏感度（默认2）
- `Max Look Angle` - 最大看角度（默认90）

---

### 2️⃣ **FPS3D 飞船模拟器模式** (`PlayerModeState.FPS3DShip`)

**控制器：** `FPS3DShipController`

**特点：**
- 第一人称3D视角
- 鼠标控制镜头和飞行方向
- WS 朝屏幕中心方向前进后退
- AD 左右平移
- Space/Ctrl 上升/下降

**按键映射：**
```
W - 朝镜头前方前进
S - 朝镜头前方后退
A - 向左移动
D - 向右移动
Space - 上升
Ctrl - 下降
鼠标移动 - 控制视角和飞行方向
```

**应用场景：**
- 驾驶飞行器探索
- 空中战斗
- 3D空间自由移动
- 需要3轴移动的场景

**参数设置（Inspector）：**
- `Move Speed` - 水平移动速度（默认5）
- `Vertical Speed` - 垂直移动速度（默认3）
- `Acceleration` - 加速度（默认10）
- `Mouse Sensitivity` - 鼠标敏感度（默认2）
- `Max Look Angle` - 最大看角度（默认90）

---

### 3️⃣ **2D 平面小人模式** (`PlayerModeState.Plane2DCharacter`)

**控制器：** `Plane2DCharacterController`

**特点：**
- 2D俯视图或侧视图
- WASD 控制上下左右移动
- Shift 冲刺
- 集成 Animator 动画支持

**按键映射：**
```
W - 向上/前移动
S - 向下/后移动
A - 向左移动
D - 向右移动
Shift - 冲刺
```

**应用场景：**
- 2D地牢探索
- 2D解谜游戏
- 俯视图战斗
- 2D关卡导航

**参数设置（Inspector）：**
- `Move Speed` - 正常移动速度（默认5）
- `Sprint Speed` - 冲刺速度（默认10）
- `Acceleration` - 加速度（默认10）
- `Animator` - 绑定的 Animator 组件

**动画参数：**
- `MoveX` (float) - X轴移动方向
- `MoveY` (float) - Y轴移动方向
- `Speed` (float) - 当前速度大小

---

## 🔄 Mode 切换流程

```
按 Tab 键
  ↓
GameFlowController.OnSwitchModePressed()
  ↓
ModeSwitchGuard.CanSwitchMode() ✓ 检查条件
  ↓
PlayerModeStateMachine.TryTransitionMode()
  ↓
PlayerManager.OnPlayerModeChanged()
  ├─ 禁用旧的 Player 对象
  └─ 启用新的 Player 对象
  ↓
发布 PlayerModeChangedEvent
  ↓
所有系统响应 Mode 变化
```

### 循环切换顺序

```
FPS3D Walk
    ↓ (Tab)
FPS3D Ship
    ↓ (Tab)
2D Character
    ↓ (Tab)
FPS3D Walk (循环)
```

---

## 📂 文件结构

```
Assets/Scripts/
├── Core/
│   ├── PlayerManager.cs                    ← 管理三个 Player 对象
│   ├── StateMachine/
│   │   └── PlayerModeStateMachine.cs       ← Mode 状态机（已更新）
│   └── Input/
│       └── ModeSwitchGuard.cs              ← 切换守卫（已更新）
│
└── Gameplay/Player/
    ├── FPS3DWalkController.cs              ← FPS3D 步行控制器
    ├── FPS3DShipController.cs              ← FPS3D 飞船控制器
    ├── Plane2DCharacterController.cs       ← 2D 小人控制器
    └── ... (其他旧的控制器)
```

---

## 🛠️ 设置指南

### 第一步：创建 Player 对象

在场景中创建三个 GameObject：

1. **FPS3D Walk Player**
   - 名称：`PlayerFPS3DWalk`
   - 添加组件：`FPS3DWalkController`
   - 添加组件：`CharacterController`
   - 添加子对象：`Camera` (挂在 FPS3DWalkController 下)
   - 初始状态：禁用（SetActive(false)）

2. **FPS3D Ship Player**
   - 名称：`PlayerFPS3DShip`
   - 添加组件：`FPS3DShipController`
   - 添加组件：`CharacterController`
   - 添加子对象：`Camera` (挂在 FPS3DShipController 下)
   - 初始状态：禁用

3. **2D Character Player**
   - 名称：`Player2DCharacter`
   - 添加组件：`Plane2DCharacterController`
   - 添加组件：`Rigidbody2D` (设为 Kinematic)
   - 添加组件：`Animator` (可选)
   - 初始状态：禁用

### 第二步：配置 PlayerManager

1. 找到或创建 `PlayerManager` GameObject
2. 在 Inspector 中配置：
   - `FPS3D Walk Player Prefab` - 选择 PlayerFPS3DWalk
   - `FPS3D Ship Player Prefab` - 选择 PlayerFPS3DShip
   - `Plane2D Character Prefab` - 选择 Player2DCharacter

### 第三步：设置初始 Mode

1. 找到 `SystemBootstrap` GameObject
2. 在 Inspector 中设置 `Initial Player Mode`
3. 选择想要的初始模式

---

## 💻 使用代码

### 切换 Mode

```csharp
using INTP.Core.Input;

ModeSwitchGuard guard = FindObjectOfType<ModeSwitchGuard>();

// 自动循环切换
if (guard.TryTogglePlayerMode())
{
    Debug.Log("Mode switched!");
}
```

### 直接切换到特定 Mode

```csharp
using INTP.Core.StateMachine;

PlayerModeStateMachine stateMachine = FindObjectOfType<PlayerModeStateMachine>();

// 切换到 FPS3D 飞船
stateMachine.TryTransitionMode(PlayerModeState.FPS3DShip);

// 切换到 2D 小人
stateMachine.TryTransitionMode(PlayerModeState.Plane2DCharacter);

// 切换到 FPS3D 步行
stateMachine.TryTransitionMode(PlayerModeState.FPS3DWalk);
```

### 获取当前 Player

```csharp
using INTP.Core;

PlayerManager manager = FindObjectOfType<PlayerManager>();

GameObject activePlayer = manager.GetActivePlayer();

// 或者获取特定 Mode 的 Player
GameObject shipPlayer = manager.GetPlayerByMode(PlayerModeState.FPS3DShip);
```

### 监听 Mode 变化

```csharp
using INTP.Core.StateMachine;
using INTP.Foundation;

PlayerModeStateMachine stateMachine = FindObjectOfType<PlayerModeStateMachine>();

// 方式1：通过委托
stateMachine.OnModeChanged += (newMode, oldMode) =>
{
    Debug.Log($"Switched from {oldMode} to {newMode}");
    
    // 根据新模式初始化相应系统
    switch (newMode)
    {
        case PlayerModeState.FPS3DWalk:
            // 初始化 FPS3D 步行模式
            OnEnterFPS3DWalk();
            break;
        case PlayerModeState.FPS3DShip:
            // 初始化 FPS3D 飞船模式
            OnEnterFPS3DShip();
            break;
        case PlayerModeState.Plane2DCharacter:
            // 初始化 2D 小人模式
            OnEnterPlane2DCharacter();
            break;
    }
};

// 方式2：通过事件总线
EventBus.Instance.Subscribe<PlayerModeChangedEvent>(evt =>
{
    Debug.Log($"New mode: {evt.newMode}");
});
```

---

## 🎮 UI 交互模式

当进入 UI 交互时，系统会禁用玩家控制：

```csharp
using INTP.Core.StateMachine;

InteractionStateMachine interaction = FindObjectOfType<InteractionStateMachine>();

// 进入 UI 交互模式
interaction.TransitionTo(InteractionState.UIInteraction);
// 现在 PlayerManager 会禁用当前 Player 的控制

// 退出 UI 交互模式
interaction.TransitionTo(InteractionState.Normal);
// 重新启用 Player 控制
```

---

## 🔒 Mode 切换守卫

系统会自动检查以下条件：

```csharp
ModeSwitchGuard.CanSwitchMode() 检查：
  ✓ 游戏是否在 Playing 状态
  ✓ 交互状态是否允许（Normal 或 BackpackOpen）
  ✓ 动画是否在播放
  ✓ 自定义条件
```

**禁用切换：**

```csharp
ModeSwitchGuard guard = FindObjectOfType<ModeSwitchGuard>();

// 禁用 Mode 切换（例如过场动画期间）
guard.SetAnimationPlaying(true);

// 动画完成后重新启用
guard.SetAnimationPlaying(false);
```

---

## 🎬 示例场景流程

### 场景1：探索环境
```
开始 → FPS3D Walk 模式
  ↓ (收集物品)
  ↓ (打开UI菜单)
  ↓ InteractionState → UIInteraction
  ↓ (玩家不能移动，只能操作菜单)
  ↓ (关闭菜单)
  ↓ InteractionState → Normal
  ↓ (继续用 FPS3D Walk 模式探索)
```

### 场景2：多种模式切换
```
开始 → FPS3D Walk 模式
  ↓ (按 Tab)
  ↓ FPS3D Ship 模式
  ↓ (在空中驾驶飞行器)
  ↓ (按 Tab)
  ↓ 2D Character 模式
  ↓ (进入2D地牢)
  ↓ (按 Tab)
  ↓ FPS3D Walk 模式
  ↓ (返回3D世界)
```

---

## 🐛 调试技巧

### 在 Console 中快速测试

```csharp
// 快速切换 Mode
INTP.Core.Input.ModeSwitchGuard guard = UnityEngine.GameObject.FindObjectOfType<INTP.Core.Input.ModeSwitchGuard>();
guard.TryTogglePlayerMode();

// 直接切换到 FPS3D Ship
INTP.Core.StateMachine.PlayerModeStateMachine sm = UnityEngine.GameObject.FindObjectOfType<INTP.Core.StateMachine.PlayerModeStateMachine>();
sm.TryTransitionMode(INTP.Core.StateMachine.PlayerModeState.FPS3DShip);

// 查看当前 Mode
UnityEngine.Debug.Log(sm.CurrentMode);
```

---

## ⚠️ 常见问题

### Q: 如何同时启用多个 Player 进行合作？
A: 修改 `PlayerManager` 的逻辑，改为启用多个 Player 而不是禁用其他。

### Q: 2D 小人无法移动？
A: 检查：
- Rigidbody2D 是否添加到了 GameObject
- Rigidbody2D Body Type 是否为 Dynamic
- Constraints 是否只冻结了 Rotation Z
- Collider 是否存在

### Q: 切换 Mode 时摄像头黑屏？
A: 检查：
- 新 Player 的 Camera 组件是否启用
- Camera 是否在正确的子对象上
- Tag 是否设置为 "MainCamera"

### Q: FPS3D Ship 控制不符合预期？
A: 注意：
- WS 是相对于镜头方向，不是世界方向
- 确保 Mouse.current 被正确初始化
- 检查 InputSystem 是否启用

---

## 📚 相关文件

| 文件 | 用途 |
|------|------|
| `PlayerManager.cs` | 管理三个 Player 对象 |
| `FPS3DWalkController.cs` | FPS3D 步行控制 |
| `FPS3DShipController.cs` | FPS3D 飞船控制 |
| `Plane2DCharacterController.cs` | 2D 小人控制 |
| `PlayerModeStateMachine.cs` | Mode 状态管理 |
| `ModeSwitchGuard.cs` | 切换条件守卫 |

---

**完成日期：** 2026年4月10日
**版本：** 2.0（全新 Player Mode 系统）
