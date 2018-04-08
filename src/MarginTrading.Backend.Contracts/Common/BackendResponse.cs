namespace MarginTrading.Backend.Contracts.Common
{
    public class BackendResponse<TResult>
    {
        private string _errorMessage;
        public TResult Result { get; set; }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => _errorMessage = value;
        }

        public string Message
        {
            get => _errorMessage;
            set => _errorMessage = value;
        }

        public bool IsOk => string.IsNullOrEmpty(ErrorMessage);

        public static BackendResponse<TResult> Ok(TResult result)
        {
            return new BackendResponse<TResult>
            {
                Result = result
            };
        }
        
        public static BackendResponse<TResult> Error(string message)
        {
            return new BackendResponse<TResult>
            {
                ErrorMessage = message
            };
        }
    }
}