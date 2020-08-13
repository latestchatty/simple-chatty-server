namespace SimpleChattyServer.Data.Responses
{
    public sealed class V1ErrorResponse
    {
        public string FaultCode { get; set; } = "AMFPHP_RUNTIME_ERROR";
        public string FaultString { get; set; }
        public string FaultDetail { get; set; }
    }
}
