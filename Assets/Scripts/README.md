# INTP Scripts 快速导览（当前版本）

> 这份文档是给“现在就要看懂项目”的版本。  
> 旧 README 里有不少 `WalkFPS/WalkSprint/VehicleFPS` 的历史描述，已经和当前实现不一致。

## 1) 先看结论：当前玩家系统怎么跑

当前是**三模式 + 独立 Player 实体切换**：

- `FPS3DWalk`：第一人称步行（鼠标视角 + WASD）
- `FPS3DShip`：第一人称飞船（WS 前后，AD 左右）
- `Plane2DCharacter`：2D 平面小人（WASD 上下左右）
- `UIInteraction`：交互模式（进入 UI 交互时限制玩家控制）

核心切换链路：

`GameFlowController` -> `ModeSwitchGuard` -> `PlayerModeStateMachine` -> `PlayerManager`

---

## 2) 关键文件（只看这些就能理解 80%）

### Core（状态与切换）
- `Assets/Scripts/Core/StateMachine/PlayerModeStateMachine.cs`
  - 定义 `PlayerModeState` 与玩家模式事件
- `Assets/Scripts/Core/Input/ModeSwitchGuard.cs`
  - 负责模式切换合法性判断 + 循环切换
- `Assets/Scripts/Core/PlayerManager.cs`
  - 根据模式启用/禁用对应 Player 对象
- `Assets/Scripts/Core/GameFlowController.cs`
  - 处理输入入口（Pause / SwitchMode / Backpack / Dream）
- `Assets/Scripts/Core/SystemBootstrap.cs`
  - 系统初始化，配置初始模式

### Gameplay/Player（三个控制器）
- `Assets/Scripts/Gameplay/Player/FPS3DWalkController.cs`
- `Assets/Scripts/Gameplay/Player/FPS3DShipController.cs`
- `Assets/Scripts/Gameplay/Player/Plane2DCharacterController.cs`

---

## 3) 启动时序（简版）

1. `SystemBootstrap` 初始化基础服务与状态机
2. `PlayerModeStateMachine` 设定初始 `PlayerModeState`
3. `PlayerManager` 监听模式变化并切换激活的 Player 对象
4. 运行时按键触发 `GameFlowController`，由 `ModeSwitchGuard` 决定能否切换

---

## 4) 现在项目里的“新旧并存”说明（重点）

下面这批脚本属于**旧玩家控制路径**，和当前三模式独立切换方案重叠：

- `Assets/Scripts/Gameplay/Player/PlayerLocomotionController.cs`
- `Assets/Scripts/Gameplay/Player/PlayerMovementInputHandler.cs`
- `Assets/Scripts/Gameplay/Player/VehiclePilotController.cs`

如果你决定只保留新方案，通常会把这三份作为清理候选。  
但是否可删还要看场景/Prefab 是否仍挂载它们。

---

## 5) 当前推荐的最小场景配置

### 必备对象
- 一个挂载 `SystemBootstrap` 的对象
- 一个挂载 `GameFlowController` + `PlayerInput` 的对象
- 一个挂载 `PlayerManager` 的对象
- 三个 Player 实体（分别挂对应控制器）

### PlayerManager 需要能找到
- `FPS3DWalkController` 对应对象
- `FPS3DShipController` 对应对象
- `Plane2DCharacterController` 对应对象

---

## 6) 输入约定（以当前代码为准）

`GameFlowController` 读取这些 Action 名称：
- `Pause`
- `SwitchMode`
- `OpenBackpack`
- `DreamHold`

请确保你的 Input Actions 里存在这些命名。

---

## 7) 推荐阅读顺序（防迷路）

1. `Assets/Scripts/Core/StateMachine/PlayerModeStateMachine.cs`
2. `Assets/Scripts/Core/PlayerManager.cs`
3. `Assets/Scripts/Core/Input/ModeSwitchGuard.cs`
4. `Assets/Scripts/Core/GameFlowController.cs`
5. 三个 Player 控制器

如果要看更完整说明，再看：
- `README_PLAYER_MODE.md`
- `IMPLEMENTATION_ROADMAP.md`

---

## 8) 版本备注

- 本文件描述的是**当前三模式实现**
- 历史文档（如 `plan.mb`）包含旧术语，仅作参考，不作为当前实现真值
