using System.Collections.Generic;
using System.Xml.Serialization;

namespace Lemegeton.Core
{

    public class Blueprint
    {

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

                [XmlAttribute]
                public string Text { get; set; }

            }

            [XmlAttribute]
            public string Name { get; set; }
            [XmlAttribute]
            public string Version { get; set; }

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
