using Lemegeton.Content;
using Lemegeton.Core;
using System.Collections.Generic;

namespace Lemegeton.ContentCategory
{

#if !SANS_GOETIA

    public class DeepDungeon : Core.ContentCategory
    {

        public override FeaturesEnum Features => FeaturesEnum.None;

        public override ContentCategoryTypeEnum ContentCategoryType => ContentCategoryTypeEnum.Content;

        protected override Dictionary<string, Core.ContentCategory> InitializeSubcategories(State st)
        {
            Dictionary<string, Core.ContentCategory> items = new Dictionary<string, Core.ContentCategory>();
            return items;
        }

        protected override Dictionary<string, Core.Content> InitializeContentItems(State st)
        {
            Dictionary<string, Core.Content> items = new Dictionary<string, Core.Content>();
            items["PalaceOfTheDead"] = new PalaceOfTheDead(st);
            items["HeavenOnHigh"] = new HeavenOnHigh(st);
            items["EurekaOrthos"] = new EurekaOrthos(st);
            return items;
        }

        public DeepDungeon(State st) : base(st)
        {
            Enabled = false;
        }

    }

#endif

}
