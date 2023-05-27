using Lemegeton.Core;
using System.Xml.Serialization;

namespace Lemegeton.Action
{

    public class ChatMessage : Core.Action
    {

        public enum ChatSeverityEnum
        {
            Normal,
            Error,
        }

        [XmlAttribute]
        public ChatSeverityEnum ChatSeverity { get; set; } = ChatSeverityEnum.Normal;
        [XmlAttribute]
        public string Text { get; set; } = "";

        public override void Execute(Context ctx)
        {
            switch (ChatSeverity)
            {
                case ChatSeverityEnum.Normal:
                    ctx.State.cg.Print(Text);
                    break;
                case ChatSeverityEnum.Error:
                    ctx.State.cg.PrintError(Text);
                    break;
            }
        }

    }

}
