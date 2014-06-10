using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleCallUtils
{
    [Serializable]
    public class OracleCallBindingException : OracleCallException
    {
        public OracleCallBindingException(string message)
            : base(message)
        {
        }

        public OracleCallBindingException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
