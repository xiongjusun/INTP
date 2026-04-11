# 🎮 Player Mode 系统 - 快速参考卡

## 📊 三种 Mode 对比

| 功能 | FPS3D 步行 | FPS3D 飞船 | 2D 小人 |
|------|----------|----------|--------|
| **枚举值** | `FPS3DWalk` | `FPS3DShip` | `Plane2DCharacter` |
| **镜头** | 第一人称FPS | 第一人称FPS | 俯视图/侧视图 |
| **鼠标** | 控制视角 | 控制视角 | 不需要 |
| **W按键** | 向前走 | 朝镜头前进 | 向上/前移 |
| **S按键** | 向后走 | 朝镜头后退 | 向下/后移 |
| **A按键** | 向左走 | 向左移动 | 向左移动 |
| **D按键** | 向右走 | 向右移动 | 向右移动 |
| **Shift** | 冲刺 | 无 | 冲刺 |
| **Space** | 跳跃 | 上升 | 无 |
| **Ctrl** | 下蹲 | 下降 | 无 |
| **坐标系** | 3D世界 | 3D世界 | 2D平面 |
| **应用** | 探索环境 | 驾驶飞行器 | 2D解谜/战斗 |

---

## ⌨️ 按键一览表

### 🎯 FPS3D 步行模式 (FPS3DWalk)
```
W/A/S/D + 鼠标 → 移动和视角
Shift + W/A/S/D → 冲刺移动
```

### 🚁 FPS3D 飞船模式 (FPS3DShip)
```
W/S + 鼠标 → 前后移动（相对镜头）
A/D → 左右平移
Space → 上升
Ctrl → 下降
鼠标 → 视角控制
```

### 🧑 2D 小人模式 (Plane2DCharacter)
```
W/A/S/D → 上下左右移动
Shift + W/A/S/D → 冲刺
```

---

## 🔧 组件映射

```
PlayerModeState              ↔  GameObject          ↔  Controller
─────────────────────────────────────────────────────────────────
FPS3DWalk                    PlayerFPS3DWalk        FPS3DWalkController
FPS3DShip                    PlayerFPS3DShip        FPS3DShipController
Plane2DCharacter             Player2DCharacter      Plane2DCharacterController
```

---

## 🎮 Mode 切换

**快速切换（循环）：**
```
按 Tab → 下一个 Mode
FPS3DWalk → FPS3DShip → Plane2DCharacter → FPS3DWalk (循环)
```

**代码切换：**
```csharp
// 循环切换
ModeSwitchGuard guard = FindObjectOfType<ModeSwitchGuard>();
guard.TryTogglePlayerMode();

// 直接切换
PlayerModeStateMachine sm = FindObjectOfType<PlayerModeStateMachine>();
sm.TryTransitionMode(PlayerModeState.FPS3DShip);
```

---

## 📋 设置清单

- [ ] 创建三个 Player GameObject
  - [ ] `PlayerFPS3DWalk` + `FPS3DWalkController`
  - [ ] `PlayerFPS3DShip` + `FPS3DShipController`
  - [ ] `Player2DCharacter` + `Plane2DCharacterController`

- [ ] 配置 `PlayerManager`
  - [ ] 分配三个 Prefab

- [ ] 在 `SystemBootstrap` 中设置初始 Mode
  - [ ] 选择 `FPS3DWalk` / `FPS3DShip` / `Plane2DCharacter`

- [ ] 添加所需的组件
  - [ ] `CharacterController` (FPS3D 两个)
  - [ ] `Rigidbody2D` (2D 小人)
  - [ ] `Animator` (可选，用于动画)

---

## 💡 常用代码片段

### 监听 Mode 变化
```csharp
PlayerModeStateMachine sm = FindObjectOfType<PlayerModeStateMachine>();
sm.OnModeChanged += (newMode, oldMode) =>
{
    Debug.Log($"Mode: {oldMode} → {newMode}");
};
```

### 检查当前 Mode
```csharp
if (sm.IsFPS3DWalkMode)
    Debug.Log("In FPS3D Walk");
else if (sm.IsFPS3DShipMode)
    Debug.Log("In FPS3D Ship");
else if (sm.IsPlane2DCharacterMode)
    Debug.Log("In 2D Character");
```

### 进入 UI 交互模式
```csharp
InteractionStateMachine interaction = FindObjectOfType<InteractionStateMachine>();
interaction.TransitionTo(InteractionState.UIInteraction);
// 禁用玩家控制
```

### 获取当前 Player
```csharp
PlayerManager manager = FindObjectOfType<PlayerManager>();
GameObject activePlayer = manager.GetActivePlayer();
```

---

## 🚦 状态流

```
SystemBootstrap
    ↓
PlayerModeStateMachine (初始化 Mode)
    ↓
PlayerManager (激活对应 Player)
    ↓
相应 Controller 接管控制
    ↓
按 Tab / 代码调用 TryTogglePlayerMode()
    ↓
PlayerModeStateMachine (发布事件)
    ↓
PlayerManager (切换 Player)
    ↓
新的 Controller 接管控制
```

---

## 🎯 使用场景

**何时使用 FPS3D 步行：**
- ✓ 室内探索
- ✓ 与NPC交互
- ✓ 解谜游戏
- ✓ 日常环境

**何时使用 FPS3D 飞船：**
- ✓ 驾驶飞行器
- ✓ 空中战斗
- ✓ 3D空间探索
- ✓ 需要垂直移动

**何时使用 2D 小人：**
- ✓ 2D地牢
- ✓ 俯视图战斗
- ✓ 2D解谜
- ✓ 像素艺术风格

**何时使用 UI 交互：**
- ✓ 打开菜单
- ✓ 对话交互
- ✓ 物品选择
- ✓ 设置面板

---

## 📁 核心文件位置

```
Assets/Scripts/
├── Core/
│   ├── PlayerManager.cs                    ← ⭐ Player 管理
│   └── StateMachine/
│       └── PlayerModeStateMachine.cs        ← ⭐ Mode 状态机
│
└── Gameplay/Player/
    ├── FPS3DWalkController.cs              ← ⭐ 步行控制
    ├── FPS3DShipController.cs              ← ⭐ 飞船控制
    └── Plane2DCharacterController.cs       ← ⭐ 2D 小人控制
```

---

## ✅ 检查清单

快速检查系统是否配置正确：

```
初始化：
  [ ] PlayerManager 已创建且配置
  [ ] 三个 Player GameObject 已创建
  [ ] SystemBootstrap 已设置初始 Mode

FPS3D 步行：
  [ ] 有 CharacterController
  [ ] 有 FPS3DWalkController
  [ ] 有 Camera 子对象
  [ ] 能用鼠标控制视角
  [ ] 能用 WASD 移动

FPS3D 飞船：
  [ ] 有 CharacterController
  [ ] 有 FPS3DShipController
  [ ] 有 Camera 子对象
  [ ] WS 相对镜头移动
  [ ] Space/Ctrl 能上升/下降

2D 小人：
  [ ] 有 Rigidbody2D
  [ ] 有 Plane2DCharacterController
  [ ] 能用 WASD 移动
  [ ] 有 Animator（可选）

切换：
  [ ] 能按 Tab 切换 Mode
  [ ] Console 显示正确的 Mode
  [ ] Player 对象正确地启用/禁用
```

---

**版本：** 2.0  
**更新日期：** 2026年4月10日
