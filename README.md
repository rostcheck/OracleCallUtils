# README #

OracleCall allows you to quickly, easily, and safely call Oracle stored procedures, functions, and 
queries from .NET.

### Quick example ###

Given a .NET class Employee with appropriate properties, you can do:

           using (OracleCall call = new OracleCall(OracleCallType.Procedure, "P_EMP_RS"))
            {
                call.Connect(OracleCall.FormConnectionString(userName, password, dataSource));
                call.AddParameter("P_DEPARTMENT_ID", OracleDbType.Decimal, ParameterDirection.Input, 50M);
                call.AddParameter("P_RECORDSET", OracleDbType.RefCursor, ParameterDirection.Output);
                return call.Execute<Employee>();
            }

Easy, no? OracleCall will automatically transform the recordset into a list of filled-out Employee objects. Here it inferred the property names from the recordset field names; if they are not clear, you can manually bind those that need to be mapped.

### More info ###
* OracleCall is designed defensively to protect you from subtle issues with the Oracle interface libraries. If your bindings are wrong, missing, or duplicated via cut-and-paste error, or if you 
put a parameter in a position Oracle will not accept, OracleCall with throw a helpful exception to explain the issue to you.
* Download the solution and see the Examples project for examples of use.
* This is version 1.0.

### How do I get set up? ###

* To use OracleCall in your project:
* Add a reference to the OracleCallUtils.dll
* Install ODP.NET (make sure to get the managed version) via NuGet
* Refer to the examples in the solution for simple usage examples
* For more info, see howto.txt in the Example project
* Unit tests can be run directly from Visual Studio

### Contribution guidelines ###

* All code should be submitted with unit tests
* All code will be reviewed by the maintainer
* Other guidelines

### Who do I talk to? ###

* See the Issues and Wiki for help
* Project maintainer is David Rostcheck (davidrostcheck@gmail.com)