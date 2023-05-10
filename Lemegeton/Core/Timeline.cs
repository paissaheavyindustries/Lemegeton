using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lemegeton.Core
{

    public class Timeline
    {

        public class Profile
        {

            public string Name { get; set; }

        }

        internal float CurrentTime { get; set; } = 0.0f;

        public enum EntryTypeEnum
        {
            Undefined,
            Timed,
            OnCastBegin
        }

        public enum ActionTypeEnum
        {
            Undefined,
            Jump
        }

        public class Action
        {

            internal Entry Owner { get; set; } = null;
            public Guid Id { get; set; } = Guid.NewGuid();
            public ActionTypeEnum Type { get; set; } = ActionTypeEnum.Undefined;
            public float TargetTime { get; set; } = 0.0f;
            public bool Builtin { get; set; } = true;

            public void Execute()
            {
                switch (Type)
                {
                    case ActionTypeEnum.Jump:
                        Owner.Owner.CurrentTime = TargetTime;
                        break;
                }
            }

        }

        public class Entry
        {

            internal Timeline Owner { get; set; } = null;
            public Guid Id { get; set; } = Guid.NewGuid();
            public string Description { get; set; } = null;
            public float StartTime { get; set; } = 0.0f;
            internal float EndTime { get { return StartTime + Duration; } }
            public float Duration { get; set; } = 0.0f;
            public uint Key { get; set; } = 0;
            public EntryTypeEnum Type { get; set; } = EntryTypeEnum.Timed;
            public float WindowStart { get; set; } = -0.5f;
            public float WindowEnd { get; set; } = 0.5f;
            internal string CachedName { get; set; } = null;

            public List<Action> Actions { get; set; } = new List<Action>();

            internal float EffectiveStart { get { return StartTime + WindowStart; } }
            internal float EffectiveEnd { get { return EndTime + WindowEnd; } }

            public void Execute()
            {
                foreach (Action a in Actions)
                {
                    a.Execute();
                }
            }

        }

        internal string CachedName { get; set; } = null;
        public ushort Territory { get; set; }
        public string Description { get; set; } = "(undefined)";
        public List<Entry> Entries { get; set; } = new List<Entry>();
        public List<Profile> Profiles { get; set; } = new List<Profile>();

        public void Reset()
        {
            CurrentTime = 0.0f;
        }

        public void AdvanceTime(float delta)
        {
            CurrentTime += delta;
        }

        private IEnumerable<Entry> InCurrentWindow()
        {
            return from ix in Entries where CurrentTime >= ix.EffectiveStart && CurrentTime <= ix.EffectiveEnd select ix;
        }

        public void FeedEvent(EntryTypeEnum type, uint key)
        {
            foreach (Entry e in InCurrentWindow())
            {
                if (e.Type == type && e.Key == key)
                {
                    e.Execute();
                }
            }
        }

        public IEnumerable<Entry> PeekEntries(int maxAmount, float timeForward)
        {
            return (from ix in Entries
                    where (
                     (ix.StartTime >= CurrentTime && ix.StartTime <= CurrentTime + timeForward)
                     ||
                     (ix.EndTime >= CurrentTime && ix.EndTime <= CurrentTime + timeForward)
                     ||
                     (CurrentTime >= ix.StartTime && CurrentTime <= ix.EndTime)
                    )
                    select ix).Take(maxAmount);
        }

    }

}
