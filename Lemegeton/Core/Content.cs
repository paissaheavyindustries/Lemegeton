using System;
using System.Collections.Generic;
using System.Linq;

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

        public Content(State st) : base(st)
        {
            Items = new Dictionary<string, ContentItem>();
            foreach (Type type in GetType().GetNestedTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Core.ContentItem))))
            {
                Core.ContentItem ci = (Core.ContentItem)Activator.CreateInstance(type, new object[] { _state });
                ci.Owner = this;
                Items[type.Name] = ci;
            }
            foreach (Type type in GetType().BaseType.GetNestedTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Core.ContentItem))))
            {
                Core.ContentItem ci = (Core.ContentItem)Activator.CreateInstance(type, new object[] { _state });
                ci.Owner = this;
                Items[type.Name] = ci;
            }
        }

    }

}
