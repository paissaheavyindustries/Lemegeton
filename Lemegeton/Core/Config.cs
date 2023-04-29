using Dalamud.Configuration;
using FFXIVClientStructs.FFXIV.Common.Math;

namespace Lemegeton.Core
{

    public class Config : IPluginConfiguration
    {

        public int Version { get; set; } = 0;
        public string Language { get; set; } = "English";
        public bool Opened { get; set; } = true;
        public bool ShowShortcut { get; set; } = true;
        public bool FirstRun { get; set; } = true;
        public bool AdvancedOptions { get; set; } = false;

        public string OpcodeUrl { get; set; } = @"https://raw.githubusercontent.com/paissaheavyindustries/Resources/main/Blueprint/blueprint.xml";
        public string OpcodeRegion { get; set; } = "EN/DE/FR/JP";
        public bool LogUnhandledOpcodes { get; set; } = false;

        public bool QuickToggleAutomarkers { get; set; } = true;
        public bool QuickToggleOverlays { get; set; } = false;
        public bool QuickToggleSound { get; set; } = false;
#if !SANS_GOETIA
        public bool QuickToggleHacks { get; set; } = false;
        public bool QuickToggleAutomation { get; set; } = false;
#endif

        public bool NagAboutStreaming { get; set; } = true;

        public bool AutomarkerSoft { get; set; } = false;
        public bool AutomarkerCommands { get; set; } = false;
        public int AutomarkersServed { get; set; } = 0;
        public bool RemoveMarkersAfterCombatEnd { get; set; } = true;
        public bool RemoveMarkersAfterWipe { get; set; } = true;

        public Vector4 SoftmarkerTint { get; set; } = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        public bool SoftmarkerBounce { get; set; } = true;
        public bool SoftmarkerBlink { get; set; } = false;
        public float SoftmarkerScale { get; set; } = 1.0f;
        public float SoftmarkerOffsetWorldX { get; set; } = 0.0f;
        public float SoftmarkerOffsetWorldY { get; set; } = 2.0f;
        public float SoftmarkerOffsetWorldZ { get; set; } = 0.0f;
        public float SoftmarkerOffsetScreenX { get; set; } = 0.0f;
        public float SoftmarkerOffsetScreenY { get; set; } = 0.0f;

        public bool DebugOnlyLogAutomarkers { get; set; } = false;

        public string PropertyBlob { get; set; } = "";

        internal AutomarkerTiming DefaultAutomarkerTiming = new AutomarkerTiming();

        public float AutomarkerIniDelayMin
        {
            get
            {
                return DefaultAutomarkerTiming.IniDelayMin;
            }
            set
            {
                DefaultAutomarkerTiming.IniDelayMin = value;
            }
        }

        public float AutomarkerIniDelayMax
        {
            get
            {
                return DefaultAutomarkerTiming.IniDelayMax;
            }
            set
            {
                DefaultAutomarkerTiming.IniDelayMax = value;
            }
        }

        public float AutomarkerSubDelayMin
        {
            get
            {
                return DefaultAutomarkerTiming.SubDelayMin;
            }
            set
            {
                DefaultAutomarkerTiming.SubDelayMin = value;
            }
        }

        public float AutomarkerSubDelayMax
        {
            get
            {
                return DefaultAutomarkerTiming.SubDelayMax;
            }
            set
            {
                DefaultAutomarkerTiming.SubDelayMax = value;
            }
        }

    }

}
