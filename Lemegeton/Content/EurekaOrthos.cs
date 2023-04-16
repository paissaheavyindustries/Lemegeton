using Lemegeton.Core;

namespace Lemegeton.Content
{

    public class EurekaOrthos : GenericDeepDungeon
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

        public EurekaOrthos(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
        }

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (newZone >= 1099 && newZone <= 1108);
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
