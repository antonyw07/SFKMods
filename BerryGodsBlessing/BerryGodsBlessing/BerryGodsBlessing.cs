using BepInEx;
using SuperFantasyKingdom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

namespace BerryGodsBlessing
{
    [BepInPlugin("com.low.berrygodsblessing", "Berry God's Blessing", "1.0.0")]
    public class BerryGodPlugin : BaseUnityPlugin
    {
        int currentBerryCount = 0;
        System.Random rng = new System.Random();

        private void Awake()
        {
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

            float chance = Mathf.Clamp(currentBerryCount / 100f * 0.10f, 0.01f, 0.10f);

            Logger.LogInfo($"Berry count = {currentBerryCount}, Blessing chance = {chance * 100}%");

            // Roll for blessing
            float roll = (float)rng.NextDouble();
            Logger.LogInfo($"Roll = {roll}");

            //if (roll <= chance)
            if (true)
            {
                IBlessing chosen = RollRandomBlessing();
                Logger.LogInfo($"✨ A blessing has occurred! The gods grant: {chosen.Name}");

                ApplyBlessing(chosen);
            }
            else
            {
                Logger.LogInfo("No blessing occurred this time.");
            }
        }

        private int CountBerries()
        {
            ResourceDepositBerry[] berries = GameObject.FindObjectsOfType<ResourceDepositBerry>();
            Logger.LogInfo($"Found {berries.Length} berries");
            return berries.Length;
        }

        private IBlessing RollRandomBlessing()
        {
            var all = BlessingList.All;
            int index = rng.Next(all.Count);
            return all[index];
        }

        private void ApplyBlessing(IBlessing blessing)
        {
            Logger.LogInfo($"Applying blessing: {blessing.Name}");
            PopupManager.Show(blessing.Message);

            if (blessing.TargetType == typeof(ResourceManager))
            {
                var rm = ResourceManager.Instance;
                var typed = (Blessing<ResourceManager>)blessing;
                typed.onApply?.Invoke(rm);
                return;
            }

            if (blessing.TargetType == typeof(FaithManager))
            {
                var fm = FaithManager.Instance;
                var typed = (Blessing<FaithManager>)blessing;
                typed.onApply?.Invoke(fm);
                return;
            }

            if (blessing.TargetType == typeof(WallManager))
            {
                var wm = WallManager.Instance;
                var typed = (Blessing<WallManager>)blessing;
                typed.onApply?.Invoke(wm);
                return;
            }
        }
    }
    public interface IBlessing
    {
        string Name { get; }
        System.Type TargetType { get; }
        string Message { get; }
    }

    public class Blessing<T> : IBlessing
    {
        public string name;
        public string message;
        public T requiredType;
        public Action<T> onApply;

        public string Name => name;
        public Type TargetType => typeof(T);
        public string Message => message;

        public Blessing(string name, string message, Action<T> onApply = null)
        {
            this.name = name;
            this.message = message;
            this.onApply = onApply;
        }
    }

    public static class BlessingList
    {
        public static Blessing<ResourceManager> GoldBlessing = new Blessing<ResourceManager>("Gold", 
            "The berry gods have blessed your kingdom with a golden bounty. Some of your bushes have sprouted golden berries.",
            rm => { rm.AddToCoins(10);  });
        public static Blessing<FaithManager> FaithBlessing = new Blessing<FaithManager>("Faith",
            "The berry gods have blessed your kingdom with their holy gaze. Your kingdom's faith has been bolstered.",
            fm => { fm.AddFaith(10); });
        public static Blessing<WallManager> WallBlessing = new Blessing<WallManager>("Wall", 
            "The berry gods have blessed your kingdom with fortifications. Vines have sprouted from the earth entertwining with your walls.",
            wm => { wm.AddToHitpointMultiplier(0.1f); });


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

        public static IEnumerable<IBlessing> GetByManagerType<T>()
            => All.Where(b => b.TargetType == typeof(T));
    }
}
