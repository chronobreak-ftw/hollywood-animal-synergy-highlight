using UnityEngine;
using UnityEngine.UI;

namespace SynergyHighlightMod
{
    public static class SynergyOverlay
    {
        private const string OVERLAY_NAME = "__SynergyOverlay__";
        private const string BORDER_NAME = "__SynergyBorder__";
        private const string TINT_NAME = "__AdModalTint__";
        private const float BORDER_ALPHA = 0.90f;
        private const float BORDER_THICKNESS = 3f;

        public const float OverlayAlphaGenre = 0.25f;
        public const float OverlayAlphaSetting = 0.10f;
        public const float OverlayAlphaContent = 0.35f;
        public const float OverlayAlphaAdModal = 0.10f;

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
        private static readonly Color ColorGreen = new Color(0.10f, 0.90f, 0.20f);
        private static readonly Color ColorYellow = new Color(0.95f, 0.95f, 0.40f);
        private static readonly Color ColorRed = new Color(0.90f, 0.10f, 0.10f);
        private static readonly Color ColorLimeGreen = new Color(0.70f, 1.00f, 0.00f);

        public static Color ScoreToColor(float? score, float overlayAlpha) =>
            BandToColor(SynergyColorBand.FromScore(score), overlayAlpha);

        public static Color GenrePairScoreToColor(float? pairSum, float overlayAlpha) =>
            BandToColor(SynergyColorBand.FromGenrePairScore(pairSum), overlayAlpha);

        public static Color AdRelevanceToColor(float relevance, float overlayAlpha) =>
            BandToColor(SynergyColorBand.FromAdRelevance(relevance), overlayAlpha);

        private static Color BandToColor(ColorBand band, float alpha)
        {
            if (band == ColorBand.Green)
                return new Color(ColorGreen.r, ColorGreen.g, ColorGreen.b, alpha);
            if (band == ColorBand.Yellow)
                return new Color(ColorYellow.r, ColorYellow.g, ColorYellow.b, alpha);
            if (band == ColorBand.Red)
                return new Color(ColorRed.r, ColorRed.g, ColorRed.b, alpha);
            if (band == ColorBand.LimeGreen)
                return new Color(ColorLimeGreen.r, ColorLimeGreen.g, ColorLimeGreen.b, alpha);
            return Clear;
        }

        // ── Shared helper ────────────────────────────────────────────────────────────

        private static Image EnsureOverlayImage(GameObject cardGO, string name)
        {
            Transform existing = cardGO.transform.Find(name);
            if (existing != null)
                return existing.GetComponent<Image>();

            var go = new GameObject(name);
            go.transform.SetParent(cardGO.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsLastSibling();
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        public static void Apply(GameObject cardGO, Color color)
        {
            var img = EnsureOverlayImage(cardGO, OVERLAY_NAME);
            img.color = color;
            img.gameObject.SetActive(color.a > 0.001f);
        }

        public static void ApplyTint(GameObject cardGO, Color color)
        {
            var img = EnsureOverlayImage(cardGO, TINT_NAME);
            img.color = color;
            img.gameObject.SetActive(color.a > 0.001f);
        }

        public static void ApplyBorder(GameObject cardGO, Color baseColor)
        {
            Transform existing = cardGO.transform.Find(BORDER_NAME);
            GameObject container;

            if (existing == null)
            {
                container = new GameObject(BORDER_NAME);
                container.transform.SetParent(cardGO.transform, false);
                var rt = container.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.SetAsLastSibling();

                AddStrip(
                    container,
                    "Top",
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(0, -BORDER_THICKNESS),
                    new Vector2(0, 0)
                );
                AddStrip(
                    container,
                    "Bottom",
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 0),
                    new Vector2(0, BORDER_THICKNESS)
                );
                AddStrip(
                    container,
                    "Left",
                    new Vector2(0, 0),
                    new Vector2(0, 1),
                    new Vector2(0, 0),
                    new Vector2(BORDER_THICKNESS, 0)
                );
                AddStrip(
                    container,
                    "Right",
                    new Vector2(1, 0),
                    new Vector2(1, 1),
                    new Vector2(-BORDER_THICKNESS, 0),
                    new Vector2(0, 0)
                );
            }
            else
            {
                container = existing.gameObject;
            }

            bool visible = baseColor.a > 0.001f;
            container.SetActive(visible);
            if (!visible)
                return;

            var borderColor = new Color(baseColor.r, baseColor.g, baseColor.b, BORDER_ALPHA);
            foreach (Transform strip in container.transform)
            {
                var img = strip.GetComponent<Image>();
                if (img != null)
                    img.color = borderColor;
            }
        }

        private static void AddStrip(
            GameObject parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax
        )
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
        }

        public static void Remove(GameObject cardGO)
        {
            foreach (string name in new[] { OVERLAY_NAME, BORDER_NAME, TINT_NAME })
            {
                Transform t = cardGO.transform.Find(name);
                if (t != null)
                    Object.Destroy(t.gameObject);
            }
        }
    }
}
