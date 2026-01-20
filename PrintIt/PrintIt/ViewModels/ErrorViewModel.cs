namespace PrintIt.ViewModels
{
    /// <summary>
    /// View model used for error handling.
    /// </summary>
    public class ErrorViewModel
    {

        /// <summary>
        /// Gets or sets the id for the DTO Request.
        /// </summary>
        public string? RequestId { get; set; }

        /// <summary>
        /// Returns true or false whether the DTO Request id is null or not.
        /// </summary>

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
