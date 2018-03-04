namespace MarginTrading.Backend.Contracts.Common
{
    public class BackendResponse<TResult>
    {
        public TResult Result { get; set; }
        
        public string ErrorMessage { get; set; }

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