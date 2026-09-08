using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class UIManager : MonoBehaviour
    {
        public UIRoot root;
        public BaseUI[] prefabs;
        public UIRoot Root => root;
        public BaseUI ActiveScreen { get; private set; }
        public IReadOnlyList<BaseUI> Popups => readOnlyPopups ?? (readOnlyPopups = popups.AsReadOnly());
        public int CachedCount => cache.Count;

        readonly Dictionary<Type, BaseUI> registrations = new Dictionary<Type, BaseUI>();
        readonly Dictionary<Type, BaseUI> cache = new Dictionary<Type, BaseUI>();
        readonly List<BaseUI> popups = new List<BaseUI>();
        readonly Dictionary<BaseUI, GameObject> previousFocus = new Dictionary<BaseUI, GameObject>();
        ReadOnlyCollection<BaseUI> readOnlyPopups;
        bool initialized, inputLocked;

        public T Show<T>(UIData data) where T : BaseUI
        {
            T view = Resolve<T>(data);
            if (popups.Contains(view))
                throw new InvalidOperationException("An open popup cannot also be the active screen.");
            Bind(view, data);
            if (ActiveScreen == view)
            {
                RefreshInput();
                return view;
            }

            // The new binding succeeds before the previous screen or its popups are closed.
            List<Exception> errors = CloseCurrentViews();
            ActiveScreen = view;
            Reparent(view, root.screenLayer);
            RefreshInput();
            view.Activate();
            ThrowErrors(errors);
            return view;
        }

        public T ShowPopup<T>(UIData data) where T : BaseUI
        {
            T view = Resolve<T>(data);
            if (ActiveScreen == view)
                throw new InvalidOperationException("The active screen cannot also be an open popup.");
            Bind(view, data);
            int existing = popups.IndexOf(view);
            if (existing != popups.Count - 1 || existing < 0)
            {
                GameObject focus = Selection;
                if (existing >= 0)
                {
                    RepairFocusReferences(view, previousFocus[view]);
                    popups.RemoveAt(existing);
                }
                previousFocus[view] = focus;
                popups.Add(view);
            }
            Reparent(view, root.popupLayer);
            RefreshInput();
            view.Activate();
            ClearUnusableSelection();
            return view;
        }

        public bool CloseTopPopup()
        {
            EnsureInitialized();
            if (popups.Count == 0) return false;
            BaseUI view = popups[popups.Count - 1];
            GameObject restore = previousFocus[view];
            popups.RemoveAt(popups.Count - 1);
            previousFocus.Remove(view);
            ClearSelectionIn(view);
            Exception error = CloseToCache(view);
            RefreshInput();
            RestoreFocus(restore);
            if (error != null) throw error;
            return true;
        }

        public void CloseAll()
        {
            // Destruction may call this before any screen has ever been opened.
            if (!initialized) return;
            List<Exception> errors = CloseCurrentViews();
            RefreshInput();
            ThrowErrors(errors);
        }

        public void SetInputLocked(bool locked)
        {
            EnsureInitialized();
            inputLocked = locked;
            RefreshInput();
        }

        public bool TryGetCached<T>(out T view) where T : BaseUI
        {
            if (cache.TryGetValue(typeof(T), out BaseUI cached) && cached)
            {
                view = (T)cached;
                return true;
            }
            view = null;
            return false;
        }

        void EnsureInitialized()
        {
            if (initialized) return;
            if (!root) throw new InvalidOperationException("UIManager requires an authored UIRoot.");
            if (prefabs == null)
                throw new InvalidOperationException("UIManager requires a prefab registration list.");
            var checkedRegistrations = new Dictionary<Type, BaseUI>();
            foreach (BaseUI prefab in prefabs)
            {
                if (!prefab)
                    throw new InvalidOperationException("UI prefab registrations cannot contain null.");
                Type type = prefab.GetType();
                if (type.IsAbstract || checkedRegistrations.ContainsKey(type))
                    throw new InvalidOperationException($"UI prefab type {type.Name} must have one concrete registration.");
                if (!prefab.group || prefab.DataType == null || !typeof(UIData).IsAssignableFrom(prefab.DataType))
                    throw new InvalidOperationException($"UI prefab {type.Name} requires a CanvasGroup and UIData type.");
                checkedRegistrations.Add(type, prefab);
            }
            root.ApplySafeArea();
            foreach (var entry in checkedRegistrations) registrations.Add(entry.Key, entry.Value);
            initialized = true;
            RefreshInput();
        }

        T Resolve<T>(UIData data) where T : BaseUI
        {
            EnsureInitialized();
            if (!registrations.TryGetValue(typeof(T), out BaseUI prefab))
                throw new InvalidOperationException($"UI type {typeof(T).Name} is not registered.");
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (!prefab.DataType.IsInstanceOfType(data))
                throw new ArgumentException($"Expected {prefab.DataType.Name} for {typeof(T).Name}.", nameof(data));
            if (cache.TryGetValue(typeof(T), out BaseUI existing)) return (T)existing;

            // An inactive parent prevents OnEnable even when an authored prefab is active.
            BaseUI instance = Instantiate(prefab, root.cacheLayer, false);
            instance.gameObject.SetActive(false);
            instance.Attach(this);
            cache.Add(typeof(T), instance);
            return (T)instance;
        }

        void Bind(BaseUI view, UIData data)
        {
            try { view.BindData(data); }
            catch
            {
                bool wasTop = popups.Count > 0 && popups[popups.Count - 1] == view;
                GameObject restore = null;
                if (previousFocus.TryGetValue(view, out restore))
                {
                    RepairFocusReferences(view, restore);
                    previousFocus.Remove(view);
                    popups.Remove(view);
                }
                if (ActiveScreen == view) ActiveScreen = null;
                ClearSelectionIn(view);
                Reparent(view, root.cacheLayer);
                RefreshInput();
                if (wasTop) RestoreFocus(restore);
                throw;
            }
        }

        List<Exception> CloseCurrentViews()
        {
            var closing = new List<BaseUI>(popups);
            if (ActiveScreen) closing.Insert(0, ActiveScreen);
            ActiveScreen = null;
            popups.Clear();
            previousFocus.Clear();
            var errors = new List<Exception>();
            for (int i = closing.Count - 1; i >= 0; i--)
            {
                ClearSelectionIn(closing[i]);
                Exception error = CloseToCache(closing[i]);
                if (error != null) errors.Add(error);
            }
            return errors;
        }

        Exception CloseToCache(BaseUI view)
        {
            if (!view) return null;
            try { view.CloseView(); }
            catch (Exception error) { return error; }
            finally { Reparent(view, root.cacheLayer); }
            return null;
        }

        static void Reparent(BaseUI view, RectTransform layer)
        {
            // Keep the prefab's anchors and offsets, including centered popup layouts.
            var rect = view.transform as RectTransform;
            if (!rect) { view.transform.SetParent(layer, false); return; }
            Vector3 position = rect.anchoredPosition3D;
            Vector2 size = rect.sizeDelta;
            rect.SetParent(layer, false);
            rect.anchoredPosition3D = position;
            rect.sizeDelta = size;
        }

        void RefreshInput()
        {
            root.inputGroup.interactable = !inputLocked;
            root.inputGroup.blocksRaycasts = !inputLocked;
            if (ActiveScreen) SetViewInput(ActiveScreen, !inputLocked && popups.Count == 0);
            for (int i = 0; i < popups.Count; i++)
            {
                SetViewInput(popups[i], !inputLocked && i == popups.Count - 1);
                popups[i].transform.SetAsLastSibling();
            }
            root.popupBlocker.gameObject.SetActive(popups.Count > 0);
            if (popups.Count > 0)
            {
                root.popupBlocker.transform.SetAsLastSibling();
                popups[popups.Count - 1].transform.SetAsLastSibling();
            }
        }

        static void SetViewInput(BaseUI view, bool allowed)
        {
            view.group.interactable = allowed;
            view.group.blocksRaycasts = allowed;
        }

        static GameObject Selection => EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;

        static bool BelongsTo(GameObject target, BaseUI view) =>
            target && view && target.transform.IsChildOf(view.transform);

        static void ClearSelectionIn(BaseUI view)
        {
            if (BelongsTo(Selection, view)) EventSystem.current.SetSelectedGameObject(null);
        }

        bool UsableFocus(GameObject candidate)
        {
            if (!candidate || !candidate.activeInHierarchy || inputLocked) return false;
            BaseUI usable = popups.Count > 0 ? popups[popups.Count - 1] : ActiveScreen;
            if (!BelongsTo(candidate, usable) || !usable.IsOpen || !usable.group.interactable) return false;
            var selectable = candidate.GetComponent<Selectable>();
            return selectable && selectable.IsActive() && selectable.IsInteractable();
        }

        void ClearUnusableSelection()
        {
            if (Selection && !UsableFocus(Selection)) EventSystem.current.SetSelectedGameObject(null);
        }

        void RestoreFocus(GameObject candidate)
        {
            if (!EventSystem.current) return;
            EventSystem.current.SetSelectedGameObject(UsableFocus(candidate) ? candidate : null);
        }

        void RepairFocusReferences(BaseUI removed, GameObject replacement)
        {
            foreach (BaseUI popup in popups)
                if (popup != removed && BelongsTo(previousFocus[popup], removed))
                    previousFocus[popup] = replacement;
        }

        static void ThrowErrors(List<Exception> errors)
        {
            if (errors.Count == 1) throw errors[0];
            if (errors.Count > 1) throw new AggregateException("UI cleanup failed.", errors);
        }
    }
}
