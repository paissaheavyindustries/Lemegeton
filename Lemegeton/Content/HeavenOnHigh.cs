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
            bool newZoneOk = ((newZone >= 770 && newZone <= 775) || (newZone >= 782 && newZone <= 785));
            if (newZoneOk == true && ZoneOk == false)
            {
                Log(State.LogLevelEnum.Info, null, "Content available");
            }
            else if (newZoneOk == false && ZoneOk == true)
            {
                Log(State.LogLevelEnum.Info, null, "Content unavailable");
            }
            ZoneOk = newZoneOk;
        }

    }

}
