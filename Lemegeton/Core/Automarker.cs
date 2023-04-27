namespace Lemegeton.Core
{

    internal abstract class Automarker : ContentItem
    {

        public override FeaturesEnum Features
        {
            get
            {
                return _state.cfg.AutomarkerSoft == false && AsSoftmarker == false ? FeaturesEnum.Automarker : FeaturesEnum.Drawing;
            }
        }

        [DebugOption]
        [AttributeOrderNumber(500)]
        public bool SelfMarkOnly { get; set; }

        [DebugOption]
        [AttributeOrderNumber(501)]
        public bool AsSoftmarker { get; set; }

        public Automarker(State state) : base(state)
        {
        }

    }

}
