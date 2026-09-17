# 最后的炼金术师 · 首个可玩垂直切片

Unity 6000.6.0f1 / Windows / 单机。模板 `SampleScene` 保持不动，游戏内容独立放在 `Assets/LastAlchemist`。

## 直接试玩

- 运行 `Builds/LastAlchemist/LastAlchemist.exe`。发布时需要复制整个 `LastAlchemist` 文件夹。
- 或在 Unity 中通过菜单「炼金原型 → 打开垂直切片」，打开 `Assets/LastAlchemist/Scenes/AlchemyPrototype.unity` 后点击 Play。
- 建议 Game 视图使用 1440×900；更小的窗口会等比缩放并留黑边。

## 操作

- `WASD` / 方向键：在横向大地图中移动；镜头会平滑跟随角色。
- `E`：与最近的材料或设备交互。
- `1—6`：白天选择炼金配方；夜间手动使用对应药剂。
- `Esc`：暂停或继续。
- 界面上的配方、自动化、交互和药剂按钮均可用鼠标操作。

## 一局内容

一局共 5 天，每个白昼 75 秒，也可以在右侧点击「准备完成 · 提前入夜」。第五夜最后会出现灰潮之王。

- 4 种材料：史莱姆液、火焰草、月光菇、魔晶。
- 6 种配方：火焰、治疗、寒霜、腐蚀、爆裂、贤者灵药。
- 3 个连续区域：荧光温室、炼金工坊、灰潮城门；总宽度约为单屏的 3 倍。
- 8 个交互点：4 个资源点、炼金炉、商摊、自动化控制台、药剂炮台，沿地图逐步出现。
- 3 种普通敌人和 1 个最终 Boss。
- 15 项炼金洞见；每次守住夜晚后三选一。
- 5 种日间事件，会根据本局生产和战斗表现变化。

### 经营与战斗的连接

炮台拥有免费的低伤害基础魔弹。开启「炮台装填」后，它会优先消耗爆裂、火焰、腐蚀和寒霜药剂，因此出售多少、留下多少就是主要策略取舍。治疗药剂能修复城门，贤者灵药能暂时让城门受到的伤害减半。

### 自动化

- 48 金币解锁自动炼制：连续生产当前选中的配方。
- 72 金币解锁自动出售：每 4 秒出售各类药剂中超过 3 瓶的部分，保留基础战斗库存。
- 自动炼制、自动出售、炮台装填都能随时开关；夜间生产不会停止。

## 存档

每 5 秒、交互后、切换阶段、失去焦点和退出时写入 `Application.persistentDataPath/last-alchemist-v3.json`。采用临时文件替换和 `.bak` 备份；损坏文件会保留诊断副本。烟雾测试使用独立状态，不读写玩家存档。v3 使用新的大地图坐标，角色从最左侧温室出生。

## 项目结构

- `AlchemyState.cs`：独立的生产、经济、昼夜、敌人、炮台、洞见和事件模拟。
- `AlchemySave.cs`：版本化 JSON 存档与备份恢复。
- `PixelWorkshop.cs`：原创代码生成角色、材料、设备和敌人像素精灵，并拼接三段手绘地图背景。
- `AlchemyPrototype.cs`：场景装配、输入、交互、镜头跟随、动画和中文 IMGUI 界面。
- `AlchemySmokeTest.cs`：Development Build 自动操作、昼夜闭环和截图检查。
- `Editor/PrototypeTools.cs`：核心逻辑自检和 Windows 构建入口。
- `Resources/Art/alchemy-workshop-left.png`、`alchemy-workshop-bg.png`、`alchemy-workshop-right.png`：依据给定氛围参考生成的原创无文字连续地图背景。
- `Resources/Art/slime-vat-full.png`、`slime-vat-depleted.png`：透明背景史莱姆萃取槽满/空状态；采集时切换模型并播放挤压、飞溅动画。
- `Resources/Art/fire-herb-rack-full.png`、`fire-herb-rack-depleted.png`：三层恒温火焰草种植架及采集后状态。
- `Resources/Art/moon-mushroom-bed-full.png`：带弯月遮光棚、月灯、冷雾和孢子收集柱的横向月光菌床；冷却期通过熄光变暗表现。
- `Resources/Art/Icons/`：金币与四类原料的原创透明手绘图标；顶部资源栏直接加载这些资产。

界面采用全宽场景构图。原来的右侧实心侧栏已拆成独立半透明浮层，今日目标移入场景左上角；世界交互标签只有进入镜头后才显示，避免提前暴露远处设施并减少遮挡。

## 验证

- 菜单「炼金原型 → 运行核心逻辑自检」覆盖 6 个配方、经济、自动化、完整夜袭、洞见、事件、失败条件、帧率独立性和损坏存档恢复。
- 菜单「炼金原型 → 构建 Windows 可玩版」会先运行核心自检，再生成 Windows Development Build。
- 独立版本追加 `--alchemy-smoke-test -screen-width 1440 -screen-height 900 -screen-fullscreen 0` 会自动完成采集、炼制、解锁、夜袭、洞见与多分辨率截图检查，结果写入程序旁的 `SmokeResults`。

当前版本是验证核心乐趣的垂直切片。正式制作下一阶段应将运行时对象拆成 Prefab，将配方和洞见迁移到 ScriptableObject，并把 IMGUI 迁移到 UI Toolkit 或 UGUI。
