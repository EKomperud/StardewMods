using System;
using Harmony;
using StardewModdingAPI;
using StardewValley;
using System.Reflection;

namespace MultipleGiftsForSpouse
{
    public class ModEntry : Mod
    {
        public static ModConfig Config;
        public static IMonitor monitor;

        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();
            monitor = Monitor;

            HarmonyInstance harmony = HarmonyInstance.Create("Redwood.MultipleGiftsForSpouse");
            Type[] types = new Type[] { typeof(Farmer) };
            MethodInfo originalMethod = typeof(NPC).GetMethod("tryToReceiveActiveObject");
            MethodInfo patchingMethod0 = typeof(PatchedSpouseGiftLimit).GetMethod("Prefix");
            MethodInfo patchingMethod1 = typeof(PatchedSpouseGiftLimit).GetMethod("Postfix");
            harmony.Patch(originalMethod, new HarmonyMethod(patchingMethod0), new HarmonyMethod(patchingMethod1));

            helper.Events.GameLoop.DayStarted += TimeEvents_AfterDayStarted;
        }

        private void TimeEvents_AfterDayStarted(object sender, EventArgs e)
        {
            PatchedSpouseGiftLimit.GiftsGiven = 0;
        }
    }

    class PatchedSpouseGiftLimit
    {
        public static bool IsGiftableSpouse;
        public static int GiftsGiven;

        public static void Prefix(NPC __instance, Farmer who)
        {
            IsGiftableSpouse = (who.spouse != null && who.spouse.Equals(__instance.Name)) &&
                               who.friendshipData[__instance.Name].GiftsToday == 0;
        }

        public static void Postfix(NPC __instance, Farmer who)
        {
            if (IsGiftableSpouse && who.friendshipData[__instance.Name].GiftsToday == 1)
            {
                GiftsGiven += 1;
                if (GiftsGiven < ModEntry.Config.GiftLimit)
                    who.friendshipData[__instance.Name].GiftsToday = 0;
            }
        }
    }
}
