using System;
using System.Collections;
using System.Collections.Generic;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.BotBarModule.BotBarScreenModule.Entities;
using Modules.BotBarModule.Shared.Data.UnityObjects;
using Modules.BotBarModule.Shared.Data.ValueObjects;
using Modules.BotBarModule.Shared.Enums;
using UnityEngine;

namespace Modules.BotBarModule.BotBarScreenModule.ViewsMediators
{
    /// <summary>
    /// The bar: as many tabs as it is handed, a highlight that moves under the selected one, and
    /// a slide off the bottom edge. The show animation is the slide in; the hide animation the
    /// slide out. It holds no rule - which tab may be selected was decided before anything here
    /// is called.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public class BotBarScreenView : ScreenView
    {
        [SerializeField] private RectTransform _bar;
        [SerializeField] private RectTransform _tabsParent;
        [SerializeField] private BotBarTabEntity _tabPrefab;
        [SerializeField] private RectTransform _highlight;

        private readonly Dictionary<string, BotBarTabEntity> _tabs = new();
        private BotBarOptionsCVO _options = new();
        private Coroutine _slide;
        private Coroutine _highlightPop;

        public Action<string> TabTapped;

        public int TabCount => _tabs.Count;

        public bool IsBarShown { get; private set; } = true;

        // Off screen by the bar's height, or by however far a raised tab and its badge reach above
        // the bar - a hidden bar shows nothing at all.
        private float _hiddenY
        {
            get
            {
                Bounds children = RectTransformUtility.CalculateRelativeRectTransformBounds(_bar);
                return -Mathf.Max(_bar.sizeDelta.y, children.max.y);
            }
        }

        public bool TryGetTab(string key, out BotBarTabEntity tab) => _tabs.TryGetValue(key ?? string.Empty, out tab);

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();

            // The same instance comes back from the pool: nothing may still be moving, and the bar
            // starts off screen so the show animation has somewhere to come from.
            StopAllCoroutines();
            _slide = null;
            _highlightPop = null;
            SetBarY(_hiddenY);
        }

        public void Build(IReadOnlyList<BotBarTabRVO> tabs, CD_BotBar config)
        {
            Clear();
            _options = config.Options ?? new BotBarOptionsCVO();

            foreach (BotBarTabRVO tab in tabs)
            {
                BotBarTabCVO authored = Find(config, tab.Key);
                if (authored == null) continue;

                BotBarTabEntity entity = Instantiate(_tabPrefab, _tabsParent);
                entity.name = "Tab_" + tab.Key;
                entity.Fill(authored, _options);
                entity.Tapped += OnTabTapped;
                _tabs[tab.Key] = entity;
            }

            _highlight.gameObject.SetActive(false);
        }

        /// <summary>The selection as it stands, no animation: what the opening Command calls.</summary>
        public void ShowSelected(string key)
        {
            foreach (KeyValuePair<string, BotBarTabEntity> pair in _tabs)
                pair.Value.ShowSelected(pair.Key == key, animate: false);

            if (_tabs.TryGetValue(key ?? string.Empty, out BotBarTabEntity selected))
                PlaceHighlight(selected, animate: false);
        }

        public void ShowSelection(string previous, string current)
        {
            if (_tabs.TryGetValue(previous ?? string.Empty, out BotBarTabEntity before))
                before.ShowSelected(false, animate: true);

            if (!_tabs.TryGetValue(current ?? string.Empty, out BotBarTabEntity after))
                return;

            after.ShowSelected(true, animate: true);
            PlaceHighlight(after, animate: true);
        }

        public void ShowTabState(string key, BotBarTabState state)
        {
            if (_tabs.TryGetValue(key ?? string.Empty, out BotBarTabEntity tab))
                tab.ShowState(state);
        }

        public void ShowBadge(string key, int count)
        {
            if (_tabs.TryGetValue(key ?? string.Empty, out BotBarTabEntity tab))
                tab.ShowBadge(count);
        }

        /// <summary>Slides the bar on or off. The screen stays open either way.</summary>
        public void ShowBar(bool shown, bool animate)
        {
            IsBarShown = shown;
            Slide(shown, animate, null);
        }

        protected override void PlayShowAnimation()
        {
            IsBarShown = true;
            Slide(true, animate: true, () => ShowCompleted?.Invoke(this));
        }

        protected override void PlayHideAnimation()
        {
            Slide(false, animate: true, () => HideCompleted?.Invoke(this));
        }

        private void OnTabTapped(string key) => TabTapped?.Invoke(key);

        private void Clear()
        {
            foreach (BotBarTabEntity entity in _tabs.Values)
            {
                entity.Tapped -= OnTabTapped;
                if (_highlight.parent == entity.transform)
                    _highlight.SetParent(_bar, false);

                // Destroy is refused outside play mode, and the edit-mode tests build twice.
                if (Application.isPlaying)
                    Destroy(entity.gameObject);
                else
                    DestroyImmediate(entity.gameObject);
            }

            _tabs.Clear();
        }

        private static BotBarTabCVO Find(CD_BotBar config, string key)
        {
            foreach (BotBarTabCVO tab in config.Tabs)
            {
                if (tab.Key == key)
                    return tab;
            }

            return null;
        }

        private void PlaceHighlight(BotBarTabEntity tab, bool animate)
        {
            if (!_options.MoveHighlight)
            {
                _highlight.gameObject.SetActive(false);
                return;
            }

            // Under the tab as its first child, so it follows the tab while the layout slides.
            _highlight.SetParent(tab.transform, false);
            _highlight.SetAsFirstSibling();
            _highlight.anchorMin = Vector2.zero;
            _highlight.anchorMax = Vector2.one;
            _highlight.offsetMin = Vector2.zero;
            _highlight.offsetMax = Vector2.zero;
            _highlight.gameObject.SetActive(true);

            if (_highlightPop != null)
            {
                StopCoroutine(_highlightPop);
                _highlightPop = null;
            }

            if (!animate || !isActiveAndEnabled || _options.Duration <= 0f)
            {
                _highlight.localScale = Vector3.one;
                return;
            }

            _highlightPop = StartCoroutine(PopHighlight());
        }

        private IEnumerator PopHighlight()
        {
            for (float t = 0f; t < 1f; t += Time.deltaTime / _options.Duration)
            {
                float k = _options.Curve.Evaluate(Mathf.Clamp01(t));
                _highlight.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, k);
                yield return null;
            }

            _highlight.localScale = Vector3.one;
            _highlightPop = null;
        }

        private void Slide(bool shown, bool animate, Action onDone)
        {
            float target = shown ? 0f : _hiddenY;

            if (_slide != null)
            {
                StopCoroutine(_slide);
                _slide = null;
            }

            if (!animate || !isActiveAndEnabled || _options.HideDuration <= 0f)
            {
                SetBarY(target);
                onDone?.Invoke();
                return;
            }

            _slide = StartCoroutine(SlideTo(target, onDone));
        }

        private IEnumerator SlideTo(float target, Action onDone)
        {
            float from = _bar.anchoredPosition.y;

            for (float t = 0f; t < 1f; t += Time.deltaTime / _options.HideDuration)
            {
                float k = _options.HideCurve.Evaluate(Mathf.Clamp01(t));
                SetBarY(Mathf.LerpUnclamped(from, target, k));
                yield return null;
            }

            SetBarY(target);
            _slide = null;
            onDone?.Invoke();
        }

        private void SetBarY(float y) => _bar.anchoredPosition = new Vector2(_bar.anchoredPosition.x, y);
    }
}