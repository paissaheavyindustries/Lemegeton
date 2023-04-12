using System.Collections.Generic;

namespace Lemegeton.Core
{

    public abstract class ContentCategory : ContentModule
    {

        public enum ContentCategoryTypeEnum
        {
            Content,
            Other,
            Subcategory
        }

        public Dictionary<string, ContentCategory> Subcategories;
        public Dictionary<string, Content> ContentItems;

        public abstract ContentCategoryTypeEnum ContentCategoryType { get; }

        protected abstract Dictionary<string, ContentCategory> InitializeSubcategories(State st);
        protected abstract Dictionary<string, Content> InitializeContentItems(State st);

        public ContentCategory(State st) : base(st)
        {
            Subcategories = InitializeSubcategories(st);
            foreach (var x in Subcategories)
            {
                x.Value.Owner = this;
            }
            ContentItems = InitializeContentItems(st);
            foreach (var x in ContentItems)
            {
                x.Value.Owner = this;
            }
        }

    }

}
