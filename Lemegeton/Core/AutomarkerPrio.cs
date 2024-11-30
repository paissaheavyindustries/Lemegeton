using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Lemegeton.Core
{

    internal class AutomarkerPrio
    {

        public enum PrioTypeEnum
        {
            PartyListOrder,
            PartyListCustom,
            Trinity,
            Role,
            Job,
            Player,
            Clockspots,
            CongaX,
            CongaY,
            Alphabetic,
        }

        public enum PrioTrinityEnum
        {
            Tank,
            Healer,
            DPS,
        }

        public enum PrioArchetypeEnum
        {
            Support,
            DPS,
        }

        public enum PrioRoleEnum
        {
            Tank,
            Healer,
            Melee,
            Ranged,
            Caster,
        }

        public enum PrioJobEnum : uint
        {
            PLD = 19, 
            WAR = 21, 
            DRK = 32, 
            GNB = 37,
            WHM = 24, 
            SCH = 28, 
            AST = 33, 
            SGE = 40,
            MNK = 20, 
            DRG = 22, 
            NIN = 30, 
            SAM = 34, 
            RPR = 39,
            VPR = 41,
            BRD = 23, 
            MCH = 31, 
            DNC = 38, 
            BLM = 25,
            PCT = 42,
            SMN = 27, 
            RDM = 35,
            BLU = 36,
        }

        public enum PrioDirectionEnum
        {            
            N = 1,
            NE = 2,
            E = 3,
            SE = 4,
            S = 5,
            SW = 6,
            W = 7,
            NW = 8,
        }

        public PrioTypeEnum Priority { get; set; } = PrioTypeEnum.Job;
        public bool Reversed { get; set; } = false;
        public PrioDirectionEnum StartingFrom = PrioDirectionEnum.N;

        public List<PrioTrinityEnum> _prioByTrinity = new List<PrioTrinityEnum>();
        public List<PrioRoleEnum> _prioByRole = new List<PrioRoleEnum>();
        public List<PrioJobEnum> _prioByJob = new List<PrioJobEnum>();
        public List<string> _prioByPlayer = new List<string>();
        public List<int> _prioByPlCustom = new List<int>();

        public AutomarkerPrio()
        {
            foreach (PrioTrinityEnum e in Enum.GetValues(typeof(PrioTrinityEnum)))
            {
                _prioByTrinity.Add(e);
            }
            foreach (PrioRoleEnum e in Enum.GetValues(typeof(PrioRoleEnum)))
            {
                _prioByRole.Add(e);
            }
            _prioByJob.AddRange(new PrioJobEnum[] {
                PrioJobEnum.PLD, PrioJobEnum.WAR, PrioJobEnum.DRK, PrioJobEnum.GNB,
                PrioJobEnum.WHM, PrioJobEnum.SCH, PrioJobEnum.AST, PrioJobEnum.SGE,
                PrioJobEnum.MNK, PrioJobEnum.DRG, PrioJobEnum.NIN, PrioJobEnum.SAM, PrioJobEnum.RPR, PrioJobEnum.VPR,
                PrioJobEnum.BRD, PrioJobEnum.MCH, PrioJobEnum.DNC,
                PrioJobEnum.BLM, PrioJobEnum.PCT, PrioJobEnum.SMN, PrioJobEnum.RDM,
            } );
            for (int i = 1; i <= 8; i++)
            {
                _prioByPlCustom.Add(i);
            }
        }

        internal static PrioRoleEnum JobToRole(uint job)
        {
            switch ((PrioJobEnum)job)
            {
                case PrioJobEnum.PLD: case PrioJobEnum.WAR: case PrioJobEnum.DRK: case PrioJobEnum.GNB:
                    return PrioRoleEnum.Tank;
                case PrioJobEnum.WHM: case PrioJobEnum.SCH: case PrioJobEnum.AST: case PrioJobEnum.SGE:
                    return PrioRoleEnum.Healer;
                case PrioJobEnum.MNK: case PrioJobEnum.DRG: case PrioJobEnum.NIN: case PrioJobEnum.SAM: case PrioJobEnum.RPR: case PrioJobEnum.VPR:
                    return PrioRoleEnum.Melee;
                case PrioJobEnum.BRD: case PrioJobEnum.MCH: case PrioJobEnum.DNC:
                    return PrioRoleEnum.Ranged;
                default:
                    return PrioRoleEnum.Caster;
            }
        }

        internal static PrioTrinityEnum JobToTrinity(uint job)
        {
            switch ((PrioJobEnum)job)
            {
                case PrioJobEnum.PLD: case PrioJobEnum.WAR: case PrioJobEnum.DRK: case PrioJobEnum.GNB:
                    return PrioTrinityEnum.Tank;
                case PrioJobEnum.WHM: case PrioJobEnum.SCH: case PrioJobEnum.AST: case PrioJobEnum.SGE:
                    return PrioTrinityEnum.Healer;
                default:
                    return PrioTrinityEnum.DPS;
            }
        }

        internal static PrioArchetypeEnum JobToArchetype(uint job)
        {
            switch ((PrioJobEnum)job)
            {
                case PrioJobEnum.PLD: case PrioJobEnum.WAR: case PrioJobEnum.DRK: case PrioJobEnum.GNB:
                case PrioJobEnum.WHM: case PrioJobEnum.SCH: case PrioJobEnum.AST: case PrioJobEnum.SGE:
                    return PrioArchetypeEnum.Support;
                default:
                    return PrioArchetypeEnum.DPS;
            }
        }

        public void SortByPriority(List<Party.PartyMember> actors)
        {
            switch (Priority)
            {
                case PrioTypeEnum.PartyListOrder:
                    {
                        actors.Sort(
                            (a, b) => Reversed == false ? 
                            a.Index.CompareTo(b.Index) : 
                            b.Index.CompareTo(a.Index)
                        );
                        break;
                    }
                case PrioTypeEnum.PartyListCustom:
                    {
                        int i = 1;
                        List<Tuple<int, int>> key = new List<Tuple<int, int>>(
                            from ix in _prioByPlCustom
                            let idx = i++
                            select new Tuple<int, int>(idx, ix)
                        );
                        actors.Sort(
                            (a, b) =>
                            {
                                int p1 = (from ix in key where ix.Item2 == a.Index select ix.Item1).FirstOrDefault();
                                int p2 = (from ix in key where ix.Item2 == b.Index select ix.Item1).FirstOrDefault();
                                if (p1 != p2)
                                {
                                    return p1.CompareTo(p2);
                                }
                                return a.Index.CompareTo(b.Index);
                            }
                        );
                        break;
                    }
                case PrioTypeEnum.Alphabetic:
                    {
                        actors.Sort(
                            (a, b) => Reversed == false ?
                            a.Name.CompareTo(b.Name) :
                            b.Name.CompareTo(a.Name)
                        );
                        break;
                    }
                case PrioTypeEnum.Trinity:
                    {
                        int i = 1;
                        List<Tuple<int, PrioTrinityEnum>> key = new List<Tuple<int, PrioTrinityEnum>>(
                            from ix in _prioByTrinity let idx = i++
                            select new Tuple<int, PrioTrinityEnum>(idx, ix)
                        );
                        actors.Sort(
                            (a, b) =>
                            {
                                int p1 = (from ix in key where ix.Item2 == JobToTrinity(a.Job) select ix.Item1).FirstOrDefault();
                                int p2 = (from ix in key where ix.Item2 == JobToTrinity(b.Job) select ix.Item1).FirstOrDefault();
                                if (p1 != p2)
                                {
                                    return p1.CompareTo(p2);
                                }
                                return a.Index.CompareTo(b.Index);
                            }
                        );
                        break;
                    }
                case PrioTypeEnum.Role:
                    {
                        int i = 1;
                        List<Tuple<int, PrioRoleEnum>> key = new List<Tuple<int, PrioRoleEnum>>(
                            from ix in _prioByRole
                            let idx = i++
                            select new Tuple<int, PrioRoleEnum>(idx, ix)
                        );
                        actors.Sort(
                            (a, b) =>
                            {
                                int p1 = (from ix in key where ix.Item2 == JobToRole(a.Job) select ix.Item1).FirstOrDefault();
                                int p2 = (from ix in key where ix.Item2 == JobToRole(b.Job) select ix.Item1).FirstOrDefault();
                                if (p1 != p2)
                                {
                                    return p1.CompareTo(p2);
                                }
                                return a.Index.CompareTo(b.Index);
                            }
                        );
                        break;
                    }
                case PrioTypeEnum.Job:
                    {
                        int i = 1;
                        List<Tuple<int, PrioJobEnum>> key = new List<Tuple<int, PrioJobEnum>>(
                            from ix in _prioByJob
                            let idx = i++
                            select new Tuple<int, PrioJobEnum>(idx, ix)
                        );
                        actors.Sort(
                            (a, b) =>
                            {
                                int p1 = (from ix in key where (uint)ix.Item2 == a.Job select ix.Item1).FirstOrDefault();
                                int p2 = (from ix in key where (uint)ix.Item2 == b.Job select ix.Item1).FirstOrDefault();
                                if (p1 != p2)
                                {
                                    return p1.CompareTo(p2);
                                }
                                return a.Index.CompareTo(b.Index);
                            }
                        );
                        break;
                    }
                case PrioTypeEnum.CongaX:
                    {
                        actors.Sort(
                            (a, b) => Reversed == false ? a.X.CompareTo(b.X) : b.X.CompareTo(a.X)
                        );
                        break;
                    }
                case PrioTypeEnum.CongaY:
                    {
                        actors.Sort(
                            (a, b) => Reversed == false ? a.Z.CompareTo(b.Z) : b.Z.CompareTo(a.Z)
                        );
                        break;
                    }
                case PrioTypeEnum.Player:
                    {
                        int i = 1;
                        List<Tuple<int, string>> key = new List<Tuple<int, string>>(
                            from ix in _prioByPlayer
                            let idx = i++
                            select new Tuple<int, string>(idx, ix)
                        );
                        actors.Sort(
                            (a, b) =>
                            {
                                int p1 = (from ix in key where String.Compare(ix.Item2, a.Name) == 0 select ix.Item1).FirstOrDefault();
                                int p2 = (from ix in key where String.Compare(ix.Item2, b.Name) == 0 select ix.Item1).FirstOrDefault();
                                if (p1 != p2)
                                {
                                    return p1.CompareTo(p2);
                                }
                                return a.Index.CompareTo(b.Index);
                            }
                        );
                        break;
                    }
                case PrioTypeEnum.Clockspots:
                    {
                        Dictionary<PrioDirectionEnum, Party.PartyMember> diractors = new Dictionary<PrioDirectionEnum, Party.PartyMember>();
                        float midx = actors.Count > 0 ? (from ax in actors select ax.X).Average() : 0.0f;
                        float midy = actors.Count > 0 ? (from ax in actors select ax.Z).Average() : 0.0f;
                        var angles = (from ax in actors select new { Object = ax, Angle = Math.Atan2(midy - ax.Z, midx - ax.X) }).ToList();
                        List<Party.PartyMember> plops = new List<Party.PartyMember>();
                        var west = (from ax in angles where Math.Abs(ax.Angle) < Math.PI / 8.0f orderby Math.Abs(ax.Angle) ascending select ax).FirstOrDefault();
                        plops.Add(west?.Object);
                        diractors[PrioDirectionEnum.W] = west?.Object;
                        var northwest = (from ax in angles where ax.Angle > 0.0f && plops.Contains(ax.Object) == false orderby ax.Angle ascending select ax).FirstOrDefault();
                        plops.Add(northwest?.Object);
                        diractors[PrioDirectionEnum.NW] = northwest?.Object;
                        var north = (from ax in angles where ax.Angle > Math.PI / 4.0f && plops.Contains(ax.Object) == false orderby ax.Angle ascending select ax).FirstOrDefault();
                        plops.Add(north?.Object);
                        diractors[PrioDirectionEnum.N] = north?.Object;
                        var northeast = (from ax in angles where ax.Angle > Math.PI / 2.0f && plops.Contains(ax.Object) == false orderby ax.Angle ascending select ax).FirstOrDefault();
                        plops.Add(northeast?.Object);
                        diractors[PrioDirectionEnum.NE] = northeast?.Object;
                        var east = (from ax in angles where Math.Abs(ax.Angle) >= Math.PI - (Math.PI / 8.0f) && plops.Contains(ax.Object) == false orderby Math.Abs(ax.Angle) descending select ax).FirstOrDefault();
                        plops.Add(east?.Object);
                        diractors[PrioDirectionEnum.E] = east?.Object;
                        var southeast = (from ax in angles where ax.Angle > 0.0f - Math.PI && plops.Contains(ax.Object) == false orderby ax.Angle ascending select ax).FirstOrDefault();
                        plops.Add(southeast?.Object);
                        diractors[PrioDirectionEnum.SE] = southeast?.Object;
                        var south = (from ax in angles where ax.Angle > (Math.PI / 4.0f) - Math.PI && plops.Contains(ax.Object) == false orderby ax.Angle ascending select ax).FirstOrDefault();
                        plops.Add(south?.Object);
                        diractors[PrioDirectionEnum.S] = south?.Object;
                        var southwest = (from ax in angles where ax.Angle > (Math.PI / 2.0f) - Math.PI && plops.Contains(ax.Object) == false orderby ax.Angle ascending select ax).FirstOrDefault();
                        plops.Add(southwest?.Object);
                        diractors[PrioDirectionEnum.SW] = southwest?.Object;
                        foreach (Party.PartyMember pm in actors)
                        {
                            if (plops.Contains(pm) == false)
                            {
                                for (int i = 1; i <= 8; i++)
                                {
                                    if (diractors[(PrioDirectionEnum)i] == null)
                                    {
                                        diractors[(PrioDirectionEnum)i] = pm;
                                    }
                                }
                            }
                        }
                        int k = (int)StartingFrom;
                        for (int j = 1; j < 8; j++)
                        {
                            if (diractors[(PrioDirectionEnum)k] != null)
                            {
                                diractors[(PrioDirectionEnum)k].Index = j;
                            }
                            if (Reversed == true)
                            {
                                k--;
                                if (k < 1)
                                {
                                    k = 8;
                                }
                            }
                            else
                            {
                                k++;
                                if (k > 8)
                                {
                                    k = 1;
                                }
                            }
                        }
                        actors.Sort((a, b) => a.Index.CompareTo(b.Index));
                        break;
                    }
            }
        }

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(String.Format("Priority={0};", Priority));
            sb.Append(String.Format("Reversed={0};", Reversed));
            sb.Append(String.Format("StartingFrom={0};", StartingFrom));
            sb.Append(String.Format("Trinity={0};", String.Join(",", from ix in _prioByTrinity select ix.ToString())));
            sb.Append(String.Format("Role={0};", String.Join(",", from ix in _prioByRole select ix.ToString())));
            sb.Append(String.Format("Job={0};", String.Join(",", from ix in _prioByJob select ix.ToString())));
            sb.Append(String.Format("Player={0};", String.Join(",", from ix in _prioByPlayer select Plugin.Base64Encode(ix))));
            sb.Append(String.Format("PlCustom={0};", String.Join(",", from ix in _prioByPlCustom select ix.ToString())));
            return sb.ToString();
        }

        public void Deserialize(string data)
        {
            string[] items = data.Split(";");
            foreach (string item in items)
            {
                string[] kp = item.Split("=", 2);
                switch (kp[0])
                {
                    case "Priority":
                        {
                            Priority = (PrioTypeEnum)Enum.Parse(typeof(PrioTypeEnum), kp[1]);
                            break;
                        }
                    case "StartingFrom":
                        {
                            StartingFrom = (PrioDirectionEnum)Enum.Parse(typeof(PrioDirectionEnum), kp[1]);
                            break;
                        }
                    case "Reversed":
                        {
                            Reversed = bool.Parse(kp[1]);
                            break;
                        }
                    case "Player":
                        {
                            _prioByPlayer = kp[1].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(a => Plugin.Base64Decode(a)).ToList();
                        }
                        break;
                    case "Trinity":
                        {
                            List<PrioTrinityEnum> temp = new List<PrioTrinityEnum>(
                                kp[1].Split(",").Select(a => (PrioTrinityEnum)Enum.Parse(typeof(PrioTrinityEnum), a))
                            );
                            temp.AddRange(from ix in _prioByTrinity where temp.Contains(ix) == false select ix);
                            _prioByTrinity = temp;
                        }
                        break;
                    case "Role":
                        {
                            List<PrioRoleEnum> temp = new List<PrioRoleEnum>(
                                kp[1].Split(",").Select(a => (PrioRoleEnum)Enum.Parse(typeof(PrioRoleEnum), a))
                            );
                            temp.AddRange(from ix in _prioByRole where temp.Contains(ix) == false select ix);
                            _prioByRole = temp;
                        }
                        break;
                    case "Job":
                        {
                            List<PrioJobEnum> temp = new List<PrioJobEnum>(
                                kp[1].Split(",").Select(a => (PrioJobEnum)Enum.Parse(typeof(PrioJobEnum), a))
                            );
                            temp.AddRange(from ix in _prioByJob where temp.Contains(ix) == false select ix);
                            if (temp.Contains(PrioJobEnum.VPR) == false)
                            {
                                int jidx = temp.IndexOf(PrioJobEnum.RPR);
                                if (jidx >= 0)
                                {
                                    temp.Insert(jidx + 1, PrioJobEnum.VPR);
                                }
                            }
                            if (temp.Contains(PrioJobEnum.PCT) == false)
                            {
                                int jidx = temp.IndexOf(PrioJobEnum.BLM);
                                if (jidx >= 0)
                                {
                                    temp.Insert(jidx + 1, PrioJobEnum.PCT);
                                }
                            }
                            _prioByJob = temp;
                        }
                        break;
                    case "PlCustom":
                        {
                            _prioByPlCustom = kp[1].Split(",", StringSplitOptions.RemoveEmptyEntries).Select(a => int.Parse(a)).ToList();
                        }
                        break;
                }
            }
        }

    }

}
