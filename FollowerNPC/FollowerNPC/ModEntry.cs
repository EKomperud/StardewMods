using System;
using System.Collections.Generic;
using Harmony;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;

namespace FollowerNPC
{
    public class ModEntry : Mod
    {
        public static ModConfig config;
        public static IMonitor monitor;
        public bool spawned;
        public NPC whiteBox;
        public float whiteBoxSpeed;
        public float whiteBoxAnimationSpeed;
        public float whiteBoxFollowThreshold;
        public Vector2 whiteBoxLastMovementDirection;
        public Vector2 whiteBoxLastPosition;
        public bool whiteBoxMovedLastFrame;
        public bool whiteBoxNeedsWarp;
        public int whiteBoxWarpTimer;
        public aStar whiteBoxAStar;
        public Queue<Vector2> whiteBoxPath;
        public Vector2 whiteBoxPathNode;
        public float whiteBoxPathfindNodeGoalTolerance;
        public bool whiteBoxFollow;

        public Farmer farmer;
        public Vector2 farmerLastTile;

        public override void Entry(IModHelper helper)
        {
            config = Helper.ReadConfig<ModConfig>();
            monitor = Monitor;
            whiteBoxFollowThreshold = 3;
            whiteBoxPathfindNodeGoalTolerance = 0.1f;

            HarmonyInstance harmony = HarmonyInstance.Create("Redwood.FollowerNPC");

            ControlEvents.KeyReleased += ControlEvents_KeyReleased;
            //GameEvents.UpdateTick += GameEvents_UpdateTick;
            GameEvents.FourthUpdateTick += GameEvents_FourthUpdateTick;
            SaveEvents.AfterLoad += SaveEvents_AfterLoad;
            PlayerEvents.Warped += PlayerEvents_Warped;
            MineEvents.MineLevelChanged += MineEvents_MineLevelChanged;
        }

        private void ControlEvents_KeyReleased(object sender, EventArgsKeyPressed e)
        {
            //TO-DO: Utilize Path.Combine
            if (!Context.IsWorldReady)
                return;

            if (e.KeyPressed == Keys.O && !spawned)
            {
                spawned = true;
                AnimatedSprite sprite = new AnimatedSprite("Characters\\Abigail",0,Game1.tileSize / 4, (Game1.tileSize * 2)/4);
                whiteBox = new NPC(sprite, Game1.player.Position, "SeedShop", 2, "Abigail", true, null, Game1.content.Load<Texture2D>("Portraits\\Abigail"));
                //AnimatedSprite sprite = new AnimatedSprite("Characters\\Maru", 0, Game1.tileSize / 4, (Game1.tileSize * 2) / 4);
                //whiteBox = new NPC(sprite, Game1.player.Position, "ScienceHouse", 2, "Maru", true, null, Game1.content.Load<Texture2D>("Portraits\\Maru"));
                Game1.player.currentLocation.addCharacter(whiteBox);
                //whiteBoxAStar = new aStar(farmer.currentLocation);
                whiteBox.showTextAboveHead("Hey " + farmer.Name + "!", -1, 2, 3000, 0);
                whiteBoxSpeed = 5f;
                whiteBoxAnimationSpeed = 10f;
                whiteBoxFollow = false;
            }

            else if (e.KeyPressed == Keys.P && spawned)
            {
                spawned = false;
                Game1.removeCharacterFromItsLocation("Abigail");
                //Game1.removeCharacterFromItsLocation("Maru");
                whiteBox = null;
            }

            else if (e.KeyPressed == Keys.U && spawned)
            {
                monitor.Log(whiteBox?.currentLocation.Name + " : " + whiteBox?.getTileLocation());
            }

            else if (e.KeyPressed == Keys.I)
            {
                monitor.Log(farmer?.currentLocation.Name + " : " + farmer?.getTileLocation());
            }

            else if (e.KeyPressed == Keys.L && spawned)
            {
                Game1.warpCharacter(whiteBox, farmer.currentLocation, farmer.getTileLocation());
            }

            else if (e.KeyPressed == Keys.K && spawned)
            {
                whiteBoxFollow = !whiteBoxFollow;
            }
        }

        //private void GameEvents_UpdateTick(object sender, EventArgs e)
        //{
        //    if (!Context.IsWorldReady || !(whiteBox != null) || !(farmer != null))
        //        return;

        //    FollowFarmer();
        //}

        private void GameEvents_FourthUpdateTick(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady || !(whiteBox != null) || !(farmer != null))
                return;

            DelayedWarp();
            if (whiteBoxFollow)
                FollowFarmer();
        }

        private void SaveEvents_AfterLoad(object sender, EventArgs e)
        {
            farmer = Game1.player;
            spawned = false;
            whiteBox = null;
        }

        private void PlayerEvents_Warped(object sender, EventArgsPlayerWarped e)
        {
            if (!Context.IsWorldReady || !spawned || !(whiteBox != null) || !(farmer != null))
                return;

            whiteBoxAStar.gameLocation = farmer.currentLocation;
            if (!farmer.isRidingHorse())
                Game1.warpCharacter(whiteBox, farmer.currentLocation, farmer.getTileLocation());
            else
            {
                whiteBoxNeedsWarp = true;
                whiteBoxFollow = false;
                whiteBoxWarpTimer = 4;
            }
        }

        private void MineEvents_MineLevelChanged(object sender, EventArgsMineLevelChanged e)
        {
            if (!Context.IsWorldReady || !spawned || !(whiteBox != null) || !(farmer != null))
                return;

            Game1.warpCharacter(whiteBox, farmer.currentLocation, farmer.getTileLocation());
        }

        #region Helpers

        private void FollowFarmer()
        {
            Point f = farmer.GetBoundingBox().Center;
            Point w = whiteBox.GetBoundingBox().Center;
            Vector2 diff = new Vector2(f.X, f.Y) - new Vector2(w.X, w.Y);
            float diffLen = diff.Length();
            if (diffLen > Game1.tileSize * whiteBoxFollowThreshold)
            {
                Vector2 farmerCurrentTile = farmer.getTileLocation();
                if (farmerLastTile != farmerCurrentTile)
                {
                    whiteBoxPath = whiteBoxAStar.Pathfind(whiteBox.getTileLocation(), farmerCurrentTile);
                    whiteBoxPathNode = whiteBoxPath.Dequeue();
                }
                if (whiteBoxPathNode != null)
                {
                    Point n = new Point((int)whiteBoxPathNode.X * Game1.tileSize, (int)whiteBoxPathNode.Y * Game1.tileSize);
                    Vector2 nodeDiff = new Vector2(n.X, n.Y) - new Vector2(w.X, w.Y);
                    float nodeDiffLen = nodeDiff.Length();
                    while (nodeDiffLen <= whiteBoxPathfindNodeGoalTolerance)
                    {
                        if (whiteBoxPath.Count == 0)
                            return;
                        whiteBoxPathNode = whiteBoxPath.Dequeue();
                        n = new Point((int)whiteBoxPathNode.X * Game1.tileSize, (int)whiteBoxPathNode.Y * Game1.tileSize);
                        nodeDiff = new Vector2(n.X, n.Y) - new Vector2(w.X, w.Y);
                        nodeDiffLen = nodeDiff.Length();
                    }
                    nodeDiff /= nodeDiffLen;
                    whiteBox.xVelocity = nodeDiff.X * whiteBoxSpeed;
                    whiteBox.yVelocity = -nodeDiff.Y * whiteBoxSpeed;
                    whiteBox.faceDirection(GetFacingDirectionFromMovement(nodeDiff));
                    SetMovementDirectionAnimation(whiteBox.FacingDirection);
                    whiteBox.MovePosition(Game1.currentGameTime, Game1.viewport, whiteBox.currentLocation);
                    whiteBoxLastMovementDirection = nodeDiff;

                }
                farmerLastTile = farmerCurrentTile;
            }
            else if (whiteBoxMovedLastFrame)
            {
                whiteBox.Sprite.faceDirectionStandard(GetFacingDirectionFromMovement(whiteBoxLastMovementDirection));
                whiteBoxMovedLastFrame = false;
            }
        }

        private void DelayedWarp()
        {
            if (whiteBoxNeedsWarp)
                if (--whiteBoxWarpTimer <= 0)
                {
                    whiteBoxFollow = true;
                    Game1.warpCharacter(whiteBox, farmer.currentLocation, farmer.getTileLocation());
                    whiteBoxNeedsWarp = false;
                }
        }

        private int GetFacingDirectionFromMovement(Vector2 movement)
        {
            int dir = 2;
            if (Math.Abs(movement.X) > Math.Abs(movement.Y))
                dir = movement.X > 0 ? 1 : 3;
            else if (Math.Abs(movement.X) < Math.Abs(movement.Y))
                dir = movement.Y > 0 ? 2 : 0;
            return dir;
        }

        private void SetMovementDirectionAnimation(int dir)
        {
            switch (dir)
            {
                case 0:
                    whiteBox.SetMovingOnlyUp();
                    whiteBox.Sprite.AnimateUp(Game1.currentGameTime, (int)(whiteBoxSpeed * -whiteBoxAnimationSpeed), ""); break;
                case 1:
                    whiteBox.SetMovingOnlyRight();
                    whiteBox.Sprite.AnimateRight(Game1.currentGameTime, (int)(whiteBoxSpeed * -whiteBoxAnimationSpeed), ""); break;
                case 2:
                    whiteBox.SetMovingOnlyDown();
                    whiteBox.Sprite.AnimateDown(Game1.currentGameTime, (int)(whiteBoxSpeed * -whiteBoxAnimationSpeed), ""); break;
                case 3:
                    whiteBox.SetMovingOnlyLeft();
                    whiteBox.Sprite.AnimateLeft(Game1.currentGameTime, (int)(whiteBoxSpeed * -whiteBoxAnimationSpeed), ""); break;

            }
        }

        private bool FloatsApproximatelyEqual(float a, float b, float threshold)
        {
            return Math.Abs(a - b) < threshold;
        }
        #endregion
    }
}

