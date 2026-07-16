using System;

namespace PotteryJournal.SharedKernel.Core
{
    /// <summary>
    /// Exception used to propagate a <see cref="DataHandlerResponse{T}"/> across a call stack that
    /// cannot return the response directly, such as an API boundary.
    /// </summary>
    /// <typeparam name="T">The type of data carried by the wrapped response.</typeparam>
    public class DataHandlerResponseException<T> : Exception
    {
        /// <summary>
        /// Initializes a new instance wrapping the given data handler response.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="response">The data handler response to propagate.</param>
        public DataHandlerResponseException(string message, DataHandlerResponse<T> response)
            : base(message)
        {
            Response = response;
        }

        /// <summary>
        /// Gets the data handler response being propagated.
        /// </summary>
        public DataHandlerResponse<T> Response { get; }
    }
}
