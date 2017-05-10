namespace MarginTrading.Frontend.Models
{
    public class ResponseModel
    {
        public enum ErrorCodeType
        {
            InvalidInputField = 0,
            InconsistentData = 1,
            AssetNotFound = 2,
            NoData = 3
        }

        public class ErrorModel
        {
            public ErrorCodeType Code { get; set; }
            public string Field { get; set; }
            public string Message { get; set; }
        }

        public ErrorModel Error { get; set; }

        public static ResponseModel CreateInvalidFieldError(string field, string message)
        {
            return new ResponseModel
            {
                Error = new ErrorModel
                {
                    Code = ErrorCodeType.InvalidInputField,
                    Field = field,
                    Message = message
                }
            };
        }

        public static ResponseModel CreateFail(ErrorCodeType errorCodeType, string message)
        {
            return new ResponseModel
            {
                Error = new ErrorModel
                {
                    Code = errorCodeType,
                    Message = message
                }
            };
        }

        private static readonly ResponseModel OkInstance = new ResponseModel();

        public static ResponseModel CreateOk()
        {
            return OkInstance;
        }
    }

    public class ResponseModel<T> : ResponseModel
    {
        public T Result { get; set; }

        public static ResponseModel<T> CreateOk(T result)
        {
            return new ResponseModel<T>
            {
                Result = result
            };
        }

        public new static ResponseModel<T> CreateInvalidFieldError(string field, string message)
        {
            return new ResponseModel<T>
            {
                Error = new ErrorModel
                {
                    Code = ErrorCodeType.InvalidInputField,
                    Field = field,
                    Message = message
                }
            };
        }

        public new static ResponseModel<T> CreateFail(ErrorCodeType errorCodeType, string message)
        {
            return new ResponseModel<T>
            {
                Error = new ErrorModel
                {
                    Code = errorCodeType,
                    Message = message
                }
            };
        }
    }
}
