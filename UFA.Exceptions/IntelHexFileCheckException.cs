using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UFA.Exceptions
{
    [Serializable]
    public class IntelHexFileCheckException : ApplicationException
    {
        public IntelHexFileCheckException() { }
        public IntelHexFileCheckException(string message) : base(message) { }
        public IntelHexFileCheckException(string message, Exception inner)
            : base(message, inner) { }
        public IntelHexFileCheckException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            :base (info,context) { }
    }
}
