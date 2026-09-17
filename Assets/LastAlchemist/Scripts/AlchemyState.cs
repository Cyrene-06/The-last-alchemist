using System;

namespace LastAlchemist
{
    public enum MaterialKind { Slime, FireHerb, MoonMushroom, Crystal }
    public enum PotionKind { Fire, Healing, Frost, Venom, Bomb, Elixir }
    public enum RunPhase { Day, Night, Insight, Victory, Defeat }

    // Pure, deterministic simulation. Unity is only responsible for presentation and input.
    [Serializable]
    public sealed class AlchemyState
    {
        public const int SaveVersion = 3;
        public const int AutomationPrice = 48;
        public const int AutoSalePrice = 72;
        public const int MaxDay = 5;
        public const float DayDuration = 75f;

        static readonly float[] BaseBrewTimes = { 3f, 3.4f, 4f, 4.2f, 4.8f, 6f };
        static readonly int[] BasePrices = { 12, 14, 18, 20, 26, 36 };

        public int version = SaveVersion;
        public int day = 1;
        public int phase = (int)RunPhase.Day;
        public float phaseRemaining = DayDuration;
        public int slime, herbs, mushrooms, crystals;
        public int[] potions = new int[6];
        public int selectedRecipe;
        public int coins;
        public int totalGathered, totalBrewed, totalSold, totalKills;
        public bool automationUnlocked, autoEnabled, autoSaleUnlocked, autoSaleEnabled, autoDefenseEnabled = true;
        public bool brewing;
        public int brewRecipe;
        public float brewRemaining;
        public float playerX = -29f, playerY = -3.1f;
        public float[] nodeCooldowns = new float[4];

        public int gateHp = 100, gateMaxHp = 100;
        public int enemiesSpawned, enemiesDefeated, enemySpawnTarget, enemyType;
        public float enemyHealth, enemyMaxHealth, spawnTimer, enemyAttackTimer, turretTimer, frostTimer, fortifyTimer;
        public int upgradeMask;
        public int[] insightChoices = { 0, 1, 2 };
        public int currentEvent;
        public float autoSaleTimer;

        public RunPhase Phase => (RunPhase)phase;
        public bool HasUpgrade(int id) => id >= 0 && id < 15 && (upgradeMask & (1 << id)) != 0;
        public int GatherAmount => HasUpgrade(1) ? 3 : 2;
        public float ResourceCooldown => HasUpgrade(13) ? 3.5f : 5.5f;
        public float BrewProgress => brewing ? 1f - brewRemaining / BrewTime(brewRecipe) : 0f;
        public bool EnemyAlive => Phase == RunPhase.Night && enemyHealth > 0;

        public int MaterialCount(MaterialKind kind)
        {
            switch (kind)
            {
                case MaterialKind.Slime: return slime;
                case MaterialKind.FireHerb: return herbs;
                case MaterialKind.MoonMushroom: return mushrooms;
                default: return crystals;
            }
        }

        public void Gather(MaterialKind kind)
        {
            int amount = GatherAmount;
            switch (kind)
            {
                case MaterialKind.Slime: slime += amount; break;
                case MaterialKind.FireHerb: herbs += amount; break;
                case MaterialKind.MoonMushroom: mushrooms += amount; break;
                case MaterialKind.Crystal: crystals += amount; break;
            }
            totalGathered += amount;
        }

        public static string RecipeName(int recipe)
        {
            string[] names = { "火焰药剂", "治疗药剂", "寒霜药剂", "腐蚀药剂", "爆裂药剂", "贤者灵药" };
            return names[Math.Max(0, Math.Min(recipe, names.Length - 1))];
        }

        public static string RecipeFormula(int recipe)
        {
            string[] formulas = { "史莱姆液 + 火焰草", "史莱姆液 + 月光菇", "月光菇 + 魔晶", "史莱姆液 + 魔晶", "火焰草 + 魔晶", "火焰草 + 月光菇 + 魔晶" };
            return formulas[Math.Max(0, Math.Min(recipe, formulas.Length - 1))];
        }

        public float BrewTime(int recipe)
        {
            float multiplier = HasUpgrade(0) ? .78f : 1f;
            if (HasUpgrade(5)) multiplier *= .88f;
            return BaseBrewTimes[Math.Max(0, Math.Min(recipe, 5))] * multiplier;
        }

        public int PotionPrice(int recipe)
        {
            float multiplier = HasUpgrade(3) ? 1.25f : 1f;
            if (HasUpgrade(14)) multiplier *= 1.2f;
            return (int)Math.Round(BasePrices[Math.Max(0, Math.Min(recipe, 5))] * multiplier);
        }

        public bool HasIngredients(int recipe)
        {
            switch ((PotionKind)recipe)
            {
                case PotionKind.Fire: return slime > 0 && herbs > 0;
                case PotionKind.Healing: return slime > 0 && mushrooms > 0;
                case PotionKind.Frost: return mushrooms > 0 && crystals > 0;
                case PotionKind.Venom: return slime > 0 && crystals > 0;
                case PotionKind.Bomb: return herbs > 0 && crystals > 0;
                default: return herbs > 0 && mushrooms > 0 && crystals > 0;
            }
        }

        void SpendIngredients(int recipe)
        {
            switch ((PotionKind)recipe)
            {
                case PotionKind.Fire: slime--; herbs--; break;
                case PotionKind.Healing: slime--; mushrooms--; break;
                case PotionKind.Frost: mushrooms--; crystals--; break;
                case PotionKind.Venom: slime--; crystals--; break;
                case PotionKind.Bomb: herbs--; crystals--; break;
                default: herbs--; mushrooms--; crystals--; break;
            }
        }

        public bool SelectRecipe(int recipe)
        {
            if (recipe < 0 || recipe >= potions.Length || brewing) return false;
            selectedRecipe = recipe;
            return true;
        }

        public bool TryStartBrew(int recipe = -1)
        {
            recipe = recipe < 0 ? selectedRecipe : recipe;
            if (brewing || recipe < 0 || recipe >= potions.Length || !HasIngredients(recipe)) return false;
            SpendIngredients(recipe);
            brewRecipe = recipe;
            brewing = true;
            brewRemaining = BrewTime(recipe);
            return true;
        }

        public void Tick(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds) || Phase == RunPhase.Insight || Phase == RunPhase.Victory || Phase == RunPhase.Defeat) return;
            while (seconds > 0)
            {
                float step = Math.Min(seconds, .05f);
                seconds -= step;
                TickProduction(step);
                if (Phase == RunPhase.Day)
                {
                    phaseRemaining -= step;
                    if (phaseRemaining <= 0) BeginNight();
                }
                else if (Phase == RunPhase.Night) TickNight(step);
            }
        }

        void TickProduction(float step)
        {
            if (brewing)
            {
                brewRemaining -= step;
                if (brewRemaining <= 0)
                {
                    brewing = false;
                    brewRemaining = 0;
                    int yield = HasUpgrade(5) && (totalBrewed + 1) % 4 == 0 ? 2 : 1;
                    potions[brewRecipe] += yield;
                    totalBrewed += yield;
                }
            }
            if (!brewing && automationUnlocked && autoEnabled) TryStartBrew();
            if (autoSaleUnlocked && autoSaleEnabled)
            {
                autoSaleTimer -= step;
                if (autoSaleTimer <= 0)
                {
                    AutoSellExcess();
                    autoSaleTimer = 4f;
                }
            }
        }

        public int SellAll()
        {
            int earned = 0;
            for (int i = 0; i < potions.Length; i++)
            {
                earned += potions[i] * PotionPrice(i);
                totalSold += potions[i];
                potions[i] = 0;
            }
            coins += earned;
            return earned;
        }

        public int AutoSellExcess()
        {
            int earned = 0;
            for (int i = 0; i < potions.Length; i++)
            {
                int amount = Math.Max(0, potions[i] - 3);
                potions[i] -= amount;
                totalSold += amount;
                earned += amount * PotionPrice(i);
            }
            coins += earned;
            return earned;
        }

        public bool TryUnlockAutomation()
        {
            if (automationUnlocked || coins < AutomationPrice) return false;
            coins -= AutomationPrice;
            automationUnlocked = true;
            autoEnabled = true;
            return true;
        }

        public bool TryUnlockAutoSale()
        {
            if (!automationUnlocked || autoSaleUnlocked || coins < AutoSalePrice) return false;
            coins -= AutoSalePrice;
            autoSaleUnlocked = true;
            autoSaleEnabled = true;
            autoSaleTimer = 4f;
            return true;
        }

        public void BeginNight()
        {
            if (Phase != RunPhase.Day) return;
            phase = (int)RunPhase.Night;
            phaseRemaining = 0;
            enemiesSpawned = enemiesDefeated = 0;
            enemySpawnTarget = 4 + day * 2;
            enemyHealth = enemyMaxHealth = 0;
            spawnTimer = .7f;
            turretTimer = .5f;
            if (HasUpgrade(7)) gateHp = Math.Min(gateMaxHp, gateHp + 20);
        }

        void TickNight(float step)
        {
            frostTimer = Math.Max(0, frostTimer - step);
            fortifyTimer = Math.Max(0, fortifyTimer - step);
            if (!EnemyAlive)
            {
                if (enemiesSpawned >= enemySpawnTarget)
                {
                    FinishNight();
                    return;
                }
                spawnTimer -= step;
                if (spawnTimer <= 0) SpawnEnemy();
                return;
            }

            turretTimer -= step;
            if (turretTimer <= 0)
            {
                enemyHealth -= TurretShot();
                turretTimer += HasUpgrade(6) ? 1.05f : 1.35f;
                if (enemyHealth <= 0) KillEnemy();
            }
            if (!EnemyAlive) return;

            enemyAttackTimer -= step;
            if (enemyAttackTimer <= 0)
            {
                int damage = EnemyDamage();
                if (fortifyTimer > 0) damage = Math.Max(1, damage / 2);
                gateHp = Math.Max(0, gateHp - damage);
                enemyAttackTimer += EnemyAttackInterval() + (frostTimer > 0 ? 1.1f : 0);
                if (gateHp <= 0) phase = (int)RunPhase.Defeat;
            }
        }

        void SpawnEnemy()
        {
            bool boss = day == MaxDay && enemiesSpawned == enemySpawnTarget - 1;
            enemyType = boss ? 3 : enemiesSpawned % 3;
            enemyMaxHealth = boss ? 125 : enemyType == 1 ? 20 + day * 4 : 13 + day * 3 + enemyType * 2;
            enemyHealth = enemyMaxHealth;
            enemiesSpawned++;
            enemyAttackTimer = boss ? 1.5f : enemyType == 2 ? 1.05f : 1.45f;
        }

        float TurretShot()
        {
            float damage = HasUpgrade(6) ? 7 : 4;
            if (!autoDefenseEnabled) return damage;
            int recipe = -1;
            int[] priority = { (int)PotionKind.Bomb, (int)PotionKind.Fire, (int)PotionKind.Venom, (int)PotionKind.Frost };
            foreach (int candidate in priority)
                if (potions[candidate] > 0) { recipe = candidate; break; }
            if (recipe < 0) return damage;
            potions[recipe]--;
            return damage + PotionDamage(recipe);
        }

        int EnemyDamage()
        {
            if (enemyType == 3) return 12;
            return 3 + day + (enemyType == 1 ? 3 : enemyType == 2 ? 1 : 0);
        }

        float EnemyAttackInterval() => enemyType == 3 ? 1.35f : enemyType == 1 ? 1.8f : enemyType == 2 ? 1.05f : 1.45f;

        int PotionDamage(int recipe)
        {
            int damage;
            switch ((PotionKind)recipe)
            {
                case PotionKind.Fire: damage = 21; if (HasUpgrade(2)) damage += 12; break;
                case PotionKind.Frost: damage = 11; break;
                case PotionKind.Venom: damage = 27; break;
                case PotionKind.Bomb: damage = 44 + (HasUpgrade(9) ? 20 : 0); break;
                default: damage = 0; break;
            }
            return damage;
        }

        public string UsePotion(int recipe)
        {
            if (recipe < 0 || recipe >= potions.Length || potions[recipe] <= 0) return "这种药剂库存不足。";
            if ((PotionKind)recipe == PotionKind.Healing)
            {
                if (gateHp >= gateMaxHp) return "城门目前无需修复。";
                potions[recipe]--;
                int heal = 25 + (HasUpgrade(10) ? 15 : 0);
                gateHp = Math.Min(gateMaxHp, gateHp + heal);
                return "治疗药剂修复城门 +" + heal;
            }
            if ((PotionKind)recipe == PotionKind.Elixir)
            {
                potions[recipe]--;
                fortifyTimer = 10;
                gateHp = Math.Min(gateMaxHp, gateHp + 10);
                return "贤者灵药强化城门 10 秒。";
            }
            if (!EnemyAlive) return "当前没有可攻击的敌人。";
            potions[recipe]--;
            int damage = PotionDamage(recipe);
            enemyHealth -= damage;
            if ((PotionKind)recipe == PotionKind.Frost) frostTimer = HasUpgrade(8) ? 7 : 4;
            if (enemyHealth <= 0) KillEnemy();
            return RecipeName(recipe) + "造成 " + damage + " 点伤害。";
        }

        void KillEnemy()
        {
            if (enemyHealth <= 0 && enemyMaxHealth > 0)
            {
                enemiesDefeated++;
                totalKills++;
                coins += 2 + day + (HasUpgrade(11) ? 3 : 0) + (enemyType == 3 ? 30 : 0);
                enemyHealth = enemyMaxHealth = 0;
                spawnTimer = .75f;
            }
        }

        void FinishNight()
        {
            if (day >= MaxDay)
            {
                phase = (int)RunPhase.Victory;
                return;
            }
            phase = (int)RunPhase.Insight;
            // Performance-dependent offset keeps different runs from offering the same draft.
            int start = ((day - 1) * 3 + totalBrewed + totalKills) % 15;
            insightChoices[0] = start;
            insightChoices[1] = (start + 5) % 15;
            insightChoices[2] = (start + 10) % 15;
        }

        public bool ChooseInsight(int slot)
        {
            if (Phase != RunPhase.Insight || slot < 0 || slot >= insightChoices.Length) return false;
            int id = insightChoices[slot];
            upgradeMask |= 1 << id;
            if (id == 4)
            {
                gateMaxHp += 30;
                gateHp += 30;
            }
            day++;
            ApplyDayEvent();
            phase = (int)RunPhase.Day;
            phaseRemaining = DayDuration;
            return true;
        }

        void ApplyDayEvent()
        {
            currentEvent = ((day + totalBrewed + totalKills) % 5) + 1;
            switch (currentEvent)
            {
                case 1: herbs += 3; mushrooms += 2; break;
                case 2: slime += 5; break;
                case 3: crystals += 4; break;
                case 4: coins += 18; break;
                case 5: gateHp = Math.Min(gateMaxHp, gateHp + 35); break;
            }
        }

        public bool IsValid()
        {
            if (version != SaveVersion || day < 1 || day > MaxDay || phase < 0 || phase > (int)RunPhase.Defeat ||
                slime < 0 || herbs < 0 || mushrooms < 0 || crystals < 0 || coins < 0 ||
                potions == null || potions.Length != 6 || selectedRecipe < 0 || selectedRecipe >= 6 || brewRecipe < 0 || brewRecipe >= 6 ||
                gateMaxHp <= 0 || gateHp < 0 || gateHp > gateMaxHp || totalGathered < 0 || totalBrewed < 0 || totalSold < 0 || totalKills < 0 ||
                (autoEnabled && !automationUnlocked) || (autoSaleEnabled && !autoSaleUnlocked) || (autoSaleUnlocked && !automationUnlocked) ||
                nodeCooldowns == null || nodeCooldowns.Length != 4 || insightChoices == null || insightChoices.Length != 3 ||
                float.IsNaN(phaseRemaining) || float.IsInfinity(phaseRemaining) || phaseRemaining < 0 ||
                float.IsNaN(brewRemaining) || float.IsInfinity(brewRemaining) || brewRemaining < 0 ||
                float.IsNaN(enemyHealth) || float.IsInfinity(enemyHealth) || enemyHealth < 0 ||
                float.IsNaN(playerX) || float.IsInfinity(playerX) || float.IsNaN(playerY) || float.IsInfinity(playerY)) return false;
            for (int i = 0; i < potions.Length; i++) if (potions[i] < 0) return false;
            foreach (float value in nodeCooldowns)
                if (float.IsNaN(value) || float.IsInfinity(value) || value < 0 || value > 6f) return false;
            return true;
        }
    }
}
