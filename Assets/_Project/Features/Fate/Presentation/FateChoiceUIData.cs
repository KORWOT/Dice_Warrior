using System;

namespace FateDice
{
    // Only public offers and explicit user requests cross this modal boundary.
    public sealed class FateChoiceUIData : RunUIData
    {
        public FateOfferUIData[] offers;
        public Action<string> confirm;
        public Action close;
    }
}
