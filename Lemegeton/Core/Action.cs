using System;
using System.Xml.Serialization;

namespace Lemegeton.Core
{

    [XmlInclude(typeof(Lemegeton.Action.ChatMessage))]
    [XmlInclude(typeof(Lemegeton.Action.Notification))]
    [XmlInclude(typeof(Lemegeton.Action.IngameCommand))]
    public abstract class Action
    {
        
        internal Guid Id = Guid.NewGuid();

        public abstract void Execute(Context ctx);
        
        public virtual string Describe()
        {
            return I18n.Translate("Timelines/ActionTypes/" + GetType().Name);
        }

        public virtual Action Duplicate()
        {
            return (Action)MemberwiseClone();
        }

    }

}
