using System;

namespace PotteryJournal.SharedKernel.Core
{
    /// <summary>
    /// Exception used to propagate a <see cref="HandlerResponse"/> across a call stack that cannot
    /// return the response directly, such as an API boundary.
    /// </summary>
    public class HandlerResponseException : Exception
    {
        /// <summary>
        /// Initializes a new instance wrapping the given handler response.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="response">The handler response to propagate.</param>
        public HandlerResponseException(string message, HandlerResponse response)
            : base(message)
        {
            Response = response;
        }

        /// <summary>
        /// Gets the handler response being propagated.
        /// </summary>
        public HandlerResponse Response { get; }
    }
}
