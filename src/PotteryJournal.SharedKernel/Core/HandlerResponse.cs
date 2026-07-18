using System.Collections.Generic;

namespace PotteryJournal.SharedKernel.Core
{
    /// <summary>
    /// Base response returned by infrastructure handler methods that have no return value.
    /// </summary>
    public class HandlerResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether the handler operation completed successfully.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets the accumulated error messages for this response.
        /// </summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>
        /// Gets the accumulated warning messages for this response.
        /// </summary>
        public List<string> Warnings { get; } = new List<string>();

        /// <summary>
        /// Adds an error message to this response.
        /// </summary>
        /// <param name="message">The error message to record.</param>
        public void AddError(string message)
        {
            Errors.Add(message);
        }

        /// <summary>
        /// Adds a warning message to this response.
        /// </summary>
        /// <param name="message">The warning message to record.</param>
        public void AddWarning(string message)
        {
            Warnings.Add(message);
        }

        /// <summary>
        /// Merges the errors and warnings from a child handler's response into this response.
        /// </summary>
        /// <param name="childResponse">The response produced by a delegated handler call.</param>
        public void Concat(HandlerResponse childResponse)
        {
            Errors.AddRange(childResponse.Errors);
            Warnings.AddRange(childResponse.Warnings);
        }

        /// <summary>
        /// Builds a failed response with a standard "no {kind} was found with id {id}" error.
        /// </summary>
        /// <param name="kind">A human-readable name for the entity type (e.g. "event", "piece").</param>
        /// <param name="id">The identifier that was not found.</param>
        public static HandlerResponse NotFound(string kind, object id)
        {
            HandlerResponse response = new HandlerResponse();
            response.AddError($"No {kind} was found with id {id}.");
            response.IsSuccess = false;
            return response;
        }
    }
}
