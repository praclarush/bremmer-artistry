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

        /// <summary>
        /// Builds a failed response with a standard "no {kind} was found with id {id}" error.
        /// </summary>
        /// <param name="kind">A human-readable name for the entity type (e.g. "event", "piece").</param>
        /// <param name="id">The identifier that was not found.</param>
        public static new DataHandlerResponse<T> NotFound(string kind, object id)
        {
            DataHandlerResponse<T> response = new DataHandlerResponse<T>();
            response.AddError($"No {kind} was found with id {id}.");
            response.IsSuccess = false;
            return response;
        }
    }
}
