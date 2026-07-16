namespace PotteryJournal.Infrastructure.Models
{
    /// <summary>
    /// A single note entry on a piece, as read or written through <see cref="Handlers.IPieceHandler"/>.
    /// </summary>
    public class PieceNoteModel
    {
        public string? Title { get; set; }

        public string NoteText { get; set; } = string.Empty;
    }
}
