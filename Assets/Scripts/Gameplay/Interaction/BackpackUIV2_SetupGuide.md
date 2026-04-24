# Backpack UI V2 使用指南

## 概述

新版背包UI系统包含以下核心组件：
- `BackpackUIControllerV2` - 主控制器
- `BackpackSlot` - 单个物品槽位组件
- `ItemDragHandler` - 物品拖拽管理器

## 核心改动

### 1. 动画系统
- 使用 Animator Controller 控制展开/收缩动画
- 通过 Bool 参数 `Expand` 切换状态
- 代码自动设置 `Expand` 参数

### 2. Slot 位置在 Editor 中预设
- Slot 不再动态生成，而是预先放置在面板中
- 在 `BackpackUIControllerV2` 的 `presetSlots` 列表中引用
- 位置、间距完全由 Editor 控制

### 3. BackpackSlot 设计
- Slot 本身可以是普通 GameObject（不需要 RectTransform）
- 子物体包含实际 UI：Icon、SelectionHighlight、QuantityText
- 支持自动查找子物体（`autoFindChildUI = true`）

### 4. 支持物品拖拽到 3D 场景
- 右键点击或双击 Slot 开始拖拽
- `ItemDragHandler` 管理全局拖拽状态
- 发布 `ItemPlacementRequestEvent` 事件供其他系统订阅

## 在 Unity Editor 中配置

### 步骤 1: 配置 Slot Prefab

1. 删除旧的 `Assets/Prefabs/UI/BackpackSlot_New.prefab`
2. 在 Unity 中：
   - 右键 `Assets/Prefabs/UI/BackpackSlot.prefab`
   - 选择 "Open" 或双击打开 Prefab 编辑模式
3. 在 Prefab 的根对象上：
   - 添加 `BackpackSlot` 组件
   - 确保 `Auto Find Child UI` 勾选（默认开启）

4. 创建子对象结构：
   ```
   BackpackSlot (根对象 - 普通GameObject)
   ├── Icon (Image)
   ├── SelectionHighlight (Image)
   └── QuantityText (TextMeshProUGUI)
   ```

> **注意**: `BackpackSlot` 组件会自动查找名为 `Icon`、`SelectionHighlight`、`QuantityText` 的子物体。如果改名了，需要手动拖拽引用。

### 步骤 2: 配置 BackpackPanel

1. 打开 `Assets/Prefabs/UI/BackpackPanelV2.prefab`
2. 在根对象添加以下组件：
   - `BackpackUIControllerV2`
   - `Animator`

3. 配置 Animator：
   - 拖入 Animation Controller 资源
   - 添加 Bool 参数 `Expand`

4. 配置 `BackpackUIControllerV2` 引用：
   | 字段 | 引用对象 |
   |------|----------|
   | Bubble Icon | BubbleIcon (RectTransform) |
   | Bubble Image | BubbleIcon 上的 Image |
   | Expanded Panel | ExpandedPanel (RectTransform) |
   | Panel Background | ExpandedPanel 上的 Image |
   | Item Count Text | InfoPanel 上的 TextMeshPro |
   | Bubble Icon Sprite | 收起状态的图标 Sprite |
   | Bubble Expanded Sprite | 展开状态的图标 Sprite |

### 步骤 3: 配置 Slot 位置和引用

1. 在 ExpandedPanel/SlotContainer 下创建/放置 BackpackSlot 对象
2. 手动调整每个 Slot 的位置和间距
3. 在 `BackpackUIControllerV2` 的 `Preset Slots` 列表中按顺序添加所有 Slot

> **重要**: presetSlots 列表必须填充，否则 UI 无法显示物品！

### 步骤 4: 创建 Animator Controller

1. 创建 `Assets/Animations/BackpackPanelController.controller`

2. 创建两个 Animation Clip：
   - `Expand.anim` - 展开动画
   - `Collapse.anim` - 收缩动画

3. 在 Animator 窗口中设置状态机：
   ```
   ┌─────────────────┐
   │   Collapsed     │  ← 初始状态 (Expand = false)
   └────────┬────────┘
            │ [Expand = true]
            ▼
   ┌─────────────────┐
   │   Expanded      │  ← Expand = true
   └─────────────────┘
   ```

4. 添加 Bool 参数：
   - 名称：`Expand`
   - 类型：Bool

5. 设置 Transition 条件：
   - Collapsed → Expanded：`Expand` = true
   - Expanded → Collapsed：`Expand` = false

### 步骤 5: 验证配置

配置完成后，`BackpackUIControllerV2` Inspector 应该显示：

```
References
├── Bubble Icon: BubbleIcon [RectTransform]
├── Bubble Image: Image
├── Expanded Panel: ExpandedPanel [RectTransform]
├── Panel Background: Image
├── Item Count Text: TextMeshProUGUI
├── Bubble Icon Sprite: [Sprite]
├── Bubble Expanded Sprite: [Sprite]
└── Scroll Indicator Up/Down: [Image]

Slot Configuration - Editor Preset
├── Preset Slots: [4个 BackpackSlot 引用]
└── [列表已填充]

Animation Triggers
├── Expand Trigger: "Expand"
└── Collapse Trigger: "Collapse"
```

## 组件详细说明

### BackpackSlot

```csharp
// 属性
public BackpackItem Item { get; }
public bool HasItem { get; }
public bool IsSelected { get; }

// 事件
public event Action<BackpackSlot> OnSlotClicked;
public event Action<BackpackSlot> OnSlotDragStarted;
public event Action<BackpackItem, Vector2> OnItemDragged;

// 方法
public void BindItem(BackpackItem item);  // 绑定物品
public void ClearSlot();                   // 清空槽位
public void SetSelected(bool selected);     // 设置选中状态
```

### ItemDragHandler

```csharp
// 属性
public static ItemDragHandler Instance { get; }
public bool IsDragging { get; }
public BackpackItem CurrentDragItem { get; }

// 方法
public void StartDrag(BackpackItem item, Vector2 startPosition);
public void EndDrag();
public void CancelDrag();
public bool IsOver3DScene();  // 判断是否在3D场景上释放
```

## 事件系统

### ItemPlacementRequestEvent
当物品被拖拽到3D场景并释放时发布。

```csharp
EventBus.Instance.Subscribe<ItemPlacementRequestEvent>(OnItemPlacement);

private void OnItemPlacement(ItemPlacementRequestEvent evt)
{
    Debug.Log($"Placing item: {evt.Item.itemName}");
    // 在这里实现实际的3D物体放置逻辑
}
```

### ItemDragStateChangedEvent
拖拽状态变更时发布。

```csharp
EventBus.Instance.Subscribe<ItemDragStateChangedEvent>(OnDragStateChanged);
```

## 后续扩展：3D 场景放置

当需要实现物品拖放到 3D 场景时：

1. 订阅 `ItemPlacementRequestEvent`
2. 使用 `evt.Item` 获取物品数据
3. 使用 `evt.WorldPosition` 获取世界坐标
4. 实例化对应的 3D Prefab
5. 调用 `BackpackService.Instance.RemoveItem()` 移除物品

```csharp
private void OnItemPlacement(ItemPlacementRequestEvent evt)
{
    if (evt.WorldPosition != Vector3.zero)
    {
        // 实例化3D物体
        var prefab = GetItemPrefab(evt.Item.itemId);
        if (prefab != null)
        {
            Instantiate(prefab, evt.WorldPosition, Quaternion.identity);
            BackpackService.Instance.RemoveItem(evt.Item.itemId);
        }
    }
}
```

## 注意事项

1. **Preset Slots 必须填充**: 这是最重要的配置，否则 UI 无法工作
2. **Slot 顺序**: `presetSlots` 列表中的顺序决定了物品显示的顺序
3. **最大槽位数**: 当前最大显示数量取决于预设的 Slot 数量
4. **相机引用**: 确保 Canvas 有正确的 render mode 和相机引用
5. **Animator 参数**: 参数名必须是 `Expand`（Bool 类型）
