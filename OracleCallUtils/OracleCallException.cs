using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleCallUtils
{
    [Serializable]
    public class OracleCallException : Exception
    {
        public OracleCallException()
            : base()
        {
        }

        public OracleCallException(string message)
            : base(message)
        {
        }

        public OracleCallException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
