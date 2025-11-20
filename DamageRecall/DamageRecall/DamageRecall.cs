using BepInEx;
using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DamageRecall
{
    [BepInPlugin("com.low.damagerecall", "Damage Recall", "0.1.1")]
    public class DamageRecallPlugin : BaseUnityPlugin
    {
        private static GameObject popupRoot;  // only created once
        private GameObject persistentDamageMeter;
        private bool isVisible = false;
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
            Logger.LogInfo("Damage Recall loaded.");
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (scene.name)
            {
                case "GameScene":
                    persistentDamageMeter = GameObject.Find("PersistedDamageMeter");
                    if (persistentDamageMeter != null)
                    {
                        persistentDamageMeter.SetActive(false);

                        var canvas = GameObject.Find("Canvas");
                        if (canvas != null)
                        {
                            persistentDamageMeter.transform.SetParent(canvas.transform, false);
                            Logger.LogInfo("Reattached PersistedDamageMeter to GameScene Canvas.");
                        }
                    }
                    break;
                case "HumanTavernScene":
                    StartCoroutine(ExecuteAfterDelay());
                    break;
                case "TitleScene":
                    CreatePopup();
                    break;
                default:
                    break;
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.name == "GameScene")
            {
                persistentDamageMeter = GameObject.Find("PersistedDamageMeter");

                if (persistentDamageMeter != null)
                {
                    persistentDamageMeter.SetActive(false);
                    GameObject.Destroy(persistentDamageMeter);
                }
            }
        }

        private void Update()
        {
            // Toggle meter on/off when pressing ~ (backquote)
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                if (SceneManager.GetActiveScene().name == "GameScene")
                {
                    if (persistentDamageMeter == null)
                    {
                        Logger.LogInfo("No persisted damage meter found");
                    } else {
                        isVisible = !isVisible;
                        persistentDamageMeter.SetActive(isVisible);
                        Logger.LogInfo($"PersistedDamageMeter visibility toggled: {isVisible}");
                    }
                }
            }
        }

        private void CopyDamageMeter()
        {
            var toCopy = Instantiate(GameObject.Find("TavernMaster/Canvas/Plate/Statistics/DamageMeterContainer/DamageMeter"));
            toCopy.name = "PersistedDamageMeter";
            toCopy.transform.position = new Vector3(-440f, -320f, 0f);
            Scene targetScene = SceneManager.GetSceneByName("PersistentScene");
            SceneManager.MoveGameObjectToScene(toCopy, targetScene);
        }

        IEnumerator ExecuteAfterDelay()
        {
            yield return new WaitForSeconds(1);
            CopyDamageMeter();
        }

        private void CreatePopup()
        {
            // Find the game's canvas
            GameObject canvasObj = GameObject.Find("Canvas");
            if (!canvasObj)
            {
                Logger.LogWarning("Canvas not found in scene!");
                return;
            }

            // If popup was already created, just re-parent it
            if (popupRoot != null)
            {
                popupRoot.transform.SetParent(canvasObj.transform, false);
                return;
            }

            // === CREATE IT FOR THE FIRST TIME ===
            popupRoot = new GameObject("ModPopup");
            popupRoot.transform.SetParent(canvasObj.transform, false);

            RectTransform rootRT = popupRoot.AddComponent<RectTransform>();
            rootRT.sizeDelta = new Vector2(400, 200);
            rootRT.anchoredPosition = Vector2.zero;

            // Background Panel
            Image bg = popupRoot.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.75f);

            // Text
            var textGO = new GameObject("PopupText");
            textGO.transform.SetParent(popupRoot.transform, false);
            Text text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = "Damage Recall loaded.\n\nDamage from previous played day can now be opened and closed using the ~ key.";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.sizeDelta = new Vector2(380, 120);
            textRT.anchoredPosition = new Vector2(0, 40);

            // Close Button
            var buttonGO = new GameObject("CloseButton");
            buttonGO.transform.SetParent(popupRoot.transform, false);

            Image btnImage = buttonGO.AddComponent<Image>();
            btnImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            Button btn = buttonGO.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                popupRoot.SetActive(false);  // hide but don't destroy
            });

            RectTransform btnRT = buttonGO.GetComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(120, 40);
            btnRT.anchoredPosition = new Vector2(0, -60);

            // Button label
            var bTextGO = new GameObject("ButtonText");
            bTextGO.transform.SetParent(buttonGO.transform, false);

            Text btnText = bTextGO.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.text = "Close";
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;

            RectTransform bTextRT = bTextGO.GetComponent<RectTransform>();
            bTextRT.sizeDelta = btnRT.sizeDelta;

            Logger.LogInfo("Popup successfully created and attached!");
        }
    }
}
