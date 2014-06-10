using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleCallUtils
{
    public enum OracleCallType
    {
        Query, // unstructured SQL, returns data
        Command, // unstructured SQL, does not return data
        Procedure, // defined procedure, may return data
        Function // defined function, may return data
    }
}
