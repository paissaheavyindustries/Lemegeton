using Lemegeton.Core;
using static Lemegeton.Core.State;

namespace Lemegeton.Content
{

    internal class UltFuturesRewritten : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        private bool ZoneOk = false;
        private bool _subbed = false;

        public UltFuturesRewritten(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
        }

        private void SubscribeToEvents()
        {
            lock (this)
            {
                if (_subbed == true)
                {
                    return;
                }
                _subbed = true;
                Log(LogLevelEnum.Debug, null, "Subscribing to events");
                _state.OnCombatChange += OnCombatChange;
            }
        }

        private void UnsubscribeFromEvents()
        {
            lock (this)
            {
                if (_subbed == false)
                {
                    return;
                }
                Log(LogLevelEnum.Debug, null, "Unsubscribing from events");
                _state.OnCombatChange -= OnCombatChange;
                _subbed = false;
            }
        }

        protected override bool ExecutionImplementation()
        {
            if (ZoneOk == true)
            {
                return base.ExecutionImplementation();
            }
            return false;
        }

        private void OnCombatChange(bool inCombat)
        {
            Reset();
        }

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (newZone == 9999);
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
                SubscribeToEvents();
                LogItems();
            }
            else if (newZoneOk == false && ZoneOk == true)
            {
                Log(State.LogLevelEnum.Info, null, "Content unavailable");
                UnsubscribeFromEvents();
            }
            ZoneOk = newZoneOk;
        }

    }

}
