using System;
using System.Collections.Generic;
using System.Linq;

namespace FateDice
{
    [Serializable] public sealed class ShopOffer
    {
        public string productId;
        public int price;
    }
    public static class ShopRules
    {
        public static bool IsLegacy(RunRulesCatalog rules) => rules.world.shopPriceMultipliers==null||rules.world.shopPriceMultipliers.Length==0;
        public static int Price(RunRulesCatalog rules,int basePrice,Grade grade)
        {
            double multiplier=IsLegacy(rules)?1:rules.world.shopPriceMultipliers[(int)grade];
            return (int)Math.Min(int.MaxValue,Math.Round(basePrice*multiplier,MidpointRounding.AwayFromZero));
        }
        public static void Enter(RunStateData state)
        {
            state.shopOffers=state.Rules.world.shop.Select(product=>new ShopOffer
                {productId=product.id,price=Price(state.Rules,product.price,state.activeGrade)}).ToList();
        }
        public static bool IsShopContext(RunStateData state) => state.phase==RunPhase.Shop||state.rewardReturnPhase==RunPhase.Shop;
        public static void RestoreLegacy(RunStateData state)
        {
            if(state?.Rules?.world==null||!IsShopContext(state)||!IsLegacy(state.Rules)||
                (state.shopOffers!=null&&state.shopOffers.Count>0)||state.Rules.world.shop==null)return;
            // These are the original run's snapshotted base prices, not today's authoring data.
            Enter(state);
        }
        public static ShopOffer Offer(RunStateData state,string productId) => state.shopOffers?.FirstOrDefault(offer=>offer.productId==productId);
        public static string[] Validate(RunStateData state)
        {
            var offers=state.shopOffers;
            if(!IsShopContext(state))
                return offers==null||offers.Count==0?Array.Empty<string>():new[]{"Unexpected shop offers outside the shop."};
            if((offers==null||offers.Count==0)&&IsLegacy(state.Rules))return Array.Empty<string>();
            if(offers==null||offers.Count!=state.Rules.world.shop.Length||offers.Any(offer=>offer==null)||
                offers.Select(offer=>offer.productId).Distinct().Count()!=offers.Count)return new[]{"The fixed shop offers are incomplete."};
            foreach(var offer in offers)
            {
                var product=state.Rules.world.shop.FirstOrDefault(item=>item.id==offer.productId);
                if(product==null||offer.price<0||offer.price!=Price(state.Rules,product.price,state.activeGrade))
                    return new[]{"The fixed shop price or product ID is invalid."};
            }
            return Array.Empty<string>();
        }
    }
}
