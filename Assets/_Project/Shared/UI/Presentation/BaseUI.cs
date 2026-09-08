using System;
using UnityEngine;

namespace FateDice
{
    public abstract class UIData { }

    public abstract class BaseUI : MonoBehaviour
    {
        public CanvasGroup group;
        public bool IsOpen { get; private set; }
        public abstract Type DataType { get; }
        public UIManager Manager { get; private set; }
        bool initialized;

        internal void Attach(UIManager manager)
        {
            if (!manager) throw new ArgumentNullException(nameof(manager));
            if (Manager && Manager != manager)
                throw new InvalidOperationException("A UI instance cannot belong to two managers.");
            if (!group) throw new InvalidOperationException("A UI view requires its authored CanvasGroup.");
            Manager = manager;
        }

        internal void BindData(UIData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (!DataType.IsInstanceOfType(data))
                throw new ArgumentException($"Expected {DataType.Name} for {GetType().Name}.", nameof(data));
            if (!initialized)
            {
                InitializeView();
                initialized = true;
            }
            IsOpen = true;
            try { BindView(data); }
            catch (Exception bindError)
            {
                try { CloseView(); }
                catch (Exception closeError) { throw new AggregateException(bindError, closeError); }
                throw;
            }
        }

        internal void Activate()
        {
            if (!IsOpen) throw new InvalidOperationException("Bind UI data before activation.");
            gameObject.SetActive(true);
        }

        internal void CloseView()
        {
            if (!IsOpen) return;
            IsOpen = false;
            StopAllCoroutines();
            try { UnbindView(); }
            finally
            {
                group.interactable = false;
                group.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
        }

        protected abstract void InitializeView();
        protected abstract void BindView(UIData data);
        protected abstract void UnbindView();
    }

    public abstract class BaseUI<TData> : BaseUI where TData : UIData
    {
        protected TData Data { get; private set; }
        public sealed override Type DataType => typeof(TData);
        protected sealed override void InitializeView() => OnInitialize();
        protected sealed override void BindView(UIData data)
        {
            Data = (TData)data;
            OnBind(Data);
        }
        protected sealed override void UnbindView()
        {
            try { OnUnbind(); }
            finally { Data = null; }
        }
        protected abstract void OnBind(TData data);
        protected virtual void OnInitialize() { }
        protected virtual void OnUnbind() { }
    }
}
