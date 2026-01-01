namespace AssistenteExecutivo.Domain.Enums;

/// <summary>
/// Defines the types of actions available in workflow automation.
/// </summary>
public enum ActionType
{
    /// <summary>
    /// Creates a document from a template.
    /// </summary>
    CreateDocument = 1,

    /// <summary>
    /// Sends an email via connected provider (Gmail/Outlook/Mailjet).
    /// </summary>
    SendEmail = 2,

    /// <summary>
    /// Opens WhatsApp with a pre-filled message.
    /// </summary>
    SendWhatsApp = 3,

    /// <summary>
    /// Sends a Calendly-style scheduling link.
    /// </summary>
    ScheduleMeeting = 4,

    /// <summary>
    /// Creates a reminder in the system.
    /// </summary>
    CreateReminder = 5,

    /// <summary>
    /// Updates a contact's information or adds a tag.
    /// </summary>
    UpdateContact = 6,

    /// <summary>
    /// Creates a note attached to a contact.
    /// </summary>
    CreateNote = 7,

    /// <summary>
    /// Makes an HTTP request to an allowed endpoint.
    /// </summary>
    HttpRequest = 8,

    /// <summary>
    /// Waits for a specified duration before continuing.
    /// </summary>
    Wait = 9,

    /// <summary>
    /// Sets a workflow variable for use in subsequent steps.
    /// </summary>
    SetVariable = 10
}
