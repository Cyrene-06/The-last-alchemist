using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastAlchemist
{
    public sealed class AlchemyPrototype : MonoBehaviour
    {
        sealed class Station
        {
            public string name, subtitle;
            public Vector2 position;
            public int kind, nodeIndex;
            public SpriteRenderer renderer;
        }

        static readonly string[] UpgradeNames = {
            "高效蒸馏", "丰饶采集", "爆燃催化", "黄金订单", "强化城门",
            "炼金回响", "急速炮台", "黎明护盾", "永冻配方", "超量装药",
            "生命炼成", "赏金契约", "精明分拣", "活体藤蔓", "大师标记"
        };
        static readonly string[] UpgradeDescriptions = {
            "全部炼制时间 -22%", "每次采集额外获得 1 份材料", "火焰药剂额外造成 12 点伤害", "所有药剂售价 +25%", "城门上限与当前耐久 +30",
            "每完成第 4 次炼制时额外产出 1 瓶", "炮台射速提高并强化基础弹", "每个夜晚开始时修复 20 点城门", "寒霜减速持续时间延长", "爆裂药剂额外造成 20 点伤害",
            "治疗药剂的修复量 +15", "每名敌人额外掉落 3 金币", "自动出售保留更多战斗库存", "资源点恢复时间缩短", "全部药剂售价再提高 20%"
        };
        static readonly string[] EventDescriptions = {
            "", "温室丰收：火焰草 +3，月光菇 +2", "史莱姆雨：史莱姆液 +5", "矿脉共鸣：魔晶 +4",
            "游商到访：获得 18 金币预付款", "守卫修缮：城门恢复 35 点耐久"
        };
        static readonly Color[] PotionColors = {
            PixelWorkshop.C("e5673f"), PixelWorkshop.C("6fd5a7"), PixelWorkshop.C("67bde8"),
            PixelWorkshop.C("9bd45c"), PixelWorkshop.C("d586db"), PixelWorkshop.C("edc464")
        };

        readonly Station[] stations = new Station[8];
        readonly SpriteRenderer[] enemies = new SpriteRenderer[4];
        readonly Texture2D[] resourceIcons = new Texture2D[5];
        readonly SpriteRenderer[] slimeDroplets = new SpriteRenderer[5];
        Sprite slimeVatFull, slimeVatDepleted;
        Sprite fireHerbRackFull, fireHerbRackDepleted, moonMushroomBedFull;
        float slimeHarvestFx, slimeRefillFx;
        float cameraVelocity;
        bool slimeWasReady;
        AlchemyState state;
        PixelWorkshop art;
        SpriteRenderer player;
        Camera worldCamera;
        Font font;
        GUIStyle text, bold, small, tiny, button, heading, centered;
        Texture2D panelTexture;
        bool paused, confirmingReset, initialized;
        string toast = "黎明将至。采集材料，炼制第一批火焰药剂。";
        float toastTimer = 9, saveTimer, elapsed;
        int nearest = -1;
        Vector2 movement;
        const float Reach = 1.55f;
        const float MapMinX = -33.5f, MapMaxX = 33.5f;
        const float LogicalWidth = 1440, LogicalHeight = 900;
        readonly Color cream = PixelWorkshop.C("eee7cf"), muted = PixelWorkshop.C("a9aaa0"), gold = PixelWorkshop.C("e5bb73");
        public static bool IsSmokeTest => Array.IndexOf(Environment.GetCommandLineArgs(), "--alchemy-smoke-test") >= 0;
        public AlchemyState State => state;
        public Vector2 PlayerPosition => player.transform.position;
        public bool SlimeHarvestAnimating => slimeHarvestFx > 0 && slimeVatDepleted != null && stations[0].renderer.sprite == slimeVatDepleted;
        public bool StyledPlantStationsActive => fireHerbRackFull != null && moonMushroomBedFull != null &&
            (stations[1].renderer.sprite == fireHerbRackFull || stations[1].renderer.sprite == fireHerbRackDepleted) &&
            stations[2].renderer.sprite == moonMushroomBedFull;

        void Awake()
        {
            string notice = "";
            state = IsSmokeTest ? new AlchemyState() : AlchemySave.Load(out notice);
            if (!string.IsNullOrEmpty(notice)) toast = notice;
            Application.targetFrameRate = 60;
            if (IsSmokeTest) Application.runInBackground = true;
            worldCamera = Camera.main;
            if (!worldCamera)
            {
                var cameraObject = new GameObject("Workshop Camera");
                cameraObject.tag = "MainCamera";
                worldCamera = cameraObject.AddComponent<Camera>();
            }
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 6.15f;
            worldCamera.transform.position = new Vector3(0, 0, -10);
            worldCamera.transform.rotation = Quaternion.identity;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = PixelWorkshop.C("121b22");
            SetCameraViewport();

            art = new PixelWorkshop();
            art.BuildGround(transform);
            AddStation(0, "史莱姆萃取槽", "+史莱姆液", "slime", new Vector2(-28f, -2.45f), 0, 0);
            AddStation(1, "火焰草架", "+火焰草", "herb", new Vector2(-21.5f, -2.25f), 1, 1);
            AddStation(2, "月光菌床", "+月光菇", "mushroom", new Vector2(-15f, -2.25f), 2, 2);
            AddStation(3, "魔晶矿箱", "+魔晶", "crystal", new Vector2(-8.5f, -2.25f), 3, 3);
            AddStation(4, "万象炼金炉", "炼制当前配方", "cauldron", new Vector2(-1.5f, -1.95f), 4, -1);
            AddStation(5, "夜鸦商摊", "出售全部成品", "market", new Vector2(5f, -2.05f), 5, -1);
            AddStation(6, "齿轮控制台", "升级与自动化", "upgrade", new Vector2(11.5f, -2f), 6, -1);
            AddStation(7, "药剂炮台", "切换自动装填", "turret", new Vector2(21f, -1.9f), 7, -1);
            player = art.Actor(transform, "player", new Vector2(Mathf.Clamp(state.playerX, MapMinX, MapMaxX), Mathf.Clamp(state.playerY, -4.45f, -.65f)), 1.4f);
            player.name = "Alchemist Player";
            enemies[0] = art.Actor(transform, "wretch", new Vector2(25.5f, -2.15f), 1.45f);
            enemies[1] = art.Actor(transform, "brute", new Vector2(25.5f, -2.15f), 1.65f);
            enemies[2] = art.Actor(transform, "wretch", new Vector2(25.5f, -2.15f), 1.7f);
            enemies[3] = art.Actor(transform, "boss", new Vector2(25.35f, -2.05f), 2.15f);
            for (int i = 0; i < enemies.Length; i++) { enemies[i].name = "Night enemy " + i; enemies[i].gameObject.SetActive(false); }
            string[] iconPaths = { "Art/Icons/icon-coin", "Art/Icons/icon-slime", "Art/Icons/icon-fire-herb", "Art/Icons/icon-moon-mushroom", "Art/Icons/icon-crystal" };
            for (int i = 0; i < resourceIcons.Length; i++) resourceIcons[i] = Resources.Load<Texture2D>(iconPaths[i]);
            slimeVatFull = art.CreateResourceSprite(Resources.Load<Texture2D>("Art/slime-vat-full"), 2.7f, new Vector2(.5f, .1f));
            slimeVatDepleted = art.CreateResourceSprite(Resources.Load<Texture2D>("Art/slime-vat-depleted"), 2.7f, new Vector2(.5f, .1f));
            fireHerbRackFull = art.CreateResourceSprite(Resources.Load<Texture2D>("Art/fire-herb-rack-full"), 3.15f, new Vector2(.5f, .075f));
            fireHerbRackDepleted = art.CreateResourceSprite(Resources.Load<Texture2D>("Art/fire-herb-rack-depleted"), 3.15f, new Vector2(.5f, .075f));
            moonMushroomBedFull = art.CreateResourceSprite(Resources.Load<Texture2D>("Art/moon-mushroom-bed-full"), 2.45f, new Vector2(.5f, .075f));
            if (slimeVatFull && slimeVatDepleted)
            {
                stations[0].renderer.sprite = state.nodeCooldowns[0] > 0 ? slimeVatDepleted : slimeVatFull;
                stations[0].renderer.transform.localScale = Vector3.one;
            }
            if (fireHerbRackFull && fireHerbRackDepleted)
            {
                stations[1].renderer.sprite = state.nodeCooldowns[1] > 0 ? fireHerbRackDepleted : fireHerbRackFull;
                stations[1].renderer.transform.localScale = Vector3.one;
            }
            if (moonMushroomBedFull)
            {
                stations[2].renderer.sprite = moonMushroomBedFull;
                stations[2].renderer.transform.localScale = Vector3.one;
            }
            for (int i = 0; i < slimeDroplets.Length; i++)
            {
                slimeDroplets[i] = art.Block(transform, "Slime harvest droplet", stations[0].position, new Vector2(.12f, .15f), "64efc7", 190 + i);
                slimeDroplets[i].gameObject.SetActive(false);
            }
            slimeWasReady = state.nodeCooldowns[0] <= 0;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Microsoft YaHei UI", "SimHei", "Arial" }, 20);
            initialized = true;
            UpdateCameraFollow(0, true);
            FindNearest();
            RefreshEnemyVisual();
        }

        void AddStation(int index, string title, string subtitle, string sprite, Vector2 position, int kind, int node)
        {
            stations[index] = new Station { name = title, subtitle = subtitle, position = position, kind = kind, nodeIndex = node,
                renderer = art.Actor(transform, sprite, position, kind < 4 ? 1.35f : 1.65f) };
        }

        void SetCameraViewport()
        {
            float scale = Mathf.Min(Screen.width / LogicalWidth, Screen.height / LogicalHeight);
            float ox = (Screen.width - LogicalWidth * scale) / 2;
            float oy = (Screen.height - LogicalHeight * scale) / 2;
            worldCamera.rect = new Rect((ox + 18 * scale) / Screen.width, (oy + 127 * scale) / Screen.height,
                1404 * scale / Screen.width, 600 * scale / Screen.height);
        }

        void Update()
        {
            if (!initialized) return;
            SetCameraViewport();
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            { if (confirmingReset) confirmingReset = false; else paused = !paused; }
            if (paused || confirmingReset) return;
            if (state.Phase == RunPhase.Insight || state.Phase == RunPhase.Victory || state.Phase == RunPhase.Defeat) return;

            elapsed += Time.deltaTime;
            toastTimer -= Time.deltaTime;
            movement = Vector2.zero;
            if (keyboard != null)
            {
                movement.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                             (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
                movement.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                             (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0);
            }
            MovePlayer(movement, Time.deltaTime);
            UpdateCameraFollow(Time.deltaTime);
            FindNearest();
            for (int i = 0; i < 4; i++)
            {
                state.nodeCooldowns[i] = Mathf.Max(0, state.nodeCooldowns[i] - Time.deltaTime);
                if (i == 1 && fireHerbRackFull && fireHerbRackDepleted)
                {
                    stations[i].renderer.sprite = state.nodeCooldowns[i] > 0 ? fireHerbRackDepleted : fireHerbRackFull;
                    stations[i].renderer.color = Color.white;
                }
                else if (i > 0) stations[i].renderer.color = state.nodeCooldowns[i] > 0 ? new Color(.48f, .53f, .54f) : Color.white;
            }
            bool slimeReady = state.nodeCooldowns[0] <= 0;
            if (slimeReady && !slimeWasReady) slimeRefillFx = .7f;
            slimeWasReady = slimeReady;
            UpdateSlimeVatVisual(Time.deltaTime);

            int brewed = state.totalBrewed;
            int kills = state.totalKills;
            RunPhase oldPhase = state.Phase;
            state.Tick(Time.deltaTime);
            if (state.totalBrewed > brewed) Notify("炼制完成 · " + AlchemyState.RecipeName(state.brewRecipe) + " +" + (state.totalBrewed - brewed));
            if (state.totalKills > kills) Notify("敌人被击退 · 获得战利金币");
            if (state.Phase != oldPhase) OnPhaseChanged(state.Phase);

            if (keyboard != null)
            {
                if (keyboard.eKey.wasPressedThisFrame) Interact();
                Key[] numberKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6 };
                for (int i = 0; i < numberKeys.Length; i++)
                    if (keyboard[numberKeys[i]].wasPressedThisFrame) PotionAction(i);
            }
            saveTimer += Time.deltaTime;
            if (saveTimer >= 5) Save();
            float pulse = state.brewing ? 1f + Mathf.Sin(elapsed * 5f) * .03f : 1f;
            stations[4].renderer.transform.localScale = Vector3.one * 1.65f * pulse;
            RefreshEnemyVisual();
        }

        void OnPhaseChanged(RunPhase phase)
        {
            if (phase == RunPhase.Night) Notify("灰潮来袭！炮台会自动消耗攻击药剂，也可用数字键手动投掷。", 10);
            else if (phase == RunPhase.Insight) Notify("你从灰潮中获得了一次新的炼金洞见。", 8);
            else if (phase == RunPhase.Victory) Notify("贤者熔炉稳定运转，灰潮之王已经倒下。", 12);
            else if (phase == RunPhase.Defeat) Notify("城门被攻破了，但知识会留给下一次实验。", 12);
            Save();
        }

        void RefreshEnemyVisual()
        {
            if (enemies[0] == null) return;
            for (int i = 0; i < enemies.Length; i++) enemies[i].gameObject.SetActive(state.EnemyAlive && i == state.enemyType);
            if (state.EnemyAlive)
            {
                var active = enemies[state.enemyType];
                float lunge = Mathf.Sin(elapsed * (state.enemyType == 2 ? 8 : 5)) * .12f;
                active.transform.position = new Vector2(25.5f + lunge, -2.15f);
                active.color = state.frostTimer > 0 ? new Color(.58f, .82f, 1f) : Color.white;
            }
        }

        void UpdateSlimeVatVisual(float delta)
        {
            if (!slimeVatFull || !slimeVatDepleted) return;
            bool ready = state.nodeCooldowns[0] <= 0;
            var vat = stations[0].renderer;
            vat.sprite = ready ? slimeVatFull : slimeVatDepleted;
            float xScale = 1f, yScale = 1f, rotation = 0;
            vat.color = Color.white;

            if (slimeHarvestFx > 0)
            {
                slimeHarvestFx = Mathf.Max(0, slimeHarvestFx - delta);
                float t = 1f - slimeHarvestFx / .9f;
                float impact = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
                xScale += impact * .14f;
                yScale -= impact * .09f;
                rotation = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t) * 2.4f;
                vat.color = Color.Lerp(new Color(.55f, 1f, .9f), Color.white, t);
                for (int i = 0; i < slimeDroplets.Length; i++)
                {
                    var droplet = slimeDroplets[i];
                    droplet.gameObject.SetActive(true);
                    float spread = (i - 2) * .22f;
                    droplet.transform.position = stations[0].position + new Vector2(spread + Mathf.Sin(t * 5f + i) * .09f,
                        1.05f + Mathf.Sin(t * Mathf.PI) * (.55f + i * .06f));
                    float size = Mathf.Max(.02f, .15f * (1f - t));
                    droplet.transform.localScale = new Vector3(size * .75f, size, 1);
                    droplet.color = new Color(.39f, .94f, .78f, 1f - t);
                }
            }
            else
            {
                foreach (var droplet in slimeDroplets) droplet.gameObject.SetActive(false);
                if (slimeRefillFx > 0)
                {
                    slimeRefillFx = Mathf.Max(0, slimeRefillFx - delta);
                    float t = 1f - slimeRefillFx / .7f;
                    float pop = Mathf.Sin(t * Mathf.PI);
                    xScale += pop * .08f;
                    yScale += pop * .12f;
                    vat.color = Color.Lerp(new Color(.52f, 1f, .87f), Color.white, t);
                }
                else if (ready)
                {
                    float breath = Mathf.Sin(elapsed * 2.1f) * .012f;
                    xScale += breath;
                    yScale -= breath * .65f;
                }
            }
            vat.transform.localScale = new Vector3(xScale, yScale, 1);
            vat.transform.localRotation = Quaternion.Euler(0, 0, rotation);
        }

        public void MovePlayer(Vector2 direction, float delta)
        {
            if (!player) return;
            var current = (Vector2)player.transform.position;
            var velocity = Vector2.ClampMagnitude(direction, 1) * (4.1f * delta);
            var next = current + new Vector2(velocity.x, 0);
            if (!Blocked(next)) current = next;
            next = current + new Vector2(0, velocity.y);
            if (!Blocked(next)) current = next;
            current.x = Mathf.Clamp(current.x, MapMinX, MapMaxX);
            current.y = Mathf.Clamp(current.y, -4.45f, -.65f);
            player.transform.position = current;
            player.sortingOrder = 100 - Mathf.RoundToInt(current.y * 10);
            if (direction.x != 0) player.flipX = direction.x < 0;
            float bob = direction.sqrMagnitude > 0 ? Mathf.Sin(elapsed * 16) * .035f : 0;
            player.transform.localScale = new Vector3(1.4f, 1.4f + bob, 1);
        }

        void UpdateCameraFollow(float delta, bool immediate = false)
        {
            if (!worldCamera || !player) return;
            float viewportAspect = worldCamera.pixelHeight > 0 ? worldCamera.pixelWidth / (float)worldCamera.pixelHeight : 1404f / 600f;
            float halfWidth = worldCamera.orthographicSize * viewportAspect;
            float target = Mathf.Clamp(player.transform.position.x, MapMinX + halfWidth, MapMaxX - halfWidth);
            float x = immediate ? target : Mathf.SmoothDamp(worldCamera.transform.position.x, target, ref cameraVelocity, .18f, 100f, Mathf.Max(.0001f, delta));
            worldCamera.transform.position = new Vector3(x, 0, -10);
        }

        bool Blocked(Vector2 point)
        {
            for (int i = 4; i < stations.Length; i++)
                if (new Rect(stations[i].position.x - .62f, stations[i].position.y - .1f, 1.24f, .82f).Contains(point)) return true;
            return false;
        }

        void FindNearest()
        {
            nearest = -1;
            float best = Reach;
            for (int i = 0; i < stations.Length; i++)
            {
                float distance = Vector2.Distance(player.transform.position, stations[i].position);
                if (distance < best) { best = distance; nearest = i; }
            }
        }

        public void Interact()
        {
            if (paused || confirmingReset || state.Phase == RunPhase.Insight || state.Phase == RunPhase.Victory || state.Phase == RunPhase.Defeat) return;
            if (nearest < 0) { Notify("靠近资源或设备，再按 E 交互。"); return; }
            var station = stations[nearest];
            if (station.kind < 4)
            {
                if (state.nodeCooldowns[station.nodeIndex] > 0) { Notify("资源正在恢复，请稍等片刻。"); return; }
                MaterialKind material = (MaterialKind)station.kind;
                state.Gather(material);
                state.nodeCooldowns[station.nodeIndex] = state.ResourceCooldown;
                if (station.kind == 0)
                {
                    slimeHarvestFx = .9f;
                    slimeRefillFx = 0;
                    slimeWasReady = false;
                }
                Notify(station.subtitle + " +" + state.GatherAmount + " · " + state.ResourceCooldown.ToString("0.0") + " 秒后恢复");
            }
            else if (station.kind == 4)
            {
                if (state.brewing) Notify("炼金炉正在炼制 " + AlchemyState.RecipeName(state.brewRecipe) + "。 ");
                else if (state.TryStartBrew()) Notify("开始炼制 " + AlchemyState.RecipeName(state.selectedRecipe) + "。 ");
                else Notify("材料不足：" + AlchemyState.RecipeFormula(state.selectedRecipe));
            }
            else if (station.kind == 5)
            {
                int earned = state.SellAll();
                Notify(earned > 0 ? "夜鸦商人收走全部药剂 · +" + earned + " 金币" : "没有可出售的成品药剂。");
            }
            else if (station.kind == 6)
            {
                if (!state.automationUnlocked)
                    Notify(state.TryUnlockAutomation() ? "自动炼制已解锁！炼金炉会连续生产当前配方。" : "自动炼制需要 " + AlchemyState.AutomationPrice + " 金币。");
                else if (!state.autoSaleUnlocked)
                    Notify(state.TryUnlockAutoSale() ? "自动出售已解锁！每种药剂保留 3 瓶后自动出售。" : "自动出售需要 " + AlchemyState.AutoSalePrice + " 金币。");
                else
                {
                    state.autoEnabled = !state.autoEnabled;
                    Notify("自动炼制已" + (state.autoEnabled ? "开启。" : "暂停。"));
                }
            }
            else
            {
                state.autoDefenseEnabled = !state.autoDefenseEnabled;
                Notify(state.autoDefenseEnabled ? "炮台自动装填已开启，会优先使用爆裂与火焰药剂。" : "炮台只使用免费的基础魔弹，药剂库存将被保留。");
            }
            Save();
        }

        void PotionAction(int recipe)
        {
            if (state.Phase == RunPhase.Night) Notify(state.UsePotion(recipe));
            else if (state.Phase == RunPhase.Day)
            {
                if (state.SelectRecipe(recipe)) Notify("当前配方：" + AlchemyState.RecipeName(recipe) + " · " + AlchemyState.RecipeFormula(recipe));
                else Notify("正在炼制，完成后才能切换配方。");
            }
            Save();
        }

        void Notify(string message, float duration = 5) { toast = message; toastTimer = duration; }

        bool Save()
        {
            if (state == null || !player || IsSmokeTest) return false;
            state.playerX = player.transform.position.x;
            state.playerY = player.transform.position.y;
            bool saved = AlchemySave.Write(state);
            if (!saved) Notify("存档写入失败，请检查磁盘空间与目录权限。", 8);
            saveTimer = 0;
            return saved;
        }

        void RestartRun()
        {
            state = new AlchemyState();
            player.transform.position = new Vector2(state.playerX, state.playerY);
            UpdateCameraFollow(0, true);
            confirmingReset = paused = false;
            FindNearest();
            RefreshEnemyVisual();
            Notify("新工坊已就绪。在第一夜到来前准备药剂。", 9);
            Save();
        }

        void OnApplicationQuit() => Save();
        void OnApplicationFocus(bool focused) { if (!focused && initialized) Save(); }
        void OnApplicationPause(bool value) { if (value && initialized) Save(); }
        void OnDestroy() { if (art != null) art.Dispose(); if (font) Destroy(font); if (panelTexture) Destroy(panelTexture); }

        void InitGUI()
        {
            if (text != null) return;
            text = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, wordWrap = false, alignment = TextAnchor.MiddleLeft, normal = { textColor = cream } };
            bold = new GUIStyle(text) { fontSize = 21, fontStyle = FontStyle.Bold };
            small = new GUIStyle(text) { fontSize = 14, normal = { textColor = muted } };
            tiny = new GUIStyle(small) { fontSize = 12 };
            centered = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter };
            heading = new GUIStyle(text) { fontSize = 32, fontStyle = FontStyle.Bold };
            panelTexture = new Texture2D(1, 1); panelTexture.SetPixel(0, 0, PixelWorkshop.C("3a5149")); panelTexture.Apply();
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 15, alignment = TextAnchor.MiddleCenter,
                normal = { background = panelTexture, textColor = cream }, hover = { background = panelTexture, textColor = gold },
                active = { background = panelTexture, textColor = Color.white }, border = new RectOffset(0, 0, 0, 0), wordWrap = true };
        }

        void Box(Rect r, string hex) { Color old = GUI.color; GUI.color = PixelWorkshop.C(hex); GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        void BoxAlpha(Rect r, string hex, float alpha)
        {
            Color old = GUI.color;
            Color color = PixelWorkshop.C(hex); color.a = alpha; GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old;
        }
        void Label(float x, float y, float w, float h, string value, GUIStyle style = null) => GUI.Label(new Rect(x, y, w, h), value, style ?? text);
        void Progress(float x, float y, float w, float fraction, string color)
        { Box(new Rect(x, y, w, 6), "273732"); Box(new Rect(x, y, w * Mathf.Clamp01(fraction), 6), color); }

        void OnGUI()
        {
            if (!initialized) return;
            InitGUI();
            float scale = Mathf.Min(Screen.width / LogicalWidth, Screen.height / LogicalHeight);
            var offset = new Vector2((Screen.width - LogicalWidth * scale) / 2, (Screen.height - LogicalHeight * scale) / 2);
            var previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale);
            Box(new Rect(0, 0, 1440, 127), "0d1719");
            Box(new Rect(0, 727, 1440, 173), "0d1719");
            DrawHeader();
            DrawWorldLabels(scale, offset);
            DrawSidebar();
            DrawPotionBar();
            Box(new Rect(21, 836, 1033, 38), "1b2a2a");
            Label(37, 841, 1005, 28, toastTimer > 0 ? toast : "采集 → 炼制 → 出售或备战 → 守住夜晚 → 选择洞见", small);
            Label(25, 877, 730, 21, "WASD 移动 · E 交互 · 1—6 选配方/投药 · Esc 暂停", tiny);
            if (GUI.Button(new Rect(822, 875, 105, 23), "重新开始", button)) confirmingReset = true;
            if (GUI.Button(new Rect(937, 875, 117, 23), paused ? "继续" : "暂停", button)) paused = !paused;

            if (state.Phase == RunPhase.Insight) DrawInsightModal();
            else if (state.Phase == RunPhase.Victory || state.Phase == RunPhase.Defeat) DrawEndModal();
            else if (paused || confirmingReset) DrawPauseModal();
            GUI.matrix = previous;
        }

        void DrawHeader()
        {
            Label(27, 11, 430, 20, "THE LAST ALCHEMIST  /  VERTICAL SLICE", tiny);
            Label(25, 32, 430, 42, "最后的炼金术师", heading);
            string phaseText = state.Phase == RunPhase.Day ? "白昼 " + Mathf.CeilToInt(state.phaseRemaining) + " 秒" :
                state.Phase == RunPhase.Night ? "灰潮 " + state.enemiesDefeated + " / " + state.enemySpawnTarget : "炼金洞见";
            Label(27, 79, 430, 29, "第 " + state.day + " / " + AlchemyState.MaxDay + " 天  ·  " + phaseText, small);
            ResourceChip(438, 0, "金币", state.coins, "e3bd72");
            ResourceChip(590, 1, "史莱姆", state.slime, "65c9ae");
            ResourceChip(742, 2, "火焰草", state.herbs, "e8885b");
            ResourceChip(894, 3, "月光菇", state.mushrooms, "b084d1");
            ResourceChip(1046, 4, "魔晶", state.crystals, "69b9e5");
            Box(new Rect(1200, 22, 214, 84), "182426");
            Label(1216, 30, 184, 20, "城门耐久", tiny);
            Label(1216, 55, 184, 33, state.gateHp + " / " + state.gateMaxHp, bold);
            Progress(1216, 94, 182, state.gateHp / (float)state.gateMaxHp, state.gateHp < state.gateMaxHp * .35f ? "d75d55" : "73c29a");
        }

        void ResourceChip(float x, int icon, string title, int value, string color)
        {
            Box(new Rect(x, 22, 142, 84), "182426");
            Box(new Rect(x, 22, 3, 84), color);
            if (resourceIcons[icon]) GUI.DrawTexture(new Rect(x + 8, 29, 61, 61), resourceIcons[icon], ScaleMode.ScaleToFit, true);
            Label(x + 72, 30, 64, 20, title, tiny);
            Label(x + 72, 55, 64, 35, value.ToString(), bold);
        }

        Vector2 GuiPoint(Vector2 world, float scale, Vector2 offset)
        { Vector3 p = worldCamera.WorldToScreenPoint(world); return new Vector2((p.x - offset.x) / scale, (Screen.height - p.y - offset.y) / scale); }

        void DrawWorldLabels(float scale, Vector2 offset)
        {
            float cameraX = worldCamera.transform.position.x;
            string region = cameraX < -10f ? "01 / 荧光温室 · 原料采集区" : cameraX < 10f ? "02 / 炼金工坊 · 生产与贸易" : "03 / 灰潮城门 · 防御前线";
            Label(35, 141, 420, 27, region, bold);
            Label(455, 143, 390, 24, "A / D 横向探索  ·  镜头跟随炼金术师", tiny);
            for (int i = 0; i < stations.Length; i++)
            {
                var station = stations[i];
                bool selected = i == nearest;
                Vector2 p = GuiPoint(station.position + Vector2.down * .52f, scale, offset);
                if (p.x < 22 || p.x > 1418 || p.y < 132 || p.y > 719) continue;
                BoxAlpha(new Rect(p.x - 70, p.y, 140, selected ? 49 : 25), selected ? "4b3628" : "172225", selected ? .94f : .78f);
                var style = new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter, normal = { textColor = selected ? gold : cream } };
                GUI.Label(new Rect(p.x - 68, p.y, 136, 24), station.name, style);
                if (selected) GUI.Label(new Rect(p.x - 68, p.y + 23, 136, 23), "[ E ] " + ActionLabel(station), style);
                if (station.nodeIndex >= 0 && state.nodeCooldowns[station.nodeIndex] > 0)
                    Progress(p.x - 48, p.y - 7, 96, 1 - state.nodeCooldowns[station.nodeIndex] / state.ResourceCooldown, "71b693");
                if (station.kind == 4 && state.brewing) Progress(p.x - 52, p.y - 7, 104, state.BrewProgress, "e5af63");
            }
            if (state.EnemyAlive)
            {
                Vector2 p = GuiPoint(new Vector2(25.5f, -.6f), scale, offset);
                if (p.x < 22 || p.x > 1418 || p.y < 132 || p.y > 719) return;
                string enemyName = state.enemyType == 0 ? "灰烬尸兵" : state.enemyType == 1 ? "腐化重卫" : state.enemyType == 2 ? "疾行妖灵" : "灰潮之王";
                GUI.Label(new Rect(p.x - 80, p.y, 160, 24), enemyName, new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, normal = { textColor = state.enemyType == 3 ? gold : cream } });
                Progress(p.x - 65, p.y + 27, 130, state.enemyHealth / state.enemyMaxHealth, state.enemyType == 3 ? "bd76d4" : "d15e5e");
            }
        }

        string ActionLabel(Station station)
        {
            if (station.kind < 4) return state.nodeCooldowns[station.nodeIndex] > 0 ? "恢复中" : "采集 +" + state.GatherAmount;
            if (station.kind == 4) return state.brewing ? "炼制中" : "炼制";
            if (station.kind == 5) return "出售全部";
            if (station.kind == 6)
            {
                if (!state.automationUnlocked) return "解锁自动炼制";
                if (!state.autoSaleUnlocked) return "解锁自动出售";
                return state.autoEnabled ? "暂停炼制" : "开启炼制";
            }
            return state.autoDefenseEnabled ? "保留弹药" : "自动装填";
        }

        void DrawSidebar()
        {
            const float x = 1095;
            BoxAlpha(new Rect(x - 11, 137, 337, 199), "10191b", .80f);
            Label(x, 146, 320, 30, "炼金控制台", bold);
            Label(x, 176, 320, 24, state.Phase == RunPhase.Night ? "夜间仍可生产，但资源点不会停止恢复" : "点击配方或按数字键选择生产目标", tiny);
            BoxAlpha(new Rect(x, 204, 320, 122), "172326", .82f);
            Label(x + 14, 212, 292, 26, "当前配方 · " + AlchemyState.RecipeName(state.selectedRecipe), bold);
            Label(x + 14, 243, 292, 24, AlchemyState.RecipeFormula(state.selectedRecipe), small);
            string brew = state.brewing ? "炼制中 " + state.brewRemaining.ToString("0.0") + " 秒" : state.HasIngredients(state.selectedRecipe) ? "材料就绪" : "等待原料";
            Label(x + 14, 272, 292, 22, brew, small);
            Progress(x + 14, 305, 292, state.BrewProgress, "d6a85d");

            BoxAlpha(new Rect(x - 11, 350, 337, 159), "10191b", .80f);
            Label(x, 354, 320, 26, "自动化", bold);
            ToggleRow(384, "自动炼制", state.automationUnlocked, state.autoEnabled, AlchemyState.AutomationPrice, () => {
                if (!state.automationUnlocked) Notify(state.TryUnlockAutomation() ? "自动炼制已解锁。" : "金币不足。");
                else state.autoEnabled = !state.autoEnabled;
            });
            ToggleRow(425, "自动出售", state.autoSaleUnlocked, state.autoSaleEnabled, AlchemyState.AutoSalePrice, () => {
                if (!state.autoSaleUnlocked) Notify(state.TryUnlockAutoSale() ? "自动出售已解锁；每种药保留 3 瓶。" : "需先解锁自动炼制并准备足够金币。");
                else state.autoSaleEnabled = !state.autoSaleEnabled;
            });
            ToggleRow(466, "炮台装填", true, state.autoDefenseEnabled, 0, () => state.autoDefenseEnabled = !state.autoDefenseEnabled);

            BoxAlpha(new Rect(35, 178, 275, 166), "10191b", .68f);
            Label(50, 187, 245, 27, state.Phase == RunPhase.Night ? "灰潮报告" : "今日目标", bold);
            if (state.Phase == RunPhase.Night)
            {
                CompactGoal(50, 218, "波次", state.enemiesDefeated + " / " + state.enemySpawnTarget, state.enemiesDefeated >= state.enemySpawnTarget);
                CompactGoal(50, 252, "城门", state.gateHp + " / " + state.gateMaxHp, state.gateHp > 0);
                Label(50, 289, 245, 31, "点击底部药剂投掷 · 治疗药修门", tiny);
            }
            else
            {
                CompactGoal(50, 218, "备药", "至少 3 瓶", AttackPotionCount() >= 3);
                CompactGoal(50, 252, "自动化", state.automationUnlocked ? "已启动" : state.coins + " / " + AlchemyState.AutomationPrice, state.automationUnlocked);
                if (state.currentEvent > 0) Label(50, 284, 245, 21, EventDescriptions[state.currentEvent], tiny);
                GUI.enabled = !state.brewing;
                if (GUI.Button(new Rect(50, 308, 245, 28), "准备完成 · 提前入夜", button)) { state.BeginNight(); OnPhaseChanged(RunPhase.Night); }
                GUI.enabled = true;
            }

            BoxAlpha(new Rect(x - 11, 628, 337, 84), "10191b", .76f);
            Label(x, 634, 320, 23, "附近交互", bold);
            Label(x, 659, 320, 19, nearest >= 0 ? stations[nearest].name + " · " + stations[nearest].subtitle : "靠近场景中的资源或设备", tiny);
            GUI.enabled = nearest >= 0;
            if (GUI.Button(new Rect(x, 682, 320, 25), nearest >= 0 ? "E  /  " + ActionLabel(stations[nearest]) : "暂无可交互目标", button)) Interact();
            GUI.enabled = true;
            Label(x, 873, 320, 20, "炼制 " + state.totalBrewed + " · 售出 " + state.totalSold + " · 击退 " + state.totalKills, tiny);
        }

        int AttackPotionCount() => state.potions[0] + state.potions[2] + state.potions[3] + state.potions[4];

        void ToggleRow(float y, string title, bool unlocked, bool enabled, int price, Action action)
        {
            BoxAlpha(new Rect(1095, y, 320, 38), "172326", .86f);
            Label(1108, y + 7, 158, 24, title, small);
            string label = !unlocked ? "解锁 " + price : enabled ? "开启" : "关闭";
            if (GUI.Button(new Rect(1280, y + 5, 122, 28), label, button)) { action(); Save(); }
        }

        void CompactGoal(float x, float y, string title, string description, bool done)
        {
            BoxAlpha(new Rect(x, y, 28, 28), done ? "47745f" : "3b3130", .92f);
            Label(x + 6, y, 18, 28, done ? "✓" : "·", centered);
            Label(x + 38, y - 1, 80, 22, title, small);
            Label(x + 115, y - 1, 128, 22, description, tiny);
        }

        void DrawPotionBar()
        {
            for (int i = 0; i < 6; i++)
            {
                float x = 22 + i * 173;
                bool selected = state.Phase == RunPhase.Day && state.selectedRecipe == i;
                Box(new Rect(x, 744, 160, 82), selected ? "563c2d" : "182527");
                Color old = GUI.color; GUI.color = PotionColors[i]; GUI.DrawTexture(new Rect(x, 744, 4, 82), Texture2D.whiteTexture); GUI.color = old;
                Label(x + 12, 749, 142, 20, (i + 1) + "  " + AlchemyState.RecipeName(i), tiny);
                Label(x + 12, 772, 78, 35, state.potions[i].ToString(), bold);
                Label(x + 70, 775, 82, 20, "售价 " + state.PotionPrice(i), tiny);
                string action = state.Phase == RunPhase.Night ? "使用" : selected ? "当前配方" : "选择配方";
                if (GUI.Button(new Rect(x + 72, 800, 80, 21), action, button)) PotionAction(i);
            }
        }

        void DrawInsightModal()
        {
            DrawDimmer();
            Box(new Rect(245, 190, 950, 510), "171c1e");
            Label(355, 224, 730, 42, "灰潮退去，一种新的理解浮现……", heading);
            Label(451, 272, 540, 30, "选择一项炼金洞见 · 本局永久生效", small);
            for (int i = 0; i < 3; i++)
            {
                int id = state.insightChoices[i];
                float x = 285 + i * 295;
                Box(new Rect(x, 330, 270, 280), i == 1 ? "24383a" : "202d2d");
                Label(x + 18, 352, 234, 26, "洞见 " + (i + 1), tiny);
                Label(x + 18, 397, 234, 36, UpgradeNames[id], bold);
                GUI.Label(new Rect(x + 18, 447, 234, 72), UpgradeDescriptions[id], new GUIStyle(small) { wordWrap = true, alignment = TextAnchor.UpperLeft });
                Label(x + 18, 524, 234, 24, state.HasUpgrade(id) ? "已拥有 · 可重复选择" : "新洞见", tiny);
                if (GUI.Button(new Rect(x + 18, 558, 234, 36), "选择", button))
                {
                    state.ChooseInsight(i);
                    Notify(EventDescriptions[state.currentEvent], 9);
                    Save();
                }
            }
        }

        void DrawEndModal()
        {
            DrawDimmer();
            Box(new Rect(395, 235, 650, 405), "171c1e");
            string title = state.Phase == RunPhase.Victory ? "贤者熔炉已经点燃" : "灰潮吞没了工坊";
            string subtitle = state.Phase == RunPhase.Victory ? "你守住了五个昼夜，并击败了灰潮之王。" : "重新规划配方、库存和自动装填，再试一次。";
            Label(465, 276, 520, 45, title, heading);
            GUI.Label(new Rect(465, 331, 520, 52), subtitle, new GUIStyle(small) { wordWrap = true, alignment = TextAnchor.UpperCenter });
            Box(new Rect(465, 402, 510, 85), "202d2d");
            Label(488, 413, 470, 26, "本局炼制 " + state.totalBrewed + " 瓶 · 售出 " + state.totalSold + " 瓶", small);
            Label(488, 445, 470, 26, "击退 " + state.totalKills + " 名敌人 · 剩余金币 " + state.coins, small);
            if (GUI.Button(new Rect(520, 535, 400, 46), "开始新的炼金实验", button)) RestartRun();
        }

        void DrawPauseModal()
        {
            DrawDimmer();
            Box(new Rect(455, 298, 530, 270), "171c1e");
            Label(505, 332, 430, 45, confirmingReset ? "重新点燃第一炉？" : "工坊已暂停", heading);
            Label(505, 392, 430, 45, confirmingReset ? "当前材料、金币、升级和天数都会清空。" : "生产、昼夜倒计时和灰潮均已冻结。", small);
            if (GUI.Button(new Rect(505, 485, 195, 42), confirmingReset ? "取消" : "返回工坊", button)) { confirmingReset = false; paused = false; }
            if (GUI.Button(new Rect(735, 485, 200, 42), confirmingReset ? "确认重新开始" : "保存进度", button))
            {
                if (confirmingReset) RestartRun();
                else if (Save()) { paused = false; Notify("进度已保存。"); }
            }
        }

        void DrawDimmer()
        {
            Color old = GUI.color;
            GUI.color = new Color(0, 0, 0, .82f);
            GUI.DrawTexture(new Rect(0, 0, 1440, 900), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
