using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TabStyle
{
    /// <summary>
    /// Shows the full card when you hover a card chip, the same way TabInfo does
    /// (same card-holder template, same canvas, same scale), so it looks identical.
    /// Only one preview exists at a time.
    /// </summary>
    internal static class CardPreview
    {
        private static GameObject current;
        private static CardChip owner;

        internal static void Show(CardChip chip)
        {
            Hide();
            if (chip == null || chip.Card == null) return;

            var canvas = TabStyleUI.Instance != null ? TabStyleUI.Instance.CanvasObject : null;
            if (canvas == null) return;

            try
            {
                var template = TabInfoBridge.CardHolderTemplate;
                GameObject holder;
                if (template != null)
                {
                    holder = Object.Instantiate(template, canvas.transform);
                }
                else
                {
                    holder = new GameObject("TabStyle Card Holder", typeof(RectTransform));
                    holder.transform.SetParent(canvas.transform, false);
                }
                holder.name = "TabStyle Card Preview";
                holder.transform.SetAsLastSibling();
                holder.transform.position = chip.transform.position;

                // The preview must never steal the mouse, or hovering would flicker on and off.
                var group = holder.GetComponent<CanvasGroup>() ?? holder.AddComponent<CanvasGroup>();
                group.blocksRaycasts = false;
                group.interactable = false;
                foreach (var g in holder.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

                var cardObj = Object.Instantiate(chip.Card.gameObject, holder.transform);
                cardObj.transform.localPosition = Vector3.zero;

                var vis = cardObj.GetComponentInChildren<CardVisuals>();
                if (vis != null) vis.firstValueToSet = true;

                foreach (var col in holder.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;

                var cardCanvas = cardObj.GetComponentInChildren<Canvas>();
                if (cardCanvas != null) cardCanvas.sortingLayerName = "MostFront";
                foreach (var ray in holder.GetComponentsInChildren<GraphicRaycaster>(true)) ray.enabled = false;
                foreach (var g in holder.GetComponentsInChildren<Graphic>(true)) g.raycastTarget = false;

                var scaler = cardObj.GetComponentInChildren<SetScaleToZero>();
                if (scaler != null)
                {
                    scaler.enabled = false;
                    scaler.transform.localScale = Vector3.one * 1.15f;
                }

                current = holder;
                owner = chip;
                TabStyleUI.Instance.StartCoroutine(ScaleNextFrame(holder, cardObj));
            }
            catch (System.Exception e)
            {
                TabStyle.Log.LogWarning("Card preview failed: " + e.Message);
                Hide();
            }
        }

        private static IEnumerator ScaleNextFrame(GameObject holder, GameObject cardObj)
        {
            yield return null;
            if (holder != null && cardObj != null && current == holder)
                cardObj.transform.localScale = Vector3.one * 25f;
        }

        internal static void Hide(CardChip onlyIfOwner = null)
        {
            if (onlyIfOwner != null && owner != onlyIfOwner) return;
            if (current != null) Object.Destroy(current);
            current = null;
            owner = null;
        }
    }

    internal class CardChip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal CardInfo Card;

        public void OnPointerEnter(PointerEventData e) => CardPreview.Show(this);
        public void OnPointerExit(PointerEventData e) => CardPreview.Hide(this);
        private void OnDisable() => CardPreview.Hide(this);
        private void OnDestroy() => CardPreview.Hide(this);
    }
}
