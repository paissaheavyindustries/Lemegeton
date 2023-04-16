using Lemegeton.Core;

namespace Lemegeton.Content
{

    public class PalaceOfTheDead : GenericDeepDungeon
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

        public PalaceOfTheDead(State st) : base(st)
        {
            st.OnZoneChange += OnZoneChange;
        }

        private void OnZoneChange(ushort newZone)
        {
            bool newZoneOk = (
                (newZone >= 561 && newZone <= 565)
                ||
                (newZone == 570)
                ||
                (newZone >= 593 && newZone <= 607)
            );
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
