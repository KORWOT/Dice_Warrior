using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    public sealed class ResultUI : RunScreenView<ResultUIData>
    {
        public RectTransform choices;
        public Text summary, grades;
        protected override void BindScreen(ResultUIData data)
        {
            SetText(summary, data.summary); SetText(grades, data.grades);
            Widgets.Choices(choices, new[] { data.restart });
        }
        protected override void UnbindScreen()
        {
            ClearText(summary); ClearText(grades);
        }
    }
}
