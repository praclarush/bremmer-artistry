using System.Threading.Tasks;
using PotteryJournal.SharedKernel.Core;

namespace PotteryJournal.Infrastructure.Handlers
{
    /// <summary>
    /// Business logic for the Contact Us form.
    /// </summary>
    public interface IContactHandler
    {
        /// <summary>
        /// Sends a Contact Us submission to the studio's configured notification address. Unlike a
        /// class booking, there's no database record backing this -- if the email can't be sent, the
        /// message is lost, so a send failure is reported as an error, not a warning.
        /// </summary>
        /// <param name="name">The sender's name.</param>
        /// <param name="email">The sender's email address.</param>
        /// <param name="message">The message body.</param>
        Task<HandlerResponse> SubmitAsync(string name, string email, string message);
    }
}
