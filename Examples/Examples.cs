using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace OracleCallUtils
{
    /// <summary>
    /// .NET Data class to bind our procedure results to
    /// </summary>
    public class Employee
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int EmployeeID { get; set; }
        public uint DepartmentID { get; set; }
        public DateTime HireDate { get; set; }
    }

    public class EmployeeInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public uint DepartmentIdNumber { get; set; }
    }

    /// <summary>
    /// Note: you should install the included stored procedures and functions in the database
    /// before running this example
    /// </summary>
    class Examples
    {
        static void Main(string[] args)
        {
            Console.WriteLine("OracleCall Examples");

            Console.WriteLine("GetEmployees (Automatic binding example - you usually want this:)");
            foreach (var employee in DataLayer.GetEmployees())
                Console.WriteLine(string.Format("{0} {1}", employee.FirstName, employee.LastName));

            Console.WriteLine("");
            Console.WriteLine("GetEmployeesInDepartment (Explicit binding example)");
            foreach (var employee in DataLayer.GetEmployeesInDepartment(50))
                Console.WriteLine(string.Format("{0} {1} {2}", employee.FirstName, employee.LastName, employee.DepartmentIdNumber));

            Console.WriteLine("");
            Console.WriteLine("GetMinimum (Function call example)");
            Console.WriteLine("Result: " + DataLayer.GetMinimum(2.1, 4.5));

            Console.WriteLine("");
            Console.WriteLine("GetDepartmentID (free-form query example)");
            Console.WriteLine("Departmetn 50 is: " + DataLayer.GetDepartmentName(50));

            Console.WriteLine("");
            Console.WriteLine("For more examples, see the unit tests.");
        }
    }
}
