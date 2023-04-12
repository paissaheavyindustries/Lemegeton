using Dalamud.Configuration;
using System.Collections.Generic;

namespace Lemegeton.Core
{

    public class Config : IPluginConfiguration
    {

        public int Version { get; set; } = 0;
        public string Language { get; set; } = "English";
        public bool Opened { get; set; } = true;
        public bool ShowShortcut { get; set; } = true;
        public bool FirstRun { get; set; } = true;

        public string OpcodeUrl { get; set; } = @"https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.min.json";
        public string OpcodeRegion { get; set; } = "Global";

        public bool QuickToggleAutomarkers { get; set; } = false;
        public bool QuickToggleOverlays { get; set; } = false;
        public bool QuickToggleSound { get; set; } = false;
        public bool QuickToggleHacks { get; set; } = false;

        public bool NagAboutStreaming { get; set; } = true;

        public bool AutomarkerSoft { get; set; } = false;
        public bool AutomarkerCommands { get; set; } = false;
        public float AutomarkerIniDelayMin { get; set; } = 0.3f;
        public float AutomarkerIniDelayMax { get; set; } = 0.7f;
        public float AutomarkerSubDelayMin { get; set; } = 0.1f;
        public float AutomarkerSubDelayMax { get; set; } = 0.3f;
        public int AutomarkersServed { get; set; } = 0;

        public bool DebugOptions { get; set; } = true;
        public bool DebugOnlyLogAutomarkers { get; set; } = false;

        public string PropertyBlob { get; set; } = "";

    }

}
