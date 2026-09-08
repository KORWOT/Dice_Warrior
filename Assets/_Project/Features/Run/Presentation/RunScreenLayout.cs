using System;
using UnityEngine;
using UnityEngine.UI;

namespace FateDice
{
    // All fixed objects and their layout are authored in each complete screen prefab.
    public sealed class RunScreenLayout : MonoBehaviour
    {
        public Text header, stats, situation, fate, notice, gear;
        public RectTransform diceRow, body, footer;
        public ScrollRect scroll;

        public void Validate()
        {
            if (!header || !stats || !situation || !fate || !notice || !gear ||
                !diceRow || !body || !footer || !scroll)
                throw new InvalidOperationException("RunScreenLayout requires its authored HUD, dice/body/footer and ScrollRect references.");
        }
    }
}
