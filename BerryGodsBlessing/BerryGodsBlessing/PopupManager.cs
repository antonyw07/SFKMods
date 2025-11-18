using UnityEngine;
using UnityEngine.UI;
using System;

namespace BerryGodsBlessing
{
    public class PopupManager : MonoBehaviour
    {
        private static PopupManager instance;

        private GameObject popupRoot;
        private Text popupText;

        public static void Show(string message)
        {
            if (instance == null)
                instance = CreatePopupManager();

            instance.ShowPopup(message);
        }

        private static PopupManager CreatePopupManager()
        {
            GameObject manager = new GameObject("PopupManager");
            DontDestroyOnLoad(manager);
            return manager.AddComponent<PopupManager>();
        }

        private void ShowPopup(string message)
        {
            EnsurePopupUICreated();

            popupText.text = message;
            popupRoot.SetActive(true);
        }

        private void EnsurePopupUICreated()
        {
            if (popupRoot != null)
                return;

            // Find existing Canvas
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                Debug.LogError("[PopupManager] Could not find Canvas!");
                return;
            }

            // Background overlay
            popupRoot = new GameObject("BlessingPopup");
            popupRoot.transform.SetParent(canvasObj.transform, false);
            popupRoot.AddComponent<CanvasRenderer>();

            Image bg = popupRoot.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);

            RectTransform bgRect = popupRoot.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Panel
            GameObject panel = new GameObject("Panel");
            panel.transform.SetParent(popupRoot.transform, false);

            Image panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(360, 200);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            // Text
            GameObject textGO = new GameObject("Message");
            textGO.transform.SetParent(panel.transform, false);

            popupText = textGO.AddComponent<Text>();
            popupText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            popupText.fontSize = 14;
            popupText.color = Color.white;
            popupText.alignment = TextAnchor.MiddleCenter;
            popupText.horizontalOverflow = HorizontalWrapMode.Wrap;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.1f, 0.25f);
            textRect.anchorMax = new Vector2(0.9f, 0.85f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            // Close Button
            GameObject buttonGO = new GameObject("CloseButton");
            buttonGO.transform.SetParent(panel.transform, false);

            Button btn = buttonGO.AddComponent<Button>();
            Image btnImg = buttonGO.AddComponent<Image>();
            btnImg.color = new Color(0.3f, 0.3f, 0.3f);

            Text btnText = new GameObject("Text").AddComponent<Text>();
            btnText.transform.SetParent(buttonGO.transform, false);
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 20;
            btnText.color = Color.white;
            btnText.text = "Close";
            btnText.alignment = TextAnchor.MiddleCenter;

            RectTransform btnRect = buttonGO.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(120, 50);
            btnRect.anchorMin = new Vector2(0.5f, 0.1f);
            btnRect.anchorMax = new Vector2(0.5f, 0.1f);
            btnRect.anchoredPosition = Vector2.zero;

            RectTransform btnTextRect = btnText.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            btn.onClick.AddListener(() =>
            {
                popupRoot.SetActive(false);
            });

            popupRoot.SetActive(false);
        }
    }
}
