using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastAlchemist.Editor
{
    public static class PrototypeTools
    {
        public const string ScenePath = "Assets/LastAlchemist/Scenes/AlchemyPrototype.unity";

        [MenuItem("炼金原型/打开垂直切片")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("炼金原型/运行核心逻辑自检")]
        public static void RunChecks()
        {
            int count = 0;
            Action<bool, string> check = (ok, message) => { if (!ok) throw new Exception("FAIL: " + message); count++; Debug.Log("PASS: " + message); };
            var state = new AlchemyState();
            check(state.IsValid() && state.Phase == RunPhase.Day && state.day == 1, "New five-day run is valid");
            check(!state.TryStartBrew(), "Empty inventory cannot brew");

            state.Gather(MaterialKind.Slime);
            state.Gather(MaterialKind.FireHerb);
            state.Gather(MaterialKind.MoonMushroom);
            state.Gather(MaterialKind.Crystal);
            check(state.slime == 2 && state.herbs == 2 && state.mushrooms == 2 && state.crystals == 2, "Four material nodes gather independently");
            for (int recipe = 0; recipe < 6; recipe++)
            {
                var brew = new AlchemyState { slime = 2, herbs = 2, mushrooms = 2, crystals = 2 };
                check(brew.SelectRecipe(recipe) && brew.TryStartBrew(), "Recipe " + recipe + " reserves its ingredients");
                int before = brew.slime + brew.herbs + brew.mushrooms + brew.crystals;
                check(!brew.TryStartBrew(), "Busy cauldron cannot double-spend recipe " + recipe);
                brew.Tick(brew.BrewTime(recipe) + .1f);
                check(!brew.brewing && brew.potions[recipe] == 1 && before < 8, "Recipe " + recipe + " completes into correct inventory slot");
            }

            var economy = new AlchemyState { slime = 4, herbs = 4 };
            for (int i = 0; i < 4; i++) { check(economy.TryStartBrew(0), "Manual fire brew starts"); economy.Tick(3.1f); }
            check(economy.SellAll() == 48 && economy.coins == 48 && economy.totalSold == 4, "Four fire potions fund automation");
            check(economy.TryUnlockAutomation() && economy.autoEnabled && economy.coins == 0, "Automation purchase spends exactly 48");
            economy.slime = economy.herbs = 2;
            economy.Tick(6.2f);
            check(economy.potions[0] == 2 && !economy.brewing, "Automation drains available ingredients then idles");
            economy.coins = AlchemyState.AutoSalePrice;
            check(economy.TryUnlockAutoSale() && economy.autoSaleEnabled, "Auto-sale unlock requires automation");
            economy.potions[0] = 7;
            economy.Tick(4.1f);
            check(economy.potions[0] == 3 && economy.coins == 48, "Auto-sale preserves three combat potions and sells excess");

            var combat = new AlchemyState();
            combat.potions[(int)PotionKind.Fire] = 12;
            combat.potions[(int)PotionKind.Healing] = 2;
            combat.BeginNight();
            check(combat.Phase == RunPhase.Night && combat.enemySpawnTarget == 6, "Day one starts a six-enemy night");
            combat.Tick(1f);
            check(combat.EnemyAlive, "First night enemy spawns");
            int kills = combat.totalKills;
            combat.Tick(80f);
            check(combat.Phase == RunPhase.Insight && combat.totalKills == kills + 6 && combat.gateHp > 0, "Prepared defenses survive the complete first wave");
            int chosen = combat.insightChoices[1];
            check(combat.ChooseInsight(1) && combat.day == 2 && combat.Phase == RunPhase.Day && combat.HasUpgrade(chosen), "Insight advances the run and applies upgrade");
            check(combat.currentEvent >= 1 && combat.currentEvent <= 5, "A performance-dependent day event is selected");

            var tools = new AlchemyState { gateHp = 30, mushrooms = 1, crystals = 1 };
            tools.potions[(int)PotionKind.Healing] = 1;
            check(tools.UsePotion((int)PotionKind.Healing).Contains("修复") && tools.gateHp == 55, "Healing potion repairs the gate");
            tools.BeginNight(); tools.Tick(1);
            tools.potions[(int)PotionKind.Frost] = 1;
            check(tools.UsePotion((int)PotionKind.Frost).Contains("伤害") && tools.frostTimer > 0, "Frost potion damages and slows an enemy");

            var defeated = new AlchemyState { gateHp = 1, autoDefenseEnabled = false };
            defeated.BeginNight(); defeated.Tick(5);
            check(defeated.Phase == RunPhase.Defeat, "Broken gate ends the run");

            var a = new AlchemyState { slime = 10, herbs = 10, automationUnlocked = true, autoEnabled = true };
            var b = JsonUtility.FromJson<AlchemyState>(JsonUtility.ToJson(a));
            a.Tick(7.5f); for (int i = 0; i < 75; i++) b.Tick(.1f);
            check(a.potions[0] == b.potions[0] && a.slime == b.slime && Mathf.Abs(a.brewRemaining - b.brewRemaining) < .06f, "Production is frame-rate independent");

            string saveDir = Path.GetFullPath(Path.Combine("ValidationResults", "SaveChecks", Guid.NewGuid().ToString("N")));
            string saveFile = Path.Combine(saveDir, "test-save.json");
            var disk = new AlchemyState { slime = 2, herbs = 2 };
            disk.TryStartBrew(); disk.Tick(1);
            check(AlchemySave.WriteTo(disk, saveFile), "Version three save is written");
            var loaded = AlchemySave.LoadFrom(saveFile, out string notice);
            check(loaded.IsValid() && loaded.brewing && loaded.brewRemaining > 1.8f, "In-progress multi-system state round-trips");
            disk.Tick(2.2f); AlchemySave.WriteTo(disk, saveFile);
            check(File.Exists(saveFile + ".bak"), "Atomic replacement keeps a backup");
            File.WriteAllText(saveFile, "{ invalid json");
            loaded = AlchemySave.LoadFrom(saveFile, out notice);
            check(loaded.brewing && notice.Length > 0, "Corrupt main file recovers valid backup");

            state.Tick(-1); state.Tick(float.NaN); state.Tick(float.PositiveInfinity);
            check(state.IsValid(), "Invalid time deltas are ignored");
            state.coins = -1; check(!state.IsValid(), "Negative balances are rejected");
            string report = "PASS: " + count + " vertical-slice checks. " + DateTime.UtcNow.ToString("O");
            Directory.CreateDirectory("ValidationResults");
            File.WriteAllText("ValidationResults/core-checks.txt", report);
            Debug.Log(report);
        }

        [MenuItem("炼金原型/构建 Windows 可玩版")]
        public static void BuildWindows()
        {
            RunChecks();
            string directory = Environment.GetEnvironmentVariable("ALCHEMY_BUILD_DIR");
            if (string.IsNullOrEmpty(directory)) directory = Path.GetFullPath("Builds/LastAlchemist");
            Directory.CreateDirectory(directory);
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Standalone, ManagedStrippingLevel.Disabled);
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1440;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.companyName = "LastAlchemistPrototype";
            PlayerSettings.productName = "最后的炼金术师";
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = Path.Combine(directory, "LastAlchemist.exe"),
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("ALCHEMY_BUILD_SUCCEEDED: " + report.summary.outputPath);
        }
    }
}
