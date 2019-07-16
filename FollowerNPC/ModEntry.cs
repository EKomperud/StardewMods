using System;
using System.Collections.Generic;
using System.Reflection;
using Harmony;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;

namespace FollowerNPC
{
    public class ModEntry : Mod
    {

        #region Members and Entry Function
        public static ModConfig config;
        public static IMonitor monitor;
        public static IModHelper modHelper;

        public CompanionsManager companionsManager;

        public static MethodInfo applyVelocity;

        private List<FarmerSprite.AnimationFrame> fishingLeftAnim;
        private List<FarmerSprite.AnimationFrame> fishingRightAnim;

        public override void Entry(IModHelper helper)
        {
            // Initialize variables //
            config = Helper.ReadConfig<ModConfig>();
            monitor = Monitor;
            modHelper = helper;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            //**********************//

            // Patch methods //
            HarmonyInstance harmony = HarmonyInstance.Create("Redwood.FollowerNPC");

            // patch GameLocation.isCollidingPosition
            harmony.Patch(
                original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.isCollidingPosition), new[] { typeof(Rectangle), typeof(xTile.Dimensions.Rectangle), typeof(bool), typeof(int), typeof(bool), typeof(Character), typeof(bool), typeof(bool), typeof(bool) }),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.IsCollidingPosition_Prefix))),
                postfix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.IsCollidingPosition_Postfix)))
            );

            // patch NPC.MovePosition
            //harmony.Patch(
            //    original: AccessTools.Method(typeof(NPC), nameof(NPC.MovePosition)),
            //    prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.MovePosition_Prefix)))
            //);

            // patch NPC.updateMovement
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.updateMovement)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.UpdateMovement_Prefix)))
            );

            // patch NPC.faceTowardFarmerForPeriod
            harmony.Patch(
                original: AccessTools.Method(typeof(NPC), nameof(NPC.faceTowardFarmerForPeriod)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.FaceTowardFarmerForPeriod_Prefix)))
            );

            // patch Farmer.gainExperience
            harmony.Patch(
                original: AccessTools.Method(typeof(Farmer), nameof(Farmer.gainExperience)),
                prefix: new HarmonyMethod(AccessTools.Method(typeof(Patches), nameof(Patches.GainExperience_Prefix)))
            );

            fishingLeftAnim = new List<FarmerSprite.AnimationFrame>
            {
                new FarmerSprite.AnimationFrame(0, 4000, false, true, null, false),
                new FarmerSprite.AnimationFrame(0, 4000, false, true, null, false)
            };
            fishingRightAnim = new List<FarmerSprite.AnimationFrame>
            {
                new FarmerSprite.AnimationFrame(20, 4000),
                new FarmerSprite.AnimationFrame(21, 4000)
            };

            applyVelocity = typeof(Character).GetMethod("applyVelocity", BindingFlags.NonPublic | BindingFlags.Instance);
            //**********************//

            // Subscribe to events //
            //Helper.Events.Input.ButtonReleased += Input_ButtonReleased;
            //**********************//
        }

        /// <summary>Raised after the game is launched, right before the first update tick. This happens once per game session (unrelated to loading saves). All mods are loaded and initialised at this point, so this is a good time to set up mod integrations.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            companionsManager = new CompanionsManager();
        }

        #endregion

        #region Event Functions

        // Just used for debug commands
        private void Input_ButtonReleased(object sender, ButtonReleasedEventArgs e)
        {
            if (!Context.IsWorldReady || companionsManager == null)
                return;

            //if (e.Button == SButton.K)
            //{
            //    companionsManager.farmer.eventsSeen.Remove(471942);
            //}
        }

        #endregion
    }

    /// <summary>
    /// A collection of Harmony patches that allow me to modify, add, or remove certain
    /// functionalities of CA's code to make it work nicely with mine. 
    /// </summary>
    class Patches
    {
        static public string companion;

        /// <summary>
        /// A weird, roundabout way of allowing Companions to pass through invisible
        /// barriers that normally block NPC's. Might want to consider making this a
        /// little less, strange(?), in the future.
        /// </summary>
        #region isCollidingPosition
        static public bool flag;
        static public string bypass = "NPC";
        static public string name;

        static public void IsCollidingPosition_Prefix(GameLocation __instance, Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character, bool pathfinding, bool projectile = false, bool ignoreCharacterRequirement = false)
        {
            if (companion != null
                && character != null
                && character.Name != null
                && character.Name.Equals(companion)
                && !character.eventActor)
            {
                flag = true;
                name = character.Name;
                character.Name = bypass;
            }
        }

        static public void IsCollidingPosition_Postfix(GameLocation __instance, Rectangle position, xTile.Dimensions.Rectangle viewport, bool isFarmer, int damagesFarmer, bool glider, Character character, bool pathfinding, bool projectile = false, bool ignoreCharacterRequirement = false)
        {
            if (flag)
            {
                flag = false;
                character.Name = name;
            }
        }
        #endregion

        /// <summary>
        /// The Companion's daily schedule is remade from scratch once they are dismissed.
        /// This pops off any routes from their schedule that are in the past until it reaches
        /// the most recent route they would have traveled. It then pops off that route and stores
        /// it as the location they will be warped to after they are finished being dismissed.
        /// </summary>
        #region checkSchedule
        static public Point scheduleCurrentDestination;

        static public void CheckSchedule_Postfix(NPC __instance, int timeOfDay)
        {
            if (companion != null && companion == __instance.Name)
            {
                SchedulePathDescription spd;
                if (__instance.Schedule.TryGetValue(timeOfDay, out spd) && spd != null)
                {
                    while (spd.route.Count != 0)
                        scheduleCurrentDestination = spd.route.Pop();
                }
            }
        }
        #endregion

        /// <summary>
        /// Prevents the Companion from updating their movement via CA's movementUpdate function
        /// while they are the farmer's companion.
        /// </summary>
        #region movePosition

        static public bool MovePosition_Prefix(NPC __instance, GameTime time, Rectangle viewport, GameLocation currentLocation)
        {
            bool dontSkip = (companion == null) || !__instance.Name.Equals(companion);
            if (!dontSkip)
            {
                object[] parameters = new object[] { __instance.currentLocation };
                ModEntry.applyVelocity.Invoke(__instance, parameters);
            }
            return dontSkip;
        }
        #endregion

        #region updateMovement

        static public bool UpdateMovement_Prefix(NPC __instance, GameLocation location, GameTime time)
        {
            bool dontSkip = (companion == null) || !__instance.Name.Equals(companion);
            return dontSkip;
        }

        #endregion

        #region faceTowardFarmerForPeriod

        static public bool dontFace;

        static public bool FaceTowardFarmerForPeriod_Prefix(NPC __instance)
        {
            if (dontFace && __instance.Name.Equals(companion))
                return false;
            return true;
        }
        #endregion

        #region gainExperience

        static public bool increaseExperience;
        static public string farmer;

        static public void GainExperience_Prefix(Farmer __instance, ref int howMuch)
        {
            if (increaseExperience && __instance.Name.Equals(farmer))
            {
                int expIncrease = Math.Max((int)(howMuch * 0.05f), 1);
                howMuch += expIncrease;
            }
        }

        #endregion
    }

    /// <summary>
    /// Harmony patche(s) that I use(d) for debug purposes. These are not (or should not) be
    /// used in any release build of this mod.
    /// </summary>
    class DebugPatches
    {
        static public void Prefix(GameLocation __instance, int waterDepth)
        {
            ModEntry.monitor.Log(waterDepth.ToString());
        }
    }

}

