using System;

namespace CDTS_PROJECT.Exceptions
{
    public class HttpResponseException : Exception
    {
        public int Status { get; set; }
        public object Value { get; set; }
    }
}