using System;
using System.Collections;
using Modules.BotBarModule.Shared.Data.ValueObjects;
using Modules.BotBarModule.Shared.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.BotBarModule.BotBarScreenModule.Entities
{
    /// <summary>
    /// One tab on the bar: scene references, raw input, and the look of one selection. It applies
    /// only the options that are on; an option that is off leaves that value alone.
    /// </summary>
    public class BotBarTabEntity : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private LayoutElement _layout;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _title;
        [SerializeField] private GameObject _lockedGroup;
        [SerializeField] private Text _lockedLabel;
        [SerializeField] private GameObject _newMark;
        [SerializeField] private GameObject _badge;
        [SerializeField] private Text _badgeCount;

        private BotBarTabCVO _tab;
        private BotBarOptionsCVO _options = new();
        private Coroutine _tween;

        public Action<string> Tapped;

        public string Key => _tab?.Key;

        public bool IsSelected { get; private set; }

        public float Scale => _content.localScale.x;

        public float Raise => _content.anchoredPosition.y;

        public float Width => _layout.flexibleWidth;

        private void OnEnable() => _button.onClick.AddListener(OnClick);

        private void OnDisable() => _button.onClick.RemoveListener(OnClick);

        private void OnClick() => Tapped?.Invoke(Key);

        public void Fill(BotBarTabCVO tab, BotBarOptionsCVO options)
        {
            _tab = tab;
            _options = options ?? new BotBarOptionsCVO();
            _title.text = tab.Title ?? string.Empty;
            _lockedLabel.text = tab.LockedLabel ?? string.Empty;
            _lockedLabel.gameObject.SetActive(!string.IsNullOrEmpty(tab.LockedLabel));
            ShowState(BotBarTabState.Open);
            ShowBadge(0);
            ShowSelected(false, animate: false);
        }

        public void ShowSelected(bool selected, bool animate)
        {
            IsSelected = selected;
            _icon.sprite = selected && _tab.SelectedIcon != null ? _tab.SelectedIcon : _tab.IdleIcon;
            _title.gameObject.SetActive(_options.Titles == BotBarTitleMode.Always
                                        || (_options.Titles == BotBarTitleMode.SelectedOnly && selected));

            float scale = selected && _options.ScaleSelected ? _options.SelectedScale : 1f;
            float raise = selected && _options.RaiseSelected ? _options.SelectedRaise : 0f;
            float width = selected && _options.WidenSelected ? _options.SelectedWidth : 1f;

            if (_tween != null)
            {
                StopCoroutine(_tween);
                _tween = null;
            }

            if (!animate || !isActiveAndEnabled || _options.Duration <= 0f)
            {
                Apply(scale, raise, width);
                return;
            }

            _tween = StartCoroutine(Tween(scale, raise, width));
        }

        public void ShowState(BotBarTabState state)
        {
            _lockedGroup.SetActive(state == BotBarTabState.Locked);
            _newMark.SetActive(state == BotBarTabState.NewlyUnlocked);
        }

        public void ShowBadge(int count)
        {
            _badge.SetActive(count > 0);
            _badgeCount.text = count > 99 ? "99+" : count.ToString();
        }

        private void Apply(float scale, float raise, float width)
        {
            _content.localScale = new Vector3(scale, scale, 1f);
            _content.anchoredPosition = new Vector2(_content.anchoredPosition.x, raise);
            _layout.flexibleWidth = width;
        }

        private IEnumerator Tween(float scale, float raise, float width)
        {
            float fromScale = Scale, fromRaise = Raise, fromWidth = Width;

            for (float t = 0f; t < 1f; t += Time.deltaTime / _options.Duration)
            {
                float k = _options.Curve.Evaluate(Mathf.Clamp01(t));
                Apply(Mathf.LerpUnclamped(fromScale, scale, k),
                    Mathf.LerpUnclamped(fromRaise, raise, k),
                    Mathf.LerpUnclamped(fromWidth, width, k));
                yield return null;
            }

            Apply(scale, raise, width);
            _tween = null;
        }
    }
}
