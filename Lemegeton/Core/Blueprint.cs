using System.Collections.Generic;
using System.Xml.Serialization;

namespace Lemegeton.Core
{

    public class Blueprint
    {

        public enum DebugFlags
        {
            None = 0,
            StatusEffects = 0x01,
            Opcodes = 0x02,
            All = StatusEffects | Opcodes,
        }

        public class Region
        {

            public class Opcode
            {

                [XmlAttribute]
                public string Name { get; set; }
                [XmlAttribute]
                public ushort Id { get; set; }

            }

            public class Warning
            {

                public enum TypeEnum
                {
                    Warning,
                    Information
                }

                [XmlAttribute]
                public string Text { get; set; }

                [XmlAttribute]
                public TypeEnum Type { get; set; } = TypeEnum.Warning;

            }

            [XmlAttribute]
            public string Name { get; set; }
            [XmlAttribute]
            public string Version { get; set; }
            [XmlAttribute]
            public DebugFlags DebugFlags { get; set; }
            private string _DebugInstanceData = "";
            [XmlAttribute(AttributeName = "DebugInstances")]
            public string DebugInstanceData
            {
                get
                {
                    return _DebugInstanceData;
                }
                set
                {
                    if (_DebugInstanceData != value)
                    {
                        DebugInstances.Clear();
                        _DebugInstanceData = value;
                        if (_DebugInstanceData != null)
                        {
                            string[] insts = _DebugInstanceData.Split(",");                            
                            foreach (string inst in insts)
                            {
                                if (int.TryParse(inst, out int ii) == true)
                                {
                                    DebugInstances.Add(ii);
                                }
                            }
                        }
                    }
                }
            }

            internal List<int> DebugInstances = new List<int>();
            public List<Warning> Warnings { get; set; } = new List<Warning>();
            public List<Opcode> Opcodes { get; set; } = new List<Opcode>();            
            internal Dictionary<string, Opcode> OpcodeLookup = new Dictionary<string, Opcode>();

        }

        public List<Region> Regions { get; set; } = new List<Region>();
        internal Dictionary<string, Region> RegionLookup = new Dictionary<string, Region>();

        internal void BuildLookups()
        {
            RegionLookup.Clear();
            foreach (Region r in Regions)
            {
                RegionLookup[r.Name] = r;
                foreach (Region.Opcode o in r.Opcodes)
                {
                    r.OpcodeLookup[o.Name] = o;
                }
            }
        }

    }

}
