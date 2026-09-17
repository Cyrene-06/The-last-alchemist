# Contributing to The Last Alchemist

感谢你参与《最后的炼金术师》。本项目目前处于可玩垂直切片阶段，贡献应优先保持核心循环清晰、可验证，并避免把 Unity 生成缓存提交到仓库。

## Development environment

- Unity `6000.6.0f1`
- Windows 10/11
- 通过 Unity Package Manager 恢复 `Packages/manifest.json` 中的依赖
- 推荐 Game 视图：`1440 × 900`

打开 `Assets/LastAlchemist/Scenes/AlchemyPrototype.unity`，或使用菜单 **炼金原型 → 打开垂直切片**。

## Branches and commits

从最新的 `main` 创建短生命周期分支：

```bash
git switch main
git pull --ff-only
git switch -c feat/short-description
```

建议使用以下提交前缀：

- `feat:` 新玩法或系统
- `fix:` 缺陷修复
- `art:` 美术与动画调整
- `docs:` 文档改进
- `test:` 自动检查或测试
- `refactor:` 不改变行为的结构调整
- `chore:` 构建、工具或仓库维护

联合完成的提交应使用 GitHub 标准 trailer，并确保邮箱关联到对应 GitHub 账号：

```text
Co-Authored-By: Name <verified-email@example.com>
```

## Project rules

- 保留所有 Unity `.meta` 文件；移动资产时同时移动对应 `.meta`。
- 不提交 `Library/`、`Temp/`、`Logs/`、`Builds/`、`UserSettings/` 或 IDE 生成文件。
- 不直接修改生成的 `.csproj`、`.sln` 或 `Library` 数据。
- 新资源应放在 `Assets/LastAlchemist` 下的对应目录中。
- 新增游戏状态优先放入 `AlchemyState`，保持核心模拟与 Unity 表现层分离。
- 任何存档结构变更都应提高 `AlchemyState.SaveVersion` 并说明迁移影响。
- 美术资产应保持哥特炼金工坊的黄铜、木石、蓝紫夜色视觉语言，同时让设备轮廓符合其功能含义。

## Validation before a pull request

在提交 PR 前完成以下检查：

1. 在 Unity 中运行 **炼金原型 → 运行核心逻辑自检**。
2. 确认 45 项核心检查全部通过。
3. 如改动运行时行为，执行 Windows Development Build 烟雾测试：

   ```text
   --alchemy-smoke-test -screen-width 1440 -screen-height 900 -screen-fullscreen 0
   ```

4. 检查 `git status`，确保未包含缓存、日志、存档或本机构建产物。
5. 对地图、UI 或美术改动附上更新后的截图。

## Pull requests

PR 应保持单一主题，并包含：

- 改动动机与结果
- 主要文件或系统
- 实际执行的验证
- 涉及视觉变化时的截图
- 已知限制或后续工作
- 真实的联合作者信息（如适用）

请不要使用空提交或无意义改动制造活动记录。联合署名应反映真实参与，确保项目历史可追溯。

