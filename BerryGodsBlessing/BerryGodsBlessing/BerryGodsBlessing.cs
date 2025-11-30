using BepInEx;
using SuperFantasyKingdom;
using SuperFantasyKingdom.Spawner;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BerryGodsBlessing
{
    [BepInPlugin("com.low.berrygodsblessing", "Berry God's Blessing", "0.1.0")]
    public class BerryGodPlugin : BaseUnityPlugin
    {
        int currentBerryCount = 0;
        System.Random rng = new System.Random();
        private void EnsureHideManagerGameObjectDisabled()
        {
            try
            {
                string configPath = Path.Combine(Paths.ConfigPath, "BepInEx.cfg");

                if (!File.Exists(configPath))
                {
                    Logger.LogWarning("BepInEx.cfg not found — cannot verify HideManagerGameObject.");
                    return;
                }

                var lines = File.ReadAllLines(configPath);
                bool changed = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("HideManagerGameObject"))
                    {
                        if (!lines[i].Contains("true"))
                        {
                            lines[i] = "HideManagerGameObject = true";
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    File.WriteAllLines(configPath, lines);
                    Logger.LogInfo("Patched BepInEx.cfg → HideManagerGameObject = true");
                }
                else
                {
                    Logger.LogInfo("HideManagerGameObject already set to true.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to update BepInEx.cfg: {ex}");
            }
        }

        private void Awake()
        {
            EnsureHideManagerGameObjectDisabled();
            Logger.LogInfo("Berry God's Blessing loaded.");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "GameScene")
            {
                StartCoroutine(DelayedBlessingCheck());
            }
        }

        IEnumerator DelayedBlessingCheck()
        {
            // Wait for scene objects to finish spawning
            yield return new WaitForSeconds(1.0f);

            currentBerryCount = CountBerries();

            // Scale chance: 1% at <=10 berries, 50% at >=100 berries
            float t = Mathf.InverseLerp(10f, 100f, currentBerryCount);
            float chance = Mathf.Lerp(0.01f, 0.50f, t);

            Logger.LogInfo($"Berry count = {currentBerryCount}, Blessing chance = {chance * 100}%");

            float roll = (float)rng.NextDouble();
            Logger.LogInfo($"Roll = {roll}");

            if (roll <= chance)
            {
                IBlessing chosen = RollRandomBlessing();
                Logger.LogInfo($"A blessing has occurred! The gods grant: {chosen.Name}");
                ApplyBlessing(chosen);
            }
            else
            {
                Logger.LogInfo("No blessing occurred this time.");
            }
        }

        IEnumerator WaitForDialogueToClose(Action onClose)
        {
            // Wait until the dialogue canvas is gone or disabled
            while (true)
            {
                var dialogueCanvas = GameObject.Find("Canvas/Dialogues");

                if (dialogueCanvas == null || !dialogueCanvas.activeInHierarchy)
                {
                    onClose?.Invoke();
                    yield break;
                }

                yield return null;
            }
        }

        public void OpenDialogue(String dialogueText)
        {
            DialogueManager manager = DialogueManager.Instance;
            if (manager == null)
            {
                Logger.LogError("DialogueManager.Instance is null");
            }
            GameManager.Instance.StartForcedPause();
            UIManager.Instance.DisplaySkipControls();
            Actor hermitActor = manager.GetActor(Actors.Hermit);
            manager.dialogueBox.Init(dialogueText, hermitActor, true, true);
            StartCoroutine(WaitForDialogueToClose(() =>
            {
                GameManager.Instance.EndForcedPause();
                UIManager.Instance.HideSkipControls();
            }));
        }


        private int CountBerries()
        {
            ResourceDepositBerry[] berries = GameObject.FindObjectsOfType<ResourceDepositBerry>();
            Logger.LogInfo($"Found {berries.Length} berries");
            return berries.Length;
        }

        private IBlessing RollRandomBlessing()
        {
            var eligible = BlessingList.All
                .Where(b => b.CanApply())
                .ToList();

            if (eligible.Count == 0)
            {
                Logger.LogWarning("No eligible blessings! All conditions failed.");
                return null;
            }

            int index = rng.Next(eligible.Count);
            return eligible[index];
        }

        private void ApplyBlessing(IBlessing blessing)
        {
            if (blessing == null)
            {
                Logger.LogInfo("Blessing was null — no valid blessings available.");
                return;
            }

            Logger.LogInfo($"Applying blessing: {blessing.Name}");
            OpenDialogue(blessing.Message);


            var rm = DroppedShardSpawner.Instance;
            blessing.Apply();
            return;
        }
    }
    public interface IBlessing
    {
        string Name { get; }
        string Message { get; }
        void Apply();
        bool CanApply();
    }

    public class Blessing<T> : IBlessing
    {
        public string name;
        public string message;
        public T requiredType;
        public Action onApply;
        public Func<bool> condition;

        public string Name => name;
        public Type TargetType => typeof(T);
        public string Message => message;

        public Blessing(string name, string message, Action onApply = null, Func<bool> condition = null)
        {
            this.name = name;
            this.message = message;
            this.onApply = onApply;
            this.condition = condition;
        }

        public void Apply()
        {
            this.onApply.Invoke();
        }

        public bool CanApply()
        {
            return condition == null || condition.Invoke();
        }
    }

    public static class BlessingList
    {
        public static Blessing<DroppedShardSpawner> GoldBlessing = new Blessing<DroppedShardSpawner>("Gold",
            "The berry gods have blessed your kingdom with a golden bounty. Some of your bushes have sprouted golden berries.",
            () => DroppedShardSpawner.Instance.Spawn(new Vector3(0f, 0f, 0f), 10, ResourceType.Gold)
        );
        public static Blessing<DroppedShardSpawner> FaithBlessing = new Blessing<DroppedShardSpawner>("Faith",
            "The berry gods have blessed your kingdom with their holy gaze. Your kingdom's faith has been bolstered.",
            () => DroppedShardSpawner.Instance.Spawn(new Vector3(0f, 0f, 0f), 10, ResourceType.Faith)
        );
        public static Blessing<WallManager> WallBlessing = new Blessing<WallManager>("Wall", 
            "The berry gods have blessed your kingdom with fortifications. Vines have sprouted from the earth entertwining with your walls.",
            () => WallManager.Instance.AddToHitpointMultiplier(0.1f),
            condition: () => WallManager.Instance.m_MaxHitpoints > 0
        );


        // Lazy-loaded blessing cache
        private static List<IBlessing> _all;
        public static List<IBlessing> All
        {
            get
            {
                if (_all == null)
                {
                    _all = typeof(BlessingList)
                        .GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Where(f => typeof(IBlessing).IsAssignableFrom(f.FieldType))
                        .Select(f => (IBlessing)f.GetValue(null))
                        .ToList();
                }
                return _all;
            }
        }

        // Optional helpers:
        public static IBlessing GetByName(string name)
            => All.FirstOrDefault(b => b.Name == name);
    }
}
