using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace OracleCallUtils
{
    public class DataLayer
    {
        // In a real application, these might come from a config file and be passed in
        private const string userName = "hr";
        private const string password = "hr";
        private const string dataSource = "localhost:1521/xe";

        /// <summary>
        /// Example using automatic binding - expects the Employee object to contain parameters
        /// that match (translated) to Oracle column names in the recordset. For example,
        /// FirstName will be filled from a recordset column named "FirstName" or (actual) 
        /// "FIRST_NAME". You don't need to supply properties for all the recordset fields,
        /// if there are some you don't want, but if you supply a property, OracleCall expects
        /// to find its counterpart in the recordset.
        /// </summary>
        /// <returns>List of Employees</returns>
        public static List<Employee> GetEmployees()
        {
            using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("P_DEPARTMENT_ID", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                call.AddParameter("P_RECORDSET", OracleDbType.RefCursor, ParameterDirection.Output);
                return call.Execute<Employee>();
            }
        }

        /// <summary>
        /// This example does the same thing as above but uses an explicit binding to bind the
        /// recordset field DEPARTMENT_ID to the .NET property DepartmentIdNumber in EmployeeInfo. 
        /// In this case the autobinding can't map them because their names are too different.
        /// Note the other fields like FIRST_NAME and LAST_NAME are still autobound.
        /// </summary>
        /// <returns></returns>
        public static List<EmployeeInfo> GetEmployeesInDepartment(uint departmentID)
        {
            using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("P_DEPARTMENT_ID", OracleDbType.Decimal, ParameterDirection.Input, departmentID);
                call.AddParameter("P_RECORDSET", OracleDbType.RefCursor, ParameterDirection.Output);
                call.AddBinding("DEPARTMENT_ID", "DepartmentIdNumber");
                return call.Execute<EmployeeInfo>();
            }
        }

        /// <summary>
        /// Example of a function call. The return value must be bound before the other parameters
        /// (OracleCall will remind you if you forget)
        /// </summary>
        /// <param name="value1">First value</param>
        /// <param name="value2">Second value</param>
        /// <returns>Minimim value (per DB function)</returns>
        public static double GetMinimum(double value1, double value2)
        {
            using (OracleCall call = new OracleCall(OracleCallType.Function, "MINIMUM"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddReturnValue(OracleDbType.Double);
                call.AddParameter("V1", OracleDbType.Decimal, ParameterDirection.Input, 5.1);
                call.AddParameter("V2", OracleDbType.Decimal, ParameterDirection.Input, 12.2);
                return call.ExecuteFunction<double>();
            }
        }

        /// <summary>
        /// Example of a query that is free-form, not bound to a data class, and uses an
        /// input parameter. It is usually better practice to make a data class and bind to 
        /// it, as in the first example.
        /// </summary>
        /// <param name="departmentID">Department ID to look up</param>
        /// <returns>Department name or empty string</returns>
        public static string GetDepartmentName(uint departmentID)
        {
            string query = "select department_name from departments where department_id = :department_id";
            using (OracleCall call = new OracleCall(OracleCallType.Query, query))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("department_id", OracleDbType.Decimal, ParameterDirection.Input, departmentID);
                List<string> results = call.Execute();
                return (results.Count > 0) ? results[0] : string.Empty;
            }
        }
    }
}
