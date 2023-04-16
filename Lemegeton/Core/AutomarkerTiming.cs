using System;
using System.Data;
using System.Globalization;

namespace Lemegeton.Core
{

    internal class AutomarkerTiming
    {

        public enum TimingTypeEnum
        {
            Inherit,
            Explicit
        }

        public TimingTypeEnum TimingType { get; set; } = TimingTypeEnum.Inherit;
        internal AutomarkerTiming Parent = null;

        public float IniDelayMin { get; set; } = 0.3f;
        public float IniDelayMax { get; set; } = 0.7f;
        public float SubDelayMin { get; set; } = 0.1f;
        public float SubDelayMax { get; set; } = 0.3f;

        private double GetRandom()
        {
            Random r = new Random();
            return r.NextDouble();
        }

        public int SampleInitialTime()
        {
            if (TimingType == TimingTypeEnum.Inherit && Parent != null)
            {
                return Parent.SampleInitialTime();
            }
            double rng = GetRandom();
            int delay = (int)Math.Ceiling((IniDelayMin + ((IniDelayMax - IniDelayMin) * rng)) * 1000.0);
            return delay;
        }

        public int SampleSubsequentTime()
        {
            if (TimingType == TimingTypeEnum.Inherit && Parent != null)
            {
                return Parent.SampleSubsequentTime();
            }
            double rng = GetRandom();
            int delay = (int)Math.Ceiling((SubDelayMin + ((SubDelayMax - SubDelayMin) * rng)) * 1000.0);
            return delay;
        }

        public string Serialize()
        {
            return String.Format("TimingType={0};IniDelayMin={1};IniDelayMax={2};SubDelayMin={3};SubDelayMax={4}", 
                TimingType.ToString(),
                IniDelayMin.ToString(CultureInfo.InvariantCulture), IniDelayMax.ToString(CultureInfo.InvariantCulture),
                SubDelayMin.ToString(CultureInfo.InvariantCulture), SubDelayMax.ToString(CultureInfo.InvariantCulture)
            );
        }

        public void Deserialize(string data)
        {
            string[] items = data.Split(";");
            foreach (string item in items)
            {
                string[] kp = item.Split("=");
                switch (kp[0])
                {
                    case "TimingType":
                        TimingType = (TimingTypeEnum)Enum.Parse(typeof(TimingTypeEnum), kp[1]);
                        break;
                    case "IniDelayMin":
                        IniDelayMin = float.Parse(kp[1], CultureInfo.InvariantCulture);
                        break;
                    case "IniDelayMax":
                        IniDelayMax = float.Parse(kp[1], CultureInfo.InvariantCulture);
                        break;
                    case "SubDelayMin":
                        SubDelayMin = float.Parse(kp[1], CultureInfo.InvariantCulture);
                        break;
                    case "SubDelayMax":
                        SubDelayMax = float.Parse(kp[1], CultureInfo.InvariantCulture);
                        break;
                }
            }
        }

    }

}
