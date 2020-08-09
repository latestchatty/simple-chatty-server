using System;

namespace SimpleChattyServer.Data
{
    public sealed class ErrorResponse
    {
        public bool Error { get; set; } = true;
        public string Code { get; set; }
        public string Message { get; set; }
    }    
}
