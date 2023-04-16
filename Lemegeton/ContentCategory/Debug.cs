using Lemegeton.Content;
using Lemegeton.Core;
using System;
using System.Collections.Generic;

namespace Lemegeton.ContentCategory
{

    #if !SANS_GOETIA

    public class Debug : Core.ContentCategory
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
            items["Debugger"] = new Debugger(st);
            return items;
        }

        public Debug(State _state) : base(_state)
        {
        }

    }

    #endif

}
