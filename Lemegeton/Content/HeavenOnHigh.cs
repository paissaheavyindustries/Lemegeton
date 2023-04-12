using Lemegeton.Core;

namespace Lemegeton.Content
{

    public class HeavenOnHigh : GenericDeepDungeon
    {

        private bool ZoneOk = false;

        protected override bool ExecutionImplementation()
        {
            if (ZoneOk == true)
            {
                return base.ExecutionImplementation();
            }
            return false;
        }

        public HeavenOnHigh(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
        }

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (newZone >= 770 && newZone <= 785);
            if (newZoneOk == true && ZoneOk == false)
            {
                _state.Log(State.LogLevelEnum.Info, null, "Content {0} available", GetType().Name);
            }
            else if (newZoneOk == false && ZoneOk == true)
            {
                _state.Log(State.LogLevelEnum.Info, null, "Content {0} unavailable", GetType().Name);
            }
            ZoneOk = newZoneOk;
        }

    }

}
