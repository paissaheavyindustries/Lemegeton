using Lemegeton.Content;
using Lemegeton.Core;
using System.Collections.Generic;

namespace Lemegeton.ContentCategory
{

    public class Raid : Core.ContentCategory
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public override ContentCategoryTypeEnum ContentCategoryType => ContentCategoryTypeEnum.Content;

        protected override Dictionary<string, Core.ContentCategory> InitializeSubcategories(State st)
        {
            Dictionary<string, Core.ContentCategory> items = new Dictionary<string, Core.ContentCategory>();
            items["EndwalkerRaids"] = new EndwalkerRaids(st);
            return items;
        }

        protected override Dictionary<string, Core.Content> InitializeContentItems(State st)
        {
            Dictionary<string, Core.Content> items = new Dictionary<string, Core.Content>();
            return items;
        }

        public Raid(State st) : base(st)
        {
            Enabled = false;
        }

    }

}
