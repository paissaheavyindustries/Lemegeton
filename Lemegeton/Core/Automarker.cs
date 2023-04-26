namespace Lemegeton.Core
{

    internal abstract class Automarker : ContentItem
    {

        public override FeaturesEnum Features
        {
            get
            {
                return _state.cfg.AutomarkerSoft == false ? FeaturesEnum.Automarker : FeaturesEnum.Drawing;
            }
        }

        [DebugOption]
        [AttributeOrderNumber(500)]
        public bool SelfMarkOnly { get; set; }


        public Automarker(State state) : base(state)
        {
        }

    }

}
