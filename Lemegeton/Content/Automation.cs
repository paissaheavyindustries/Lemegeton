using FFXIVClientStructs.FFXIV.Client.Game;
using Lemegeton.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lemegeton.Content
{

#if !SANS_GOETIA

    public class Automation : Core.Content
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public class FishForever : Core.ContentItem
        {

            public override FeaturesEnum Features => FeaturesEnum.Automation;

            private enum FishingStateEnum
            {
                Idle,
                Fishing,
                Hooking
            }

            [AttributeOrderNumber(1000)]
            public FoodSelector Food { get; set; }

            [AttributeOrderNumber(2000)]
            public bool IgnoreLight { get; set; } = false;
            [AttributeOrderNumber(2001)]
            public bool IgnoreMedium { get; set; } = false;
            [AttributeOrderNumber(2002)]
            public bool IgnoreHeavy { get; set; } = false;

            [AttributeOrderNumber(3000)]
            public bool UsePatience2 { get; set; } = true;
            [AttributeOrderNumber(3001)]
            public bool UseMooch { get; set; } = true;
            [AttributeOrderNumber(3002)]
            public bool UseMooch2 { get; set; } = true;
            [AttributeOrderNumber(3003)]
            public bool UseThaliakFavor { get; set; } = true;

            [AttributeOrderNumber(4000)]
            public bool ReleaseEverything { get; set; } = false;

            private bool _listening = false;
            private bool _patience2Active = false;
            private int _anglerStacks = 0;
            private DateTime _reeval = DateTime.MinValue;

            private DateTime _stateChanged = DateTime.MinValue;
            private FishingStateEnum _FishingState = FishingStateEnum.Idle;
            private FishingStateEnum FishingState
            {
                get
                {
                    return _FishingState;
                }
                set
                {
                    if (_FishingState != value)
                    {
                        _FishingState = value;
                        _stateChanged = DateTime.Now;
                        Log(Core.State.LogLevelEnum.Debug, null, "State changed to {0}", _FishingState);                        
                    }
                }
            }

            private int GetRandomDelay(int upTo)
            {
                Random r = new Random();
                return r.Next(upTo);
            }

            private bool CanRelease()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    return (am->GetActionStatus((ActionType)1, 300) == 0);
                }
            }

            private bool CanCast()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    return (am->GetActionStatus((ActionType)1, 289) == 0);
                }
            }

            private bool CanMooch()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    return (am->GetActionStatus((ActionType)1, 297) == 0);
                }
            }

            private bool CanMooch2()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    return (am->GetActionStatus((ActionType)1, 268) == 0 && _state.cs.LocalPlayer.CurrentGp >= 100);
                }
            }

            private void Cast()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    if (UseMooch == true && CanMooch() == true)
                    {
                        Mooch();
                        return;
                    }
                    if (UseMooch2 == true && CanMooch2() == true)
                    {
                        Mooch2();
                        return;
                    }
                    Log(Core.State.LogLevelEnum.Debug, null, "Casting");
                    if (am->UseAction((ActionType)1, 289) == true)
                    {
                        Log(Core.State.LogLevelEnum.Debug, null, "Casted");
                        FishingState = FishingStateEnum.Fishing;
                    }
                }
            }

            private void Mooch()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    Log(Core.State.LogLevelEnum.Debug, null, "Casting (Mooch)");
                    if (am->UseAction((ActionType)1, 297) == true)
                    {
                        Log(Core.State.LogLevelEnum.Debug, null, "Casted (Mooch)");
                        FishingState = FishingStateEnum.Fishing;
                    }
                }
            }

            private void Release()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    Log(Core.State.LogLevelEnum.Debug, null, "Releasing");
                    if (am->UseAction((ActionType)1, 300) == true)
                    {
                        Log(Core.State.LogLevelEnum.Debug, null, "Released");                        
                    }
                }
            }

            private void Mooch2()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    Log(Core.State.LogLevelEnum.Debug, null, "Casting (Mooch 2)");
                    if (am->UseAction((ActionType)1, 268) == true)
                    {
                        Log(Core.State.LogLevelEnum.Debug, null, "Casted (Mooch 2)");
                        FishingState = FishingStateEnum.Fishing;
                    }
                }
            }

            private void Patience2()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    Log(Core.State.LogLevelEnum.Debug, null, "Using Patience 2");
                    if (am->UseAction((ActionType)1, 4106) == true)
                    {
                        Log(Core.State.LogLevelEnum.Debug, null, "Used Patience 2");                        
                    }
                }
            }

            private void ThaliakFavor()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    Log(Core.State.LogLevelEnum.Debug, null, "Using Thaliak's Favor");
                    if (am->UseAction((ActionType)1, 26804) == true)
                    {
                        Log(Core.State.LogLevelEnum.Debug, null, "Used Thaliak's Favor");
                        FishingState = FishingStateEnum.Fishing;
                    }
                }
            }

            private void Hook()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    Log(Core.State.LogLevelEnum.Debug, null, "Hooking");
                    Task t = new Task(() =>
                    {
                        Thread.Sleep(500 + GetRandomDelay(500));
                        if (am->UseAction((ActionType)1, 296) == true)
                        {
                            Log(Core.State.LogLevelEnum.Debug, null, "Hooked");
                            FishingState = FishingStateEnum.Hooking;
                        }
                    });
                    t.Start();
                }
            }

            private void PrecisionHookset()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    Log(Core.State.LogLevelEnum.Debug, null, "Hooking (Precision)");
                    Task t = new Task(() =>
                    {
                        Thread.Sleep(500 + GetRandomDelay(500));
                        if (am->UseAction((ActionType)1, 4179) == true)
                        {
                            Log(Core.State.LogLevelEnum.Debug, null, "Hooked (Precision)");
                            FishingState = FishingStateEnum.Hooking;
                        }
                    });
                    t.Start();
                }
            }

            private void PowerfulHookset()
            {
                unsafe
                {
                    ActionManager* am = ActionManager.Instance();
                    Log(Core.State.LogLevelEnum.Debug, null, "Hooking (Powerful)");
                    Task t = new Task(() =>
                    {
                        Thread.Sleep(500 + GetRandomDelay(500));
                        if (am->UseAction((ActionType)1, 4103) == true)
                        {
                            Log(Core.State.LogLevelEnum.Debug, null, "Hooked (Powerful)");
                            FishingState = FishingStateEnum.Hooking;
                        }
                    });
                    t.Start();
                }
            }

            protected override bool ExecutionImplementation()
            {
                if (DateTime.Now < _reeval || _state.cs.LocalPlayer.ClassJob.Id != 18)
                {
                    return false;
                }
                switch (FishingState)
                {
                    case FishingStateEnum.Idle:
                        if (_listening == false)
                        {
                            _state.OnEventPlay += _state_OnEventPlay;
                            _state.OnStatusChange += _state_OnStatusChange;
                            _listening = true;
                        }
                        if (ReleaseEverything == true && CanRelease() == true && CanMooch() == false && CanMooch2() == false)
                        {
                            Release();
                            _reeval = DateTime.Now.AddMilliseconds(500 + GetRandomDelay(500));
                            return true;
                        }
                        if (CanCast() == true)
                        {
                            if (Food.Cycle() == false)
                            {
                                Log(Core.State.LogLevelEnum.Debug, null, "Used food");
                                _reeval = DateTime.Now.AddMilliseconds(3000 + GetRandomDelay(500));
                                return true;
                            }
                            if (UseThaliakFavor == true && _state.cs.LocalPlayer.CurrentGp < _state.cs.LocalPlayer.MaxGp - 200 && _anglerStacks >= 3)
                            {
                                Log(Core.State.LogLevelEnum.Debug, null, "Applying Thaliak's Favor");
                                _reeval = DateTime.Now.AddMilliseconds(500 + GetRandomDelay(500));
                                ThaliakFavor();
                                return true;
                            }
                            else if (UsePatience2 == true && _patience2Active == false)
                            {
                                if (CanMooch() == false && CanMooch2() == false)
                                {
                                    if (_state.cs.LocalPlayer.CurrentGp >= 560)
                                    {
                                        Log(Core.State.LogLevelEnum.Debug, null, "Applying Patience 2");
                                        _reeval = DateTime.Now.AddMilliseconds(2000 + GetRandomDelay(1000));
                                        Patience2();
                                        return true;
                                    }
                                    else
                                    {
                                        Log(Core.State.LogLevelEnum.Debug, null, "Not enough GP for Patience 2");
                                    }
                                }
                            }
                            _reeval = DateTime.Now.AddMilliseconds(2000);
                            Cast();
                        }
                        break;
                    case FishingStateEnum.Fishing:
                    case FishingStateEnum.Hooking:
                        if (DateTime.Now > _stateChanged.AddMinutes(2))
                        {
                            Log(Core.State.LogLevelEnum.Debug, null, "Stuck on a state since {0}, resetting", _stateChanged);
                            FishingState = FishingStateEnum.Idle;
                        }
                        break;
                }
                return true;
            }

            public FishForever(State state) : base(state)
            {
                Food = new FoodSelector();
                Food.State = state;
                Food.MinimumTime = 120.0f;
                OnEnabledChanged += FishForever_OnEnabledChanged;
                Enabled = false;
            }

            private void FishForever_OnEnabledChanged(bool newState)
            {
                if (_listening == true)
                {
                    _state.OnEventPlay -= _state_OnEventPlay;
                    _state.OnStatusChange -= _state_OnStatusChange;
                    _listening = false;
                }
                _anglerStacks = 0;
                _patience2Active = false;
                FishingState = FishingStateEnum.Idle;
            }

            private void _state_OnStatusChange(uint src, uint dest, uint statusId, bool gained, float duration, int stacks)
            {
                if (statusId == 764)
                {
                    _patience2Active = gained;
                    Log(Core.State.LogLevelEnum.Debug, null, "Patience 2: {0}", gained);
                }
                if (statusId == 2778)
                {
                    _anglerStacks = stacks;
                }
            }

            private void _state_OnEventPlay(uint actorId, uint eventId, ushort scene, uint flags, uint param1, ushort param2, byte param3, uint param4)
            {
                if (FishingState == FishingStateEnum.Fishing && scene == 5)
                {
                    if (
                        (param3 == 36 && IgnoreLight == false)
                        ||
                        (param3 == 37 && IgnoreMedium == false)
                        ||
                        (param3 == 38 && IgnoreHeavy == false)
                    )
                    {
                        if (_patience2Active == true && _state.cs.LocalPlayer.CurrentGp >= 50)
                        {
                            if (param3 == 36)
                            {
                                PrecisionHookset();
                            }
                            else
                            {
                                PowerfulHookset();
                            }
                        }
                        else
                        {
                            Hook();
                        }
                    }
                }
                if (FishingState == FishingStateEnum.Hooking && scene == 2)
                {
                    if (param2 == 2 && (param3 == 0 || param3 == 27))
                    {
                        Log(Core.State.LogLevelEnum.Debug, null, "Recasting");
                        Task t = new Task(() =>
                        {
                            Thread.Sleep(2000 + GetRandomDelay(500));
                            FishingState = FishingStateEnum.Idle;
                        });
                        t.Start();
                    }
                }
            }

        }

        public Automation(State st) : base(st)
        {
            Enabled = false;
        }

    }

#endif

}
