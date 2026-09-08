using System;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class TitleUIData : UIData
    {
        public string title, subtitle, status;
        public Action enterLobby;
        public ButtonAppearance appearance;
    }

    // Displays supplied title data. Navigation remains an injected callback.
    public sealed class TitleUI : BaseUI<TitleUIData>
    {
        public Text title, subtitle, status;
        public CommonButtonView enterButton;

        protected override void OnBind(TitleUIData data)
        {
            if (!title || !subtitle || !status || !enterButton)
                throw new InvalidOperationException("TitleUI requires title, subtitle, status and enterButton references.");
            if (data.appearance == null)
                throw new ArgumentException("TitleUIData.appearance is required.", nameof(data));

            SetText(title, data.title);
            SetText(subtitle, data.subtitle);
            SetText(status, data.status);
            var enter = data.enterLobby;
            enterButton.Bind("enter-lobby", "로비로 이동", null, ">", data.appearance,
                enter != null, false, enter == null ? null : _ => enter());
        }

        protected override void OnUnbind()
        {
            if (enterButton) enterButton.Unbind();
            ClearText(title);
            ClearText(subtitle);
            ClearText(status);
        }

        private static void SetText(Text field, string value)
        {
            field.text = value ?? "";
            field.gameObject.SetActive(!string.IsNullOrEmpty(field.text));
        }

        private static void ClearText(Text field)
        {
            if (!field) return;
            field.text = "";
            field.gameObject.SetActive(false);
        }
    }
}
