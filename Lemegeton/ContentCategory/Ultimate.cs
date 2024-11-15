using Lemegeton.Content;
using Lemegeton.Core;
using System.Collections.Generic;

namespace Lemegeton.ContentCategory
{

    public class Ultimate : Core.ContentCategory
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
            items["UltUcob"] = new UltUcob(st);
            items["UltWeaponsRefrain"] = new UltWeaponsRefrain(st);
            items["UltAlexander"] = new UltAlexander(st);
            items["UltDragonsongReprise"] = new UltDragonsongReprise(st);
            items["UltOmegaProtocol"] = new UltOmegaProtocol(st);
            items["UltFuturesRewritten"] = new UltFuturesRewritten(st);
            return items;
        }

        public Ultimate(State st) : base(st)
        {
        }

    }

}
