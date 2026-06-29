using System;
using System.Collections.Generic;
using System.Text;

namespace Osmos.Core.Helpers
{
    public class HttpExceptions
    {
        public class BadRequestException : Exception
        {
            public BadRequestException(string message) : base(message)
            {

            }
        }

        public class NotFoundException : Exception
        {
            public NotFoundException()
            {

            }
        }
    }
}
