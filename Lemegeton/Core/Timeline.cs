using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Xml.Serialization;
using static Lemegeton.Core.State;
using static Lemegeton.Core.Timeline.Encounter;


namespace Lemegeton.Core
{

    public class Timeline
    {

        public class Encounter
        {

            public class Trigger
            {

                [Flags]
                public enum EventTypeEnum                
                {
                    None = 0x00,
                    Start = 0x01,
                    Stop = 0x02,
                    Select = 0x04,
                    All = 0xff,
                }

                public enum TriggerTypeEnum
                {
                    None,
                    Default,
                    OnSelect,
                    OnCombatStart,
                    OnCombatEnd,
                    OnCastBegin,
                    OnCastEnd,
                    Spawn,
                    Pulled,
                }

                [XmlAttribute]
                public EventTypeEnum Type { get; set; } = EventTypeEnum.None;

                [XmlAttribute]
                public TriggerTypeEnum EventType { get; set; } = TriggerTypeEnum.None;

                [XmlAttribute]
                public int NameId { get; set; } = 0;

                [XmlAttribute]
                public int HP { get; set; } = 0;

                [XmlAttribute]
                public int AbilityId { get; set; } = 0;

            }

            [XmlAttribute]
            public int Id { get; set; } = 0;
            [XmlAttribute]
            public string Description { get; set; } = null;
            [XmlAttribute]
            public float StartTime { get; set; } = 0.0f;

            public List<Trigger> Triggers { get; set; } = new List<Trigger>();
            public List<Entry> Entries { get; set; } = new List<Entry>();

            public Encounter()
            {
            }

            public Trigger.EventTypeEnum GetEvents(Trigger.TriggerTypeEnum eventType, uint NameId, uint HP, uint AbilityId)
            {
                return (from ix in Triggers
                        where (
                            (ix.EventType == eventType)
                            &&
                            (
                                (
                                    ix.EventType != Trigger.TriggerTypeEnum.OnCastBegin
                                    &&
                                    ix.EventType != Trigger.TriggerTypeEnum.OnCastEnd
                                )
                                ||
                                (
                                    ix.AbilityId > 0 && ix.AbilityId == AbilityId
                                    &&
                                    (ix.NameId == 0 || (ix.NameId > 0 && ix.NameId == NameId))
                                )
                            )
                            &&
                            (                                
                                (
                                    ix.EventType != Trigger.TriggerTypeEnum.Spawn
                                    &&
                                    ix.EventType != Trigger.TriggerTypeEnum.Pulled
                                )
                                ||
                                (
                                    (ix.NameId > 0 || ix.HP > 0)
                                    &&
                                    (
                                        (ix.NameId == 0 || (ix.NameId > 0 && ix.NameId == NameId))
                                        &&
                                        (ix.HP == 0 || (ix.HP > 0 && ix.HP == HP))
                                    )
                                )
                            )
                        )
                        select ix.Type).Aggregate(Trigger.EventTypeEnum.None, (a, b) => a | b);
            }

        }

        public class Handler
        {

            public enum HandlerTypeEnum
            {
                Undefined,
                JumpToTime,
                JumpToEncounter,
                JumpToEntry,
                SetVariable,
                ClearVariable,
            }

            [XmlAttribute]
            public HandlerTypeEnum Type { get; set; } = HandlerTypeEnum.Undefined;
            [XmlAttribute]
            public string Name { get; set; } = null;
            [XmlAttribute]
            public string Value { get; set; } = null;

            public int ValueAsInt(int defValue = 0)
            {
                if (int.TryParse(Value, out int val) == true)
                {
                    return val;
                }
                return defValue;
            }

            public float ValueAsFloat(int defValue = 0)
            {
                if (float.TryParse(Value, out float val) == true)
                {
                    return val;
                }
                return defValue;
            }

            public Guid ValueAsGuid(Guid defValue)
            {
                if (Guid.TryParse(Value, out Guid val) == true)
                {
                    return val;
                }
                return defValue;
            }

        }

        public class Entry
        {

            public enum EntryTypeEnum
            {
                Undefined,
                Targettable,
                Untargettable,
                Timed,
                Ability,
                Spawn,
            }

            public class ProfileSettings
            {

                [XmlAttribute]
                public bool ShowOverlay { get; set; } = true;
                [XmlAttribute]
                public bool ReactionsActive { get; set; } = true;

                public List<Reaction> Reactions { get; set; } = new List<Reaction>();

                public ProfileSettings Duplicate()
                {
                    ProfileSettings temp = (ProfileSettings)MemberwiseClone();
                    temp.Reactions = new List<Reaction>();
                    foreach (Reaction r in Reactions)
                    {
                        temp.Reactions.Add(r.Duplicate());
                    }
                    return temp;
                }

            }

            [XmlAttribute]
            public Guid Id { get; set; } = Guid.NewGuid();
            [XmlAttribute]
            public string Description { get; set; } = null;
            [XmlAttribute]
            public float StartTime { get; set; } = 0.0f;
            [XmlAttribute]
            public float Duration { get; set; } = 0.0f;
            [XmlAttribute]
            public EntryTypeEnum Type { get; set; } = EntryTypeEnum.Timed;
            [XmlAttribute]
            public float WindowStart { get; set; } = -1.5f;
            [XmlAttribute]
            public float WindowEnd { get; set; } = 1.5f;
            [XmlAttribute]
            public bool Hidden { get; set; } = false;

            [XmlAttribute]
            public string Keys
            {
                get
                {
                    return String.Join(",", Array.ConvertAll<uint, string>(KeyValues.ToArray(), x => x.ToString()));
                }
                set
                {
                    KeyValues.Clear();
                    if (value != null)
                    {
                        KeyValues.AddRange(
                            Array.ConvertAll<string, uint>(
                                value.Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                                x => uint.Parse(x))
                        );
                    }
                }
            }

            internal List<uint> KeyValues { get; set; } = new List<uint>();
            public List<Handler> Handlers { get; set; } = null;

            internal float EndTime { get { return StartTime + Duration; } }
            internal string CachedName { get; set; } = null;
            internal float EffectiveStart { get { return StartTime + WindowStart; } }
            internal float EffectiveEnd { get { return EndTime + WindowEnd; } }

            internal List<ProfileSettings> Settings { get; set; } = new List<ProfileSettings>();

            public bool VisibleOnTimeline()
            {
                if (Hidden == true)
                {
                    return false;
                }
                foreach (ProfileSettings p in Settings)
                {
                    if (p.ShowOverlay == false)
                    {
                        return false;
                    }
                }
                return true;
            }

            public bool ReactionsActive()
            {
                foreach (ProfileSettings p in Settings)
                {
                    if (p.ReactionsActive == false)
                    {
                        return false;
                    }
                }
                return true;
            }

        }
        
        public class Profile
        {

            [XmlAttribute]
            public bool Default { get; set; } = false;
            [XmlAttribute]
            public string Name { get; set; } = "(undefined)";
            [XmlAttribute]
            public bool ShowOverlay { get; set; } = true;
            [XmlAttribute]
            public bool ReactionsActive { get; set; } = true;
            [XmlAttribute]
            public bool ApplyAlways { get; set; } = true;
            [XmlAttribute]
            public bool ApplyAutomatically { get; set; } = false;
            [XmlAttribute]
            public ulong ApplyOnJobs { get; set; } = 0;

            public SerializableDictionary<Guid, Entry.ProfileSettings> EntrySettings = new SerializableDictionary<Guid, Entry.ProfileSettings>();

            public bool AppliesToJob(ulong jobId)
            {
                return ApplyAlways == true || (ApplyAutomatically == true && ((ApplyOnJobs >> (int)jobId) & 0x1UL) != 0);
            }

            public Profile Duplicate()
            {
                Profile pro = (Profile)MemberwiseClone();
                pro.EntrySettings = new SerializableDictionary<Guid, Entry.ProfileSettings>();
                foreach (var evr in EntrySettings)
                {
                    pro.EntrySettings[evr.Key] = evr.Value.Duplicate();
                }
                return pro;
            }

            internal void AddReactionForEntry(Entry e, Reaction r)
            {
                if (e == null || r == null)
                {
                    return;
                }
                Entry.ProfileSettings ep = GetSettingsForEntry(e);
                ep.Reactions.Add(r);
            }

            internal void RemoveReactionFromEntry(Entry e, Reaction r)
            {
                if (e == null || r == null)
                {
                    return;
                }
                Entry.ProfileSettings ep = GetSettingsForEntry(e);
                if (ep.Reactions.Contains(r) == true)
                {
                    ep.Reactions.Remove(r);
                }                
            }

            public Entry.ProfileSettings GetSettingsForEntry(Entry e)
            {
                if (e == null)
                {
                    return null;
                }
                if (EntrySettings.TryGetValue(e.Id, out Entry.ProfileSettings pro) == true)
                {
                    return pro;
                }
                Entry.ProfileSettings ep = new Entry.ProfileSettings();
                EntrySettings[e.Id] = ep;
                return ep;
            }

        }

        public class Reaction
        {

            public enum ReactionTriggerEnum
            {
                Timed,
                OnCastBegin,
                OnCastEnd,
                Targettable,
                Untargettable,
                Spawn,
            }

            [XmlAttribute]
            public float TimeWindowStart { get; set; } = 0.0f;
            [XmlAttribute]
            public float TimeWindowEnd { get; set; } = 0.0f;
            [XmlAttribute]
            public string Name { get; set; } = "(undefined)";
            [XmlAttribute]
            public ReactionTriggerEnum Trigger { get; set; } = ReactionTriggerEnum.Timed;

            internal Guid Id { get; set; } = Guid.NewGuid();
            internal bool Fired { get; set; } = false;

            internal float EffectiveTime
            {
                get
                {
                    Random rng = new Random();
                    double rngv = rng.NextDouble();
                    return TimeWindowStart + (float)((TimeWindowEnd - TimeWindowStart) * rngv);
                }
            }

            [XmlElement(ElementName = "ChatMessage", Type = typeof(Lemegeton.Action.ChatMessage))]
            [XmlElement(ElementName = "Notification", Type = typeof(Lemegeton.Action.Notification))]
            [XmlElement(ElementName = "IngameCommand", Type = typeof(Lemegeton.Action.IngameCommand))]
            public List<Action> ActionList { get; set; } = new List<Action>();

            public void Execute(Context ctx)
            {
                foreach (Action a in ActionList)
                {
                    a.Execute(ctx);
                }
            }

            public Reaction Duplicate()
            {
                Reaction r = (Reaction)MemberwiseClone();
                r.ActionList = new List<Action>();
                foreach (Action a in ActionList)
                {
                    r.ActionList.Add(a.Duplicate());
                }
                return r;
            }

        }

        public enum StatusEnum
        {
            Stopped,
            Running,
        }

        [XmlAttribute]
        public Guid Id = Guid.NewGuid();
        [XmlAttribute]
        public ushort Territory { get; set; }
        [XmlAttribute]
        public string Description { get; set; } = "(undefined)";

        internal string Filename = null;
        internal DateTime LastModified = DateTime.MinValue;

        internal Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        public List<Encounter> Encounters { get; set; } = new List<Encounter>();
        internal List<Profile> Profiles { get; set; } = new List<Profile>();
        internal Profile DefaultProfile { get; set; } = new Profile() { Default = true };

        private Encounter _CurrentEncounter = null;
        internal Encounter CurrentEncounter
        {
            get
            {
                return _CurrentEncounter;
            }
            set
            {
                _CurrentEncounter = value;
                CurrentTime = _CurrentEncounter != null ? _CurrentEncounter.StartTime : 0.0f;
                AutoSync = 0.0f;
                LastJumpPoint = _CurrentEncounter != null ? _CurrentEncounter.StartTime : 0.0f;
            }
        }

        internal StatusEnum Status { get; set; } = StatusEnum.Stopped;
        internal float AutoSync { get; set; } = 0.0f;
        internal float CurrentTime { get; set; } = 0.0f;
        internal float LastJumpPoint { get; set; } = 0.0f;
        internal string CachedName { get; set; } = null;

        internal List<Profile> SelectedProfiles { get; set; } = new List<Profile>();

        public IEnumerable<Encounter> GetEncountersForEvents(Encounter.Trigger.EventTypeEnum type, Encounter.Trigger.TriggerTypeEnum eventType, uint nameId, uint hp, uint abilityId)
        {
            return from ix in Encounters where (ix.GetEvents(eventType, nameId, hp, abilityId) & type) != 0 select ix;
        }

        public bool IsOverlayVisible()
        {
            if (DefaultProfile.ShowOverlay == false)
            {
                return false;
            }
            foreach (Profile p in SelectedProfiles)
            {
                if (p.ShowOverlay == false)
                {
                    return false;
                }
            }
            return true;
        }

        public void AddSelectedProfile(Profile p)
        {
            SelectedProfiles.Add(p);
            ApplyProfileSelection();
        }

        public void ClearSelectedProfiles()
        {
            SelectedProfiles.Clear();
            ApplyProfileSelection();
        }

        private void ApplyProfileSelection()
        {
            foreach (Encounter enc in Encounters)
            {
                foreach (Entry e in enc.Entries)
                {
                    e.Settings.Clear();
                    ApplyProfileSelection(e, DefaultProfile);
                    foreach (Profile p in SelectedProfiles)
                    {
                        ApplyProfileSelection(e, p);
                    }
                }
            }
        }

        private void ApplyProfileSelection(Entry e, Profile p)
        {
            Entry.ProfileSettings pro;
            if (p.EntrySettings.TryGetValue(e.Id, out pro) == false)
            {
                pro = new Entry.ProfileSettings();
                p.EntrySettings[e.Id] = pro;
            }
            e.Settings.Add(pro);
        }        

        public void Reset(State st)
        {
            Status = StatusEnum.Stopped;
            IEnumerable<Encounter> defaults = GetEncountersForEvents(Encounter.Trigger.EventTypeEnum.Select, Encounter.Trigger.TriggerTypeEnum.Default, 0, 0, 0);
            Encounter ec = defaults.FirstOrDefault();
            if (ec != null)
            {
                st.Log(LogLevelEnum.Debug, null, "Timeline resetting to default encounter {0}", ec.Id);
                CurrentEncounter = ec;
            }
            else
            {
                if (CurrentEncounter != null)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline resetting to current encounter {0}", ec.Id);
                    CurrentEncounter = CurrentEncounter;
                }
                else
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline resetting to default time");
                    CurrentTime = 0.0f;
                    LastJumpPoint = CurrentTime;
                    AutoSync = 0.0f;
                }
            }
            Random rng = new Random();
            foreach (Encounter enc in Encounters)
            {
                foreach (Entry e in enc.Entries)
                {
                    foreach (Entry.ProfileSettings ps in e.Settings)
                    {
                        foreach (Reaction r in ps.Reactions)
                        {
                            r.Fired = false;
                        }
                    }
                }
            }
        }

        public int SelectProfiles(ulong jobId)
        {
            int num = 0;
            SelectedProfiles.Clear();
            foreach (Profile p in Profiles)
            {
                if (p.AppliesToJob(jobId) == true)
                {
                    SelectedProfiles.Add(p);
                    num++;
                }
            }
            ApplyProfileSelection();
            return num;
        }

        public void AddProfile(Profile p, ulong jobId)
        {
            Profiles.Add(p);
            SelectProfiles(jobId);
        }

        public void RemoveProfile(Profile p, ulong jobId)
        {
            if (Profiles.Contains(p) == true)
            {
                Profiles.Remove(p);
            }
            if (SelectedProfiles.Contains(p) == true)
            {
                SelectProfiles(jobId);
            }
        }

        public void AdvanceTime(State st, float delta)
        {
            if (Status == StatusEnum.Stopped)
            {
                return;
            }
            CurrentTime += delta;
            delta = Math.Clamp(AutoSync / 100.0f, -0.005f, 0.005f);
            AutoSync -= delta;
            CurrentTime += delta;
            IEnumerable<Entry> entries = InCurrentWindow(0.0f, 30.0f);
            foreach (Entry e in entries)
            {
                if (e.ReactionsActive() == false)
                {
                    continue;
                }
                foreach (Entry.ProfileSettings pro in e.Settings)
                {
                    if (pro.ReactionsActive == false)
                    {
                        continue;
                    }
                    foreach (Reaction r in pro.Reactions)
                    {
                        if (r.Trigger != Reaction.ReactionTriggerEnum.Timed)
                        {
                            continue;
                        }                        
                        st.QueueReactionExecution(new Context { State = st }, r, e.StartTime - CurrentTime);
                    }
                }
            }
        }

        private IEnumerable<Entry> InCurrentTime(float timeBackward, float timeForward)
        {
            return from ix in CurrentEncounter.Entries
                   where (
                        CurrentTime >= ix.EffectiveStart - timeBackward && CurrentTime <= ix.EffectiveEnd + timeForward
                   )
                   select ix;
        }

        private IEnumerable<Entry> InCurrentWindow(float timeBackward, float timeForward)
        {
            return from ix in CurrentEncounter.Entries
                   where (
                        (ix.EffectiveStart >= CurrentTime && ix.EffectiveStart <= (CurrentTime + timeForward))
                        ||
                        (ix.EffectiveEnd >= CurrentTime && ix.EffectiveEnd <= (CurrentTime + timeForward))
                        ||
                        (CurrentTime >= ix.EffectiveEnd && CurrentTime - timeBackward <= ix.EffectiveEnd)
                   )
                   select ix;
        }

        public bool EventIsInVisibleWindow(Entry e, float timeBackward, float timeForward)
        {
            return (
                e.VisibleOnTimeline() == true
                &&
                (
                    (e.StartTime >= CurrentTime && e.StartTime <= (CurrentTime + timeForward))
                    ||
                    (e.EndTime >= CurrentTime && e.EndTime <= (CurrentTime + timeForward))
                    ||
                    (CurrentTime >= e.EndTime && CurrentTime - timeBackward <= e.EndTime)
                )
            );
        }

        public IEnumerable<Entry> PeekEntries(int maxAmount, float timeBackward, float timeForward)
        {
            return (from ix in CurrentEncounter.Entries where EventIsInVisibleWindow(ix, timeBackward, timeForward) == true select ix).Take(maxAmount);
        }

        public void ExecuteEntry(Context ctx, Entry e, Reaction.ReactionTriggerEnum rt, bool allowJump, bool allowSync, float syncOffset)
        {
            bool jumped = false;
            if (e.Handlers != null)
            {
                foreach (Handler h in e.Handlers)
                {
                    switch (h.Type)
                    {
                        default:
                        case Handler.HandlerTypeEnum.Undefined:
                            break;
                        case Handler.HandlerTypeEnum.JumpToEncounter:
                            if (allowJump == true)
                            {
                                int enc = h.ValueAsInt();
                                Encounter ec = (from ix in Encounters where ix.Id == enc select ix).FirstOrDefault();
                                if (ec != null)
                                {
                                    ctx.State.Log(LogLevelEnum.Debug, null, "Timeline jumping to encounter {0}", ec.Id);
                                    CurrentEncounter = ec;
                                    jumped = true;
                                }
                                else
                                {
                                    ctx.State.Log(LogLevelEnum.Debug, null, "Timeline tried to jump to encounter {0} but it has not been defined", enc);
                                }
                            }
                            break;
                        case Handler.HandlerTypeEnum.JumpToTime:
                            if (allowJump == true)
                            {
                                float time = h.ValueAsFloat();
                                ctx.State.Log(LogLevelEnum.Debug, null, "Timeline jumping to time {0}", time);
                                CurrentTime = time;
                                LastJumpPoint = time;
                                AutoSync = 0.0f;
                                jumped = true;
                            }
                            break;
                        case Handler.HandlerTypeEnum.JumpToEntry:
                            if (allowJump == true)
                            {
                                Guid eg = h.ValueAsGuid(Guid.Empty);
                                bool found = false;
                                foreach (Encounter enc in Encounters)
                                {
                                    foreach (Entry ent in enc.Entries)
                                    {
                                        if (ent.Id == eg)
                                        {
                                            ctx.State.Log(LogLevelEnum.Debug, null, "Timeline jumping to encounter {0}, entry {1} (at {2})", enc.Id, ent.Id, ent.StartTime);
                                            CurrentEncounter = enc;
                                            jumped = true;
                                            found = true;
                                            CurrentTime = ent.StartTime;
                                            LastJumpPoint = ent.StartTime;
                                            AutoSync = 0.0f;
                                        }
                                    }
                                }
                                if (found == false)
                                {
                                    ctx.State.Log(LogLevelEnum.Debug, null, "Timeline tried to jump to entry {0} but it has not been defined", eg);
                                }
                            }
                            break;
                        case Handler.HandlerTypeEnum.SetVariable:
                            string varn = h.Name != null ? h.Name : "";
                            Variables[varn] = h.Value != null ? h.Value : "";
                            break;
                        case Handler.HandlerTypeEnum.ClearVariable:
                            if (h.Name != null && Variables.ContainsKey(h.Name) == true)
                            {
                                Variables.Remove(h.Name);
                            }
                            break;
                    }
                }
            }
            if (jumped == false && allowSync == true)
            {
                ctx.State.Log(LogLevelEnum.Debug, null, "Timeline autosync to {0} ({1} from current time {2}, entry at {3}, sync offset {4})", e.StartTime + syncOffset, (e.StartTime + syncOffset) - CurrentTime, CurrentTime, e.StartTime, syncOffset);
                AutoSync = (e.StartTime + syncOffset) - CurrentTime;                
            }
            if (e.ReactionsActive() == false)
            {
                return;
            }
            foreach (Entry.ProfileSettings pro in e.Settings)
            {
                if (pro.ReactionsActive == false)
                {
                    continue;
                }
                foreach (Reaction r in pro.Reactions)
                {
                    if (r.Trigger != rt)
                    {
                        continue;
                    }
                    ctx.State.QueueReactionExecution(ctx, r, 0.0f);
                }
            }
        }

        public void FeedCombatStart(State st)
        {
            IEnumerable<Encounter> encs = GetEncountersForEvents(Encounter.Trigger.EventTypeEnum.All, Encounter.Trigger.TriggerTypeEnum.OnCombatStart, 0, 0, 0);
            foreach (Encounter enc in encs)
            {
                Encounter.Trigger.EventTypeEnum evs = enc.GetEvents(Encounter.Trigger.TriggerTypeEnum.OnCombatStart, 0, 0, 0);
                if ((evs & Encounter.Trigger.EventTypeEnum.Select) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline jumping to encounter {0} based on combat start", enc.Id);
                    CurrentEncounter = enc;
                }
                if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Start) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} starting based on combat start", enc.Id);
                    Status = StatusEnum.Running;
                }
                if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Stop) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} stopping based on combat start", enc.Id);
                    Status = StatusEnum.Stopped;
                }
            }
        }

        public void FeedCombatEnd(State st)
        {
            IEnumerable<Encounter> encs = GetEncountersForEvents(Encounter.Trigger.EventTypeEnum.All, Encounter.Trigger.TriggerTypeEnum.OnCombatEnd, 0, 0, 0);
            foreach (Encounter enc in encs)
            {
                Encounter.Trigger.EventTypeEnum evs = enc.GetEvents(Encounter.Trigger.TriggerTypeEnum.OnCombatEnd, 0, 0, 0);
                if ((evs & Encounter.Trigger.EventTypeEnum.Select) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline jumping to encounter {0} based on combat end", enc.Id);
                    CurrentEncounter = enc;
                }
                if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Start) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} starting based on combat end", enc.Id);
                    Status = StatusEnum.Running;
                }
                if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Stop) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} stopping based on combat end", enc.Id);
                    Status = StatusEnum.Stopped;
                }
            }
        }

        public void FeedPulled(State st, GameObject go)
        {
            if (go == null)
            {
                return;
            }
            if (go is Character)
            {
                Character ch = (Character)go;
                IEnumerable<Encounter> encs = GetEncountersForEvents(Encounter.Trigger.EventTypeEnum.All, Encounter.Trigger.TriggerTypeEnum.Pulled, ch.NameId, ch.MaxHp, 0);
                foreach (Encounter enc in encs)
                {
                    Encounter.Trigger.EventTypeEnum evs = enc.GetEvents(Encounter.Trigger.TriggerTypeEnum.Pulled, ch.NameId, ch.CurrentHp, 0);
                    if ((evs & Encounter.Trigger.EventTypeEnum.Select) != 0)
                    {
                        st.Log(LogLevelEnum.Debug, null, "Timeline jumping to encounter {0} based on pulled combatant {1} (name ID {2}, hp {3})", enc.Id, go, ch.NameId, ch.MaxHp);
                        CurrentEncounter = enc;
                    }
                    if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Start) != 0)
                    {
                        st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} starting based on pulled combatant {1} (name ID {2}, hp {3})", enc.Id, go, ch.NameId, ch.MaxHp);
                        Status = StatusEnum.Running;
                    }
                    if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Stop) != 0)
                    {
                        st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} stopping based on pulled combatant {1} (name ID {2}, hp {3})", enc.Id, go, ch.NameId, ch.MaxHp);
                        Status = StatusEnum.Stopped;
                    }
                }
            }
        }

        public void FeedNewCombatant(State st, GameObject go)
        {
            if (go == null)
            {
                return;
            }
            if (go is Character)
            {
                Character ch = (Character)go;
                IEnumerable<Encounter> encs = GetEncountersForEvents(Encounter.Trigger.EventTypeEnum.All, Encounter.Trigger.TriggerTypeEnum.Spawn, ch.NameId, ch.CurrentHp, 0);
                foreach (Encounter enc in encs)
                {
                    Encounter.Trigger.EventTypeEnum evs = enc.GetEvents(Encounter.Trigger.TriggerTypeEnum.Spawn, ch.NameId, ch.CurrentHp, 0);
                    if ((evs & Encounter.Trigger.EventTypeEnum.Select) != 0)
                    {
                        st.Log(LogLevelEnum.Debug, null, "Timeline jumping to encounter {0} based on new combatant {1} (name ID {2}, hp {3})", enc.Id, go, ch.NameId, ch.CurrentHp);
                        CurrentEncounter = enc;
                    }
                    if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Start) != 0)
                    {
                        st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} starting based on new combatant {1} (name ID {2}, hp {3})", enc.Id, go, ch.NameId, ch.CurrentHp);
                        Status = StatusEnum.Running;
                    }
                    if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Stop) != 0)
                    {
                        st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} stopping based on new combatant {1} (name ID {2}, hp {3})", enc.Id, go, ch.NameId, ch.CurrentHp);
                        Status = StatusEnum.Stopped;
                    }
                }
                uint nameId = ch.NameId;
                IEnumerable<Entry> entries = InCurrentTime(0.0f, 0.0f);
                foreach (Entry e in entries)
                {
                    if (e.Type != Entry.EntryTypeEnum.Spawn)
                    {
                        continue;
                    }
                    if (e.KeyValues.Contains(nameId) == false)
                    {
                        continue;
                    }
                    Context ctx = new Context { State = st };
                    ctx.SourceName = go.Name.ToString();
                    ExecuteEntry(ctx, e, Reaction.ReactionTriggerEnum.Spawn, true, true, 0.0f);
                }
            }
        }

        public void FeedEventTargettable(State st)
        {
            IEnumerable<Entry> entries = InCurrentTime(0.0f, 0.0f);
            foreach (Entry e in entries)
            {
                if (e.Type != Entry.EntryTypeEnum.Targettable)
                {
                    continue;
                }
                Context ctx = new Context { State = st };
                ExecuteEntry(ctx, e, Reaction.ReactionTriggerEnum.Targettable, true, true, 0.0f);
            }
        }

        public void FeedEventUntargettable(State st)
        {
            IEnumerable<Entry> entries = InCurrentTime(0.0f, 0.0f);
            foreach (Entry e in entries)
            {
                if (e.Type != Entry.EntryTypeEnum.Untargettable)
                {
                    continue;
                }
                Context ctx = new Context { State = st };
                ExecuteEntry(ctx, e, Reaction.ReactionTriggerEnum.Untargettable, true, true, 0.0f);
            }
        }

        public void FeedEventCastBegin(State st, GameObject src, GameObject dest, uint abilityId, float castTime)
        {
            uint nameId = 0;
            if (dest is Character)
            {
                Character ch = (Character)dest;
                nameId = ch.NameId;
            }
            IEnumerable<Encounter> encs = GetEncountersForEvents(Encounter.Trigger.EventTypeEnum.All, Encounter.Trigger.TriggerTypeEnum.OnCastBegin, nameId, 0, abilityId);
            foreach (Encounter enc in encs)
            {
                Encounter.Trigger.EventTypeEnum evs = enc.GetEvents(Encounter.Trigger.TriggerTypeEnum.OnCastBegin, nameId, 0, abilityId);
                if ((evs & Encounter.Trigger.EventTypeEnum.Select) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline jumping to encounter {0} based on ability {1} cast begin by {2}", enc.Id, abilityId, nameId);
                    CurrentEncounter = enc;
                }
                if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Start) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} starting based on ability {1} cast begin by {2}", enc.Id, abilityId, nameId);
                    Status = StatusEnum.Running;
                }
                if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Stop) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} stopping based on ability {1} cast begin by {2}", enc.Id, abilityId, nameId);
                    Status = StatusEnum.Stopped;
                }
            }
            IEnumerable<Entry> entries = InCurrentTime(castTime, 0.0f);
            foreach (Entry e in entries)
            {
                if (e.Type != Entry.EntryTypeEnum.Ability)
                {
                    continue;
                }
                if (e.KeyValues.Contains(abilityId) == false)
                {
                    continue;
                }
                Context ctx = new Context { State = st };
                ctx.SourceName = src.Name.ToString();
                ctx.EffectName = st.plug.GetActionName(abilityId);
                ctx.DestName = dest.Name.ToString();
                ExecuteEntry(ctx, e, Reaction.ReactionTriggerEnum.OnCastBegin, false, true, 0.0f - castTime);
            }
        }

        public void FeedEventCastEnd(State st, GameObject src, GameObject dest, uint abilityId)
        {
            uint nameId = 0;
            if (dest is Character)
            {
                Character ch = (Character)dest;
                nameId = ch.NameId;
            }
            IEnumerable<Encounter> encs = GetEncountersForEvents(Encounter.Trigger.EventTypeEnum.All, Encounter.Trigger.TriggerTypeEnum.OnCastEnd, nameId, 0, abilityId);
            foreach (Encounter enc in encs)
            {
                Encounter.Trigger.EventTypeEnum evs = enc.GetEvents(Encounter.Trigger.TriggerTypeEnum.OnCastEnd, nameId, 0, abilityId);
                if ((evs & Encounter.Trigger.EventTypeEnum.Select) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline jumping to encounter {0} based on ability {1} cast end by {2}", enc.Id, abilityId, nameId);
                    CurrentEncounter = enc;
                }
                if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Start) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} starting based on ability {1} cast end by {2}", enc.Id, abilityId, nameId);
                    Status = StatusEnum.Running;
                }
                if (CurrentEncounter == enc && (evs & Encounter.Trigger.EventTypeEnum.Stop) != 0)
                {
                    st.Log(LogLevelEnum.Debug, null, "Timeline encounter {0} stopping based on ability {1} cast end by {2}", enc.Id, abilityId, nameId);
                    Status = StatusEnum.Stopped;
                }
            }
            IEnumerable<Entry> entries = InCurrentTime(0.0f, 0.0f);
            foreach (Entry e in entries)
            {
                if (e.Type != Entry.EntryTypeEnum.Ability)
                {
                    continue;
                }
                if (e.KeyValues.Contains(abilityId) == false)
                {
                    continue;
                }
                Context ctx = new Context { State = st };
                ctx.SourceName = src.Name.ToString();
                ctx.EffectName = st.plug.GetActionName(abilityId);
                ctx.DestName = dest.Name.ToString();
                ExecuteEntry(ctx, e, Reaction.ReactionTriggerEnum.OnCastEnd, true, true, 0.0f);
            }
        }

    }

}
