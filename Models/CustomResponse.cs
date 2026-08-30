namespace smp_trask1.Models
{
    public class OutputHandler
    {
        public string? responseCode { get; set; }
        public object? responseResult { get; set; }
        public string? responseMessage { get; set; }

        public static OutputHandler Success(object? result = null)
        {
            return new OutputHandler
            {
                responseCode = "200",
                responseResult = result
            };
        }

        public static OutputHandler SuccessCode()
        {
            return new OutputHandler
            {
                responseCode = "200"
            };
        }

        public static OutputHandler Failure(string message, string code = "400")
        {
            return new OutputHandler
            {
                responseCode = code,
                responseMessage = message
            };
        }

    }
}
