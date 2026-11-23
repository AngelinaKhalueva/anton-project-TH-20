using UnityEngine;
using UnityEngine.UI;

namespace AntonDoors
{
    public class HudController : MonoBehaviour
    {
        private Text hpText;
        private Text timeText;
        private Text promptText;
        private Text messageText;
        private Text endTitle;
        private Text endSubtitle;
        private Image damageFlash;
        private GameObject endRoot;

        private float messageTimer;
        private float flashTimer;

        public void Build(Font font)
        {
            GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            Canvas canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            hpText = MakeText(canvasGo.transform, "HP", font, 34, TextAnchor.UpperLeft, new Color(1f, 0.4f, 0.4f));
            SetRect(hpText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(32f, -28f), new Vector2(420f, 60f));

            timeText = MakeText(canvasGo.transform, "Timer", font, 40, TextAnchor.UpperCenter, Color.white);
            SetRect(timeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -26f), new Vector2(420f, 70f));

            Text hint = MakeText(canvasGo.transform, "Hint", font, 22, TextAnchor.UpperCenter, new Color(0.65f, 0.68f, 0.75f));
            hint.text = "WASD — движение   |   Shift — бег   |   Esc — курсор";
            SetRect(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -80f), new Vector2(1000f, 50f));

            Text crosshair = MakeText(canvasGo.transform, "Crosshair", font, 30, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.65f));
            crosshair.text = "+";
            SetRect(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(40f, 40f));

            promptText = MakeText(canvasGo.transform, "Prompt", font, 28, TextAnchor.LowerCenter, new Color(1f, 0.9f, 0.5f));
            SetRect(promptText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 90f), new Vector2(800f, 60f));

            messageText = MakeText(canvasGo.transform, "Message", font, 44, TextAnchor.MiddleCenter, Color.white);
            SetRect(messageText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 130f), new Vector2(1200f, 80f));
            messageText.gameObject.SetActive(false);

            damageFlash = MakeImage(canvasGo.transform, "DamageFlash", new Color(1f, 0f, 0f, 0f));
            Stretch(damageFlash.rectTransform);
            damageFlash.raycastTarget = false;

            endRoot = new GameObject("EndScreen", typeof(RectTransform));
            endRoot.transform.SetParent(canvasGo.transform, false);
            Image endBg = endRoot.AddComponent<Image>();
            endBg.color = new Color(0f, 0f, 0f, 0.88f);
            endBg.raycastTarget = false;
            Stretch(endBg.rectTransform);

            endTitle = MakeText(endRoot.transform, "Title", font, 92, TextAnchor.MiddleCenter, Color.white);
            SetRect(endTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 90f), new Vector2(1400f, 160f));

            endSubtitle = MakeText(endRoot.transform, "Subtitle", font, 38, TextAnchor.MiddleCenter, new Color(0.85f, 0.85f, 0.9f));
            SetRect(endSubtitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -40f), new Vector2(1400f, 220f));

            endRoot.SetActive(false);
        }

        private void Update()
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f && messageText != null) messageText.gameObject.SetActive(false);
            }

            if (flashTimer > 0f && damageFlash != null)
            {
                flashTimer -= Time.deltaTime;
                Color c = damageFlash.color;
                c.a = Mathf.Clamp01(flashTimer / 0.35f) * 0.45f;
                damageFlash.color = c;
            }
        }

        public void SetHp(int hp, int max)
        {
            if (hpText == null) return;
            hpText.text = "HP  " + hp + " / " + max;
            float t = Mathf.Clamp01((float)hp / max);
            hpText.color = Color.Lerp(new Color(1f, 0.12f, 0.12f), new Color(0.35f, 1f, 0.5f), t);
        }

        public void SetTime(float seconds, float limit)
        {
            if (timeText == null) return;
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            timeText.text = string.Format("{0:00}:{1:00}", s / 60, s % 60);
            timeText.color = seconds < 30f ? new Color(1f, 0.25f, 0.25f) : Color.white;
        }

        public void SetPrompt(string text)
        {
            if (promptText != null) promptText.text = text;
        }

        public void ShowMessage(string text, Color color)
        {
            if (messageText == null) return;
            messageText.text = text;
            messageText.color = color;
            messageText.gameObject.SetActive(true);
            messageTimer = 2f;
        }

        public void FlashDamage()
        {
            flashTimer = 0.35f;
        }

        public void ShowEnd(string title, string subtitle, bool win)
        {
            if (endRoot == null) return;
            endTitle.text = title;
            endTitle.color = win ? new Color(0.35f, 1f, 0.55f) : new Color(1f, 0.3f, 0.3f);
            endSubtitle.text = subtitle + "\n\n[R] начать заново";
            endRoot.SetActive(true);
        }

        public void HideEnd()
        {
            if (endRoot != null) endRoot.SetActive(false);
        }

        private static Text MakeText(Transform parent, string name, Font font, int size, TextAnchor anchor, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            Text t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private static Image MakeImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            return img;
        }

        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
