namespace Zemanat.Data.Documents;

public enum DocumentEventKind
{
    Submitted = 1,
    Issued,
    Approved,
    Rejected,
    Settled,
    Cancelled
}

/// <param name="ActorUserId">Who caused the event (the one who approved, rejected, submitted...).</param>
public sealed record DocumentEvent(DocumentEventKind Kind, int DocumentId, int ActorUserId, int CreatedByUserId, int PersonId, string PersonName, string? NumberText);

/// <summary>
/// An in-process publish/subscribe point shared by every signed-in browser tab (one singleton per site).
/// Blazor pages and the notification component subscribe so that they update without a page refresh.
/// </summary>
public sealed class DocumentEventHub
{
    public event Action<DocumentEvent>? Published;

    public void Publish(DocumentEvent e)
    {
        var handlers = Published;
        if (handlers is null) return;

        foreach (var handler in handlers.GetInvocationList().Cast<Action<DocumentEvent>>())
        {
            // One broken (already closed) browser tab must never stop the others or fail the save that already happened.
            try { handler(e); }
            catch (Exception) { }
        }
    }
}
