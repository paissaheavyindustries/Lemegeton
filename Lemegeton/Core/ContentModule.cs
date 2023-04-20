using System;
using System.Collections.Generic;
using static Lemegeton.Content.UltOmegaProtocol;

namespace Lemegeton.Core
{

    public abstract class ContentModule
    {

        [Flags]
        public enum FeaturesEnum
        {
            None = 0x00,
            Automarker = 0x01,
            Drawing = 0x02,
            Sound = 0x04,
            #if !SANS_GOETIA
            Hack = 0x08,
            Automation = 0x10,
            #endif
        }

        public abstract FeaturesEnum Features { get; }

        public virtual string Author { get; } = "";

        protected State _state = null;
        private bool _enabled = true;
        private bool _active = true;
        internal bool _debugDisplayToggled = false;
        private ContentModule _owner = null;
        internal State.LogLevelEnum LogLevel { get; set; } = State.LogLevelEnum.Debug;

        internal delegate void StateChangeDelegate(bool newState);
        internal event StateChangeDelegate OnActiveChanged;
        internal event StateChangeDelegate OnEnabledChanged;

        internal List<ContentModule> Children = new List<ContentModule>();

        internal ContentModule Owner
        {
            get
            {
                return _owner;
            }
            set
            {
                if (_owner != value)
                {
                    if (_owner != null)
                    {
                        _owner.Children.Remove(this);                        
                    }
                    _owner = value;
                    if (_owner != null)
                    {                        
                        _owner.Children.Add(this);
                    }
                    Active = ((_owner == null || _owner.Active == true) && Enabled == true);
                }
            }
        }

        internal bool Active
        {
            get
            {
                return _active;
            }
            set
            {
                if (_active != value)
                {
                    _active = value;
                    OnActiveChanged?.Invoke(value);
                    foreach (ContentModule cm in Children)
                    {
                        cm.Active = (_active == true && cm.Enabled == true);
                    }
                }
            }
        }

        [AttributeOrderNumber(-1000)]
        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    OnEnabledChanged?.Invoke(value);
                    Active = ((_owner == null || _owner.Active == true) && Enabled == true);
                }
            }
        }

        public ContentModule(State st)
        {
            _state = st;
        }

        internal void Log(State.LogLevelEnum level, Exception ex, string message, params object[] args)
        {
            if (level > LogLevel)
            {
                return;
            }
            _state.Log(level, null, GetType().Name + ": " + message, args);
        }

        protected virtual bool ExecutionImplementation()
        {
            return true;
        }

        public virtual void Reset()
        {
        }

        public void Execute()
        {
            if (Active == false)
            {
                return;
            }
            bool escape = false;
            if ((Features & FeaturesEnum.Automarker) != 0)
            {
                _state.NumFeaturesAutomarker++;
                escape = (escape == true || _state.cfg.QuickToggleAutomarkers == false);
            }
            if ((Features & FeaturesEnum.Drawing) != 0)
            {
                _state.NumFeaturesDrawing++;
                escape = (escape == true || _state.cfg.QuickToggleOverlays == false);
            }
            if ((Features & FeaturesEnum.Sound) != 0)
            {
                _state.NumFeaturesSound++;
                escape = (escape == true || _state.cfg.QuickToggleSound == false);
            }
#if !SANS_GOETIA
            if ((Features & FeaturesEnum.Hack) != 0)
            {
                _state.NumFeaturesHack++;
                escape = (escape == true || _state.cfg.QuickToggleHacks == false);
            }
            if ((Features & FeaturesEnum.Automation) != 0)
            {
                _state.NumFeaturesAutomation++;
                escape = (escape == true || _state.cfg.QuickToggleAutomation == false);
            }
#endif
            if (escape == true)
            {
                return;
            }
            if (ExecutionImplementation() == true)
            {
                foreach (ContentModule cm in Children)
                {
                    cm.Execute();
                }
            }
        }

    }

}
