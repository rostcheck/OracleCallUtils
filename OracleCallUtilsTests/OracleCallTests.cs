using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using OracleCallUtils;

namespace OracleCallUtilsTests
{

    public class Employee
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int EmployeeID { get; set; }
        public uint DepartmentID { get; set; }
        public DateTime HireDate { get; set; }
    }

    [TestClass]
    public class OracleCallTests
    {
        private const string userName = "hr";
        private const string password = "hr";
        private const string dataSource = "localhost:1521/xe";

        [TestMethod]
        public void TestConnect()
        {
            string query = "select department_name from departments where department_id = 50";
            OracleCall call = new OracleCall(OracleCallType.Query, query);
            call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
        }

        [TestMethod]
        public void TestSimpleQuery()
        {
            string query = "select department_name from departments where department_id = 50";
            using (OracleCall call = new OracleCall(OracleCallType.Query, query))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                List<string> results = call.Execute();
                Assert.IsTrue(results.Count == 1);
                Assert.IsTrue(results[0] == "Shipping");
            }
        }

        [TestMethod]
        [ExpectedExceptionAttribute(typeof(OracleCallException), "Must specify value for input or input/output OracleCall parameters")]
        public void TestQueryWithParamsNotSpecified()
        {
            string query = "select department_name from departments where department_id = :department_id";
            using (OracleCall call = new OracleCall(OracleCallType.Query, query))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("department_id", OracleDbType.Decimal, ParameterDirection.Input);
                List<string> results = call.Execute();
            }
        }

        [TestMethod]
        public void TestQueryWithParams()
        {
            string query = "select department_name from departments where department_id = :department_id";
            using (OracleCall call = new OracleCall(OracleCallType.Query, query))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("department_id", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                List<string> results = call.Execute();
                Assert.IsTrue(results.Count == 1);
                Assert.IsTrue(results[0] == "Shipping");
            }
        }

        [TestMethod]
        [ExpectedExceptionAttribute(typeof(OracleCallException), "Parameter name department_id is already set")]
        public void TestProcedureAddParameterTwice()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("department_id", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                // This is a common cut and paste error type (department_id is repeated)
                call.AddParameter("department_id", OracleDbType.RefCursor, ParameterDirection.Output);
            }
        }

        // Note, if you're looking for an example, this is the easiest way to call an Oracle procedure
        // and get back a typed list. Autobinding (turned on by default) will automatically
        // find similarly-named properties on the .NET type and fill them from the DB results.
        [TestMethod]
        public void TestProcedure()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("P_DEPARTMENT_ID", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                call.AddParameter("P_RECORDSET", OracleDbType.RefCursor, ParameterDirection.Output);
                List<Employee> results = call.Execute<Employee>();
                Assert.IsTrue(results.Count >= 1);
                Assert.IsTrue(results.Where(s => s.FirstName == "Alexis").Count() > 0);
                Assert.IsTrue(results[0].HireDate > new DateTime(1970, 1, 1));
            }
        }

        [TestMethod]        
        public void TestProcedureUnbound()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("P_DEPARTMENT_ID", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                call.AddParameter("P_RECORDSET", OracleDbType.RefCursor, ParameterDirection.Output);
                List<string> results = call.Execute();
                Assert.IsTrue(results.Count >= 1);
            }
        }

        [TestMethod]
        [ExpectedExceptionAttribute(typeof(OracleCallBindingException), "No output bindings are set (call AddBinding)")]
        public void TestProcedureMissingBindings()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.AutoBind = false; // Force manual-only bindings, then don't provide any
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("P_DEPARTMENT_ID", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                call.AddParameter("P_RECORDSET", OracleDbType.RefCursor, ParameterDirection.Output);
                List<Employee> results = call.Execute<Employee>();
            }
        }

        [TestMethod]
        public void TestProcedureWithBindings()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("P_DEPARTMENT_ID", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                call.AddParameter("P_RECORDSET", OracleDbType.RefCursor, ParameterDirection.Output);
                call.AddBinding("FIRST_NAME", "FirstName");
                call.AddBinding("DEPARTMENT_ID", "DepartmentID");
                List<Employee> results = call.Execute<Employee>();
                Assert.IsTrue(results.Count >= 1);
                Assert.IsTrue(results.Where(s => s.FirstName == "Alexis").Count() > 0);
            }
        }

        [TestMethod]
        [ExpectedExceptionAttribute(typeof(OracleCallBindingException), "Binding on DEPARTMENT_ID has wrong type: Object of type 'System.Int32' cannot be converted to type 'System.UInt32'")]
        public void TestProcedureWithBadBindings()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("P_DEPARTMENT_ID", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                call.AddParameter("P_RECORDSET", OracleDbType.RefCursor, ParameterDirection.Output);
                call.AddBinding("FIRST_NAME", "FirstName");
                call.AddBinding("LAST_NAME", "DepartmentID"); // Wrong type (can't fit string into int)
                List<Employee> results = call.Execute<Employee>();
            }
        }

        [TestMethod]
        [ExpectedExceptionAttribute(typeof(OracleCallException), "Cannot bind column name MANIFEST_DESTINY - not in DB results")]
        public void TestProcedureWithNonexistentBindings()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("P_DEPARTMENT_ID", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                call.AddParameter("P_RECORDSET", OracleDbType.RefCursor, ParameterDirection.Output);
                call.AddBinding("FIRST_NAME", "FirstName");
                call.AddBinding("MANIFEST_DESTINY", "DepartmentID"); // Doesn't exist
                List<Employee> results = call.Execute<Employee>();
            }
        }

        [TestMethod]
        [ExpectedExceptionAttribute(typeof(OracleCallException), "The return value must be added before any parameters")]
        public void TestFunctionBad()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Function, "MINIMUM"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("V1", OracleDbType.Decimal, ParameterDirection.Input, 5.1);
                call.AddParameter("V2", OracleDbType.Decimal, ParameterDirection.Input, 12.2);
                call.AddReturnValue(OracleDbType.Double);
                double result = call.ExecuteFunction<double>();
                Assert.IsTrue(result == 5.1);
            }
        }

        [TestMethod]
        public void TestFunction()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Function, "MINIMUM"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddReturnValue(OracleDbType.Double);
                call.AddParameter("V1", OracleDbType.Decimal, ParameterDirection.Input, 5.1);
                call.AddParameter("V2", OracleDbType.Decimal, ParameterDirection.Input, 12.2);
                double result = call.ExecuteFunction<double>();
                Assert.IsTrue(result == 5.1);
            }
        }

        [TestMethod]
        public void TestDateFunction()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Function, "add30days"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddReturnValue(OracleDbType.Date);
                call.AddParameter("in_date", OracleDbType.Date, ParameterDirection.Input,
                    new DateTime(1969, 7, 1));
                DateTime result = call.ExecuteFunction<DateTime>();
                Assert.IsTrue(result.Year == 1969);
                Assert.IsTrue(result.Month == 7);
                Assert.IsTrue(result.Day == 31);
            }
        }
    }
}
