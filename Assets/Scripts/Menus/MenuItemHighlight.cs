using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BopBeat.Menus
{
    /// <summary>Mouse hover and keyboard focus share a small visual enlargement, with a stable click area.</summary>
    public sealed class MenuItemHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField] private RectTransform visual;
        [SerializeField, Range(1, 1.2f)] private float enlargedScale = 1.06f;
        private bool hovered, selected;

        public RectTransform Visual => visual;
        private void OnEnable()
        {
            hovered = selected = false;
            if (visual != null) visual.localScale = Vector3.one;
        }
        private void Update()
        {
            if (visual == null) return;
            float scale = hovered || selected ? enlargedScale : 1;
            visual.localScale = Vector3.Lerp(visual.localScale, Vector3.one * scale, 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            GetComponent<Button>().Select();
        }
        public void OnPointerExit(PointerEventData eventData) => hovered = false;
        public void OnSelect(BaseEventData eventData) => selected = true;
        public void OnDeselect(BaseEventData eventData) => selected = false;
    }
}
