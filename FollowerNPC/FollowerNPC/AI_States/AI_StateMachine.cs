﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StardewValley;
using StardewModdingAPI.Events;

namespace FollowerNPC.AI_States
{
    public class AI_StateMachine
    {
        public CompanionsManager owner;

        public AI_State currentState { get; private set; }
        public AI_State[] states { get; private set; }
        private Dictionary<string, bool> bools;

        public AI_StateMachine(CompanionsManager owner)
        {
            this.owner = owner;

            states = new AI_State[2];
            states[0] = new AI_StateFollowCharacter(owner.companion, owner.farmer, this);
            states[1] = new AI_StateAggroEnemy(owner.companion, owner.farmer, this);

            bools = new Dictionary<string, bool>();

            ChangeState(eAI_State.followFarmer);
        }

        public bool GetBool(string s)
        {
            if (bools.TryGetValue(s, out bool b))
                return b;
            throw new KeyNotFoundException("bool " + s + " does not exist in this State Machine!");
        }

        public void SetBool(string s, bool b)
        {
            bools[s] = b;
        }

        public void Dispose()
        {
            if (currentState != null)
                currentState.ExitState();
            currentState = null;
            owner = null;
        }

        internal void ChangeState(eAI_State newState)
        {
            if (currentState != null)
                currentState.ExitState();
            currentState = states[(int)newState];
            currentState.EnterState();
        }

    }

    public enum eAI_State
    {
        nil = -1,
        followFarmer = 0,
        aggroEnemy = 1
    }

    public class AI_State
    {
        public AI_State()
        {
        }

        public virtual void EnterState()
        {

        }

        public virtual void ExitState()
        {

        }

        public virtual void Update(UpdateTickedEventArgs e)
        {

        }
    }
}
