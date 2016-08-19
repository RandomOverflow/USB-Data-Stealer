using System;

namespace USB_Data_Stealer.Core.Logging
{
    public class Message
    {
        public enum EventTypes
        {
            Info,
            Error
        }

        public Message(EventTypes eventType, string text)
        {
            EventType = eventType;
            Text = text;
            DateTime = DateTime.Now;
        }

        public DateTime DateTime { get; }
        public EventTypes EventType { get; }
        public string Text { get; }

        public override string ToString()
        {
            return DateTime.Now + " [" + EventType + "] " + Text;
        }
    }
}