namespace PotteryJournal.SharedKernel.Core
{
    /// <summary>
    /// Response returned by infrastructure handler methods that return a single object.
    /// </summary>
    /// <typeparam name="T">The type of data returned.</typeparam>
    public class DataHandlerResponse<T> : HandlerResponse
    {
        /// <summary>
        /// Gets or sets the data returned by the handler operation.
        /// </summary>
        public T? Data { get; set; }
    }
}
