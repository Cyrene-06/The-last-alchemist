#if UNITY_EDITOR || DEBUG
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace LastAlchemist
{
    public sealed class AlchemySmokeTest : MonoBehaviour
    {
        string output;
        AlchemyPrototype game;
        bool completed;
        Keyboard testKeyboard;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartWhenRequested()
        {
            if (AlchemyPrototype.IsSmokeTest) new GameObject("Vertical slice smoke checks").AddComponent<AlchemySmokeTest>();
        }

        IEnumerator Start()
        {
            output = Path.Combine(Application.dataPath, "..", "SmokeResults");
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "runtime-checks.txt"), "");
            Application.logMessageReceived += OnLog;
            yield return null;
            game = FindAnyObjectByType<AlchemyPrototype>();
            Check(game != null, "Scene bootstrap exists");
            Check(game.StyledPlantStationsActive, "Fire herb rack and moonlight mushroom bed use styled image models");
            Screen.SetResolution(1440, 900, false);
            yield return new WaitForSeconds(.8f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "01-workshop.png"));

            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testKeyboard = InputSystem.AddDevice<Keyboard>();
            Vector2 start = game.PlayerPosition;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.D));
            yield return new WaitForSeconds(.2f);
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null;
            Check(game.PlayerPosition.x > start.x + .15f, "Keyboard movement uses Input System");

            Vector2[] resources = { new Vector2(-28f, -3.35f), new Vector2(-21.5f, -3.15f), new Vector2(-15f, -3.15f), new Vector2(-8.5f, -3.15f) };
            for (int i = 0; i < resources.Length; i++)
            {
                yield return Walk(resources[i]);
                yield return Press(Key.E);
                if (i == 0)
                {
                    Check(game.SlimeHarvestAnimating, "Slime vat switches to depleted harvest animation");
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, "02-slime-harvest.png"));
                }
            }
            Check(game.State.slime == 2 && game.State.herbs == 2 && game.State.mushrooms == 2 && game.State.crystals == 2, "All four materials gather through world interactions");
            yield return Walk(new Vector2(-1.5f, -3.05f));
            yield return Press(Key.E);
            Check(game.State.brewing, "Cauldron starts selected recipe through E");
            yield return new WaitForSeconds(3.2f);
            Check(game.State.potions[(int)PotionKind.Fire] == 1, "Fire potion completes into hotbar inventory");

            game.State.coins = AlchemyState.AutomationPrice;
            yield return Walk(new Vector2(11.5f, -3.1f));
            yield return Press(Key.E);
            Check(game.State.automationUnlocked && game.State.autoEnabled, "Automation upgrades through its world station");
            game.State.slime = game.State.herbs = 2;
            yield return new WaitForSeconds(6.2f);
            Check(game.State.potions[(int)PotionKind.Fire] >= 3, "Automatic production visibly grows potion inventory");

            game.State.potions[(int)PotionKind.Fire] = 12;
            game.State.autoDefenseEnabled = false;
            yield return Walk(new Vector2(21f, -3.05f));
            game.State.BeginNight();
            yield return new WaitForSeconds(.9f);
            Check(game.State.Phase == RunPhase.Night && game.State.EnemyAlive, "Night phase spawns an enemy");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "02-night-defense.png"));
            game.State.autoDefenseEnabled = true;
            yield return new WaitForSeconds(22f);
            Check(game.State.Phase == RunPhase.Insight && game.State.gateHp > 0, "Potion-fed turret survives the first night");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "03-insight-choice.png"));
            yield return new WaitForSeconds(.6f);
            int upgrade = game.State.insightChoices[0];
            Check(game.State.ChooseInsight(0) && game.State.HasUpgrade(upgrade) && game.State.day == 2, "Roguelike insight advances to day two");

            yield return new WaitForSeconds(.5f);
            Screen.SetResolution(1024, 768, false);
            yield return new WaitForSeconds(.7f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "04-small-window.png"));
            yield return new WaitForSeconds(.8f);
            foreach (string name in new[] { "01-workshop.png", "02-slime-harvest.png", "02-night-defense.png", "03-insight-choice.png", "04-small-window.png" }) CheckScreenshot(name);
            completed = true;
            File.AppendAllText(Path.Combine(output, "runtime-checks.txt"), "PASS: ALL VERTICAL SLICE RUNTIME CHECKS\n");
            Application.Quit(0);
        }

        void CheckScreenshot(string name)
        {
            string path = Path.Combine(output, name);
            Check(File.Exists(path), "Screenshot exists: " + name);
            var texture = new Texture2D(2, 2);
            Check(texture.LoadImage(File.ReadAllBytes(path)), "Screenshot decodes: " + name);
            var colors = new System.Collections.Generic.HashSet<Color32>();
            for (int y = 0; y < texture.height; y += 20)
                for (int x = 0; x < texture.width; x += 20) colors.Add(texture.GetPixel(x, y));
            Destroy(texture);
            Check(colors.Count > 30, "Screenshot contains rendered content: " + name);
        }

        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(key));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
            yield return null; yield return null;
        }

        IEnumerator Walk(Vector2 target)
        {
            int watchdog = 0;
            while (Vector2.Distance(game.PlayerPosition, target) > .12f)
            {
                Check(++watchdog < 1500, "Movement path watchdog", false);
                Vector2 difference = target - game.PlayerPosition;
                game.MovePlayer(difference.normalized, Mathf.Min(.05f, difference.magnitude / 4.1f));
                yield return null;
            }
            yield return null;
        }

        void Check(bool condition, string message, bool logSuccess = true)
        {
            if (!condition) throw new InvalidOperationException("Smoke test failed: " + message);
            if (logSuccess) File.AppendAllText(Path.Combine(output, "runtime-checks.txt"), "PASS: " + message + "\n");
        }

        void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            File.AppendAllText(Path.Combine(output, "runtime-checks.txt"), "FAIL: " + condition + "\n" + stackTrace + "\n");
            Application.Quit(1);
        }

        void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            if (testKeyboard != null && testKeyboard.added) InputSystem.RemoveDevice(testKeyboard);
            if (!completed) Debug.LogWarning("Smoke checks did not complete.");
        }
    }
}
#endif
