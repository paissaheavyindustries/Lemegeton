using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using static Lemegeton.Core.State;
using static Lemegeton.Plugin;

namespace Lemegeton.Core
{

    public abstract class Content : ContentModule
    {

        public Dictionary<string, ContentItem> Items;

        public override void Reset()
        {
            foreach (var kp in Items)
            {
                kp.Value.Reset();
            }
        }

        public void LogItems()
        {
            foreach (KeyValuePair<string, ContentItem> kp in Items)
            {
                kp.Value.Log(LogLevelEnum.Info, null, "Active = {0}, Enabled = {1}", kp.Value.Active, kp.Value.Enabled);
            }
        }

        public Content(State st) : base(st)
        {
            Items = new Dictionary<string, ContentItem>();
            foreach (Type type in GetType().GetNestedTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Core.ContentItem))))
            {
                try
                {
                    Core.ContentItem ci = (Core.ContentItem)Activator.CreateInstance(type, new object[] { _state });
                    ci.Owner = this;
                    Items[type.Name] = ci;
                }
                catch (Exception ex)
                {
                    st.Log(LogLevelEnum.Error, ex, "Couldn't initialize ContentItem type {0} due to exception: {1} at {2}", type.Name, ex.Message, ex.StackTrace);
                }
            }
            foreach (Type type in GetType().BaseType.GetNestedTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Core.ContentItem))))
            {
                try
                {
                    Core.ContentItem ci = (Core.ContentItem)Activator.CreateInstance(type, new object[] { _state });
                    ci.Owner = this;
                    Items[type.Name] = ci;
                }
                catch (Exception ex)
                {
                    st.Log(LogLevelEnum.Error, ex, "Couldn't initialize ContentItem type {0} due to exception: {1} at {2}", type.Name, ex.Message, ex.StackTrace);
                }
            }
        }

    }

}
