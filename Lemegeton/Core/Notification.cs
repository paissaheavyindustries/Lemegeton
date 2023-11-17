using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lemegeton.Core
{

    public class Notification
    {

        public enum NotificationSeverityEnum
        {
            Critical,
            Important,
            Normal,
        }

        public Lemegeton.Action.Notification Notif { get; set; } = null;
        public string Text { get; set; } = "(undefined)";
        public State.SoundEffectEnum SoundEffect { get; set; } = State.SoundEffectEnum.None;
        public NotificationSeverityEnum Severity { get; set; } = NotificationSeverityEnum.Normal;
        public DateTime SpawnTime { get; set; } = DateTime.Now;
        public float TTL { get; set; } = 5.0f;
        public bool FirstDisplay { get; set; } = true;
        public bool TTS { get; set; } = false;
        public Context ctx { get; set; } = null;

    }

}
