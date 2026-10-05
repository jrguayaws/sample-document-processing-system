namespace DocumentProcessor.Web.Models;

public enum NotificationLevel { Info, Success, Warning, Danger }

public sealed record Notification(string Text, NotificationLevel Level)
{
    /// <summary>Bootstrap contextual suffix, e.g. "alert-success".</summary>
    public string CssSuffix => Level.ToString().ToLowerInvariant();
}
