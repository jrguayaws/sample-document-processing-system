using System;

namespace DocumentProcessor.WebForms.Models
{
    public enum NotificationLevel
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Danger = 3
    }

    /// <summary>
    /// A message shown above the uploader. Serializable so the page can park the list in
    /// ViewState across postbacks.
    /// </summary>
    [Serializable]
    public class Notification
    {
        public Notification()
        {
        }

        public Notification(string text, NotificationLevel level)
        {
            Text = text;
            Level = level;
        }

        public string Text { get; set; }

        public NotificationLevel Level { get; set; }

        /// <summary>Bootstrap contextual suffix, e.g. "success" for "notice-success".</summary>
        public string CssSuffix
        {
            get { return Level.ToString().ToLowerInvariant(); }
        }

        public string IconClass
        {
            get
            {
                switch (Level)
                {
                    case NotificationLevel.Success:
                        return "bi-check-lg";
                    case NotificationLevel.Danger:
                        return "bi-exclamation-octagon";
                    case NotificationLevel.Warning:
                        return "bi-exclamation-triangle";
                    default:
                        return "bi-info-circle";
                }
            }
        }
    }
}
