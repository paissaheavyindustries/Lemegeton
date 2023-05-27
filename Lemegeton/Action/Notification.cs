using Lemegeton.Core;
using static Lemegeton.Core.State;
using System.Xml.Serialization;

namespace Lemegeton.Action
{

    public class Notification : Core.Action
    {

        [XmlAttribute]
        public Core.Notification.NotificationSeverityEnum NotificationSeverity { get; set; } = Core.Notification.NotificationSeverityEnum.Normal;
        [XmlAttribute]
        public string Text { get; set; } = "";
        [XmlAttribute]
        public SoundEffectEnum SoundEffect { get; set; } = SoundEffectEnum.None;
        [XmlAttribute]
        public float TTL { get; set; } = 5.0f;
        [XmlAttribute]
        public bool TTS { get; set; } = false;

        public override void Execute(Context ctx)
        {
            ctx.State.plug.AddNotification(new Core.Notification()
            {
                Severity = this.NotificationSeverity,
                Text = this.Text,
                SoundEffect = this.SoundEffect,
                TTL = this.TTL,
                TTS = this.TTS
            });
        }

    }

}
