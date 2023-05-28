using Lemegeton.Content;
using Lemegeton.Core;
using System.Collections.Generic;

namespace Lemegeton.ContentCategory
{

    public class Miscellaneous : Core.ContentCategory
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public override ContentCategoryTypeEnum ContentCategoryType => ContentCategoryTypeEnum.Other;

        protected override Dictionary<string, Core.ContentCategory> InitializeSubcategories(State st)
        {
            Dictionary<string, Core.ContentCategory> items = new Dictionary<string, Core.ContentCategory>();
            return items;
        }

        protected override Dictionary<string, Core.Content> InitializeContentItems(State st)
        {
            Dictionary<string, Core.Content> items = new Dictionary<string, Core.Content>();
            items["Radar"] = new Radar(st);
            items["Overlays"] = new Overlays(st);
            items["VisualEnhancement"] = new VisualEnhancement(st);
#if !SANS_GOETIA
            items["Hack"] = new Hack(st);
            items["Automation"] = new Automation(st);
#endif
            return items;
        }

        public Miscellaneous(State _state) : base(_state)
        {
        }

    }

}
