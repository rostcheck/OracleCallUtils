using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace OracleCallUtils
{
    /// <summary>
    /// Stores a binding relationship between an Oracle column in a result's DataReader
    /// and a .NET property (in a .NET type) it is being bound to
    /// </summary>
    internal class ORMBinding
    {
        public string ColumnName { get; set; }
        public string PropertyName { get; set; }
    }

    /// <summary>
    /// Utility class that encapsulates Oracle database calls to procedures and 
    /// functions to reduce the complexity of binding
    /// </summary>
    public class OracleCall : IDisposable
    {
        private OracleConnection connection = null;
        private bool isOpen = false;
        private OracleCommand command = null;
        private string sqlCall;
        private List<ORMBinding> bindingList;
        private OracleCallType callType = OracleCallType.Query; // default
        private OracleParameter retVal;
        private const string csMissingSize = "Must specify size for non-input OracleCall parameters";
        private const string csMissingValue = "Must specify value for input or input/output OracleCall parameters";
        private const string csParameterAlreadySet = "Parameter name {0} is already set";
        private const string csNoBindingsSet = "No output bindings are set (call AddBinding)";
        private const string csCannotBindColumn = "Cannot bind column name {0} - not in DB results";
        private const string csWrongBindingType = "Binding on {0} has wrong type: {1}";
        private const string csNoPropertyOnNETType = "Property {0} does not exist on the .NET type bound to the call";
        private const string csReturnValueNotFirst = "The return value must be added before any parameters";
        private const string csNotFunction = "OracleCall must be created with call type Function to call ExecuteFunction";
        private const string csImproperFunctionCall = "Functions must be called with ExecuteFunction";
        private const string csPassAsDateTime = "Dates and time parameters must be passed as DateTime";

        /// <summary>
        /// If true (default), calling Execute<typeparamref name="T"/> will find properties on T
        /// named similarly to the DB results (ignoring case and _) and fill them
        /// </summary>
        public bool AutoBind { get; set; }

        public OracleCall(OracleCallType callType, string sqlCall)
        {
            AutoBind = true;
            bindingList = new List<ORMBinding>();
            this.sqlCall = sqlCall;
            this.callType = callType;
        }

        public void Connect(string connectionString)
        {
            connection = new OracleConnection(connectionString);
            connection.Open();
            isOpen = true;
            command = new OracleCommand(sqlCall, connection);
            command.CommandType = GetCommandType(callType);
            command.BindByName = true; // always bind parameters by name
        }

        public static string FormConnectionString(string userName, string password, string dataSource)
        {
            return string.Format("User Id={0};Password={1};Data Source={2}",
                userName, password, dataSource);
        }

        /// <summary>
        /// Add a parameter for an Oracle stored procedure (or function) call
        /// </summary>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="type">Type (OracleDbType)</param>
        /// <param name="direction">ParameterDirection (input, output, or both)</param>
        /// <param name="value">Parameter value (optional) for input/both parameters</param>
        /// <param name="size">Parameter size (optional, usually not needed)</param>
        public void AddParameter(string parameterName, OracleDbType type,  ParameterDirection direction, object value = null, int size = 0)
        {
            if (direction != ParameterDirection.Input && type != OracleDbType.RefCursor && size == 0)
                throw new OracleCallException(csMissingSize);

            if ((direction == ParameterDirection.Input || direction == ParameterDirection.InputOutput) && value == null)
                throw new OracleCallException(csMissingValue);

            if (command.Parameters.Contains(parameterName))
                throw new OracleCallException(string.Format(csParameterAlreadySet, parameterName));

            OracleParameter parameter = new OracleParameter();
            parameter.ParameterName = parameterName;
            parameter.OracleDbType = type;
            if (value != null)
                parameter.Value = SetDBValue(value, type);
            if (size != 0)
                parameter.Size = size;
            parameter.Direction = direction;
            command.Parameters.Add(parameter); 
        }

        public void AddReturnValue(OracleDbType type, int size = 0)
        {
            if (command.Parameters.Count > 0)
                throw new OracleCallException(csReturnValueNotFirst);

            retVal = new OracleParameter();
            retVal.ParameterName = "retVal";
            retVal.OracleDbType = type;
            if (size != 0)
                retVal.Size = size;
            retVal.Direction = ParameterDirection.ReturnValue;
            command.Parameters.Add(retVal);
        }

        /// <summary>
        /// Add a binding between the DB results and the .NET types returned by Execute<typeparamref name="T"/>
        /// </summary>
        /// <param name="columnName">Column name in Oracle results</param>
        /// <param name="dotNetPropertyName">Property name in .NET type to set</param>
        public void AddBinding(string columnName, string dotNetPropertyName)
        {
            bindingList.Add(new ORMBinding() { ColumnName = columnName, PropertyName = dotNetPropertyName});
        }

        /// <summary>
        /// Execute a function, returning the its return value as the specified type
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <returns>The function return converted (if possible) to specified type T</returns>
        public T ExecuteFunction<T>()
        {
            if (callType != OracleCallType.Function)
                throw new OracleCallException(csNotFunction);

            command.ExecuteNonQuery();
            return (T)Convert.ChangeType(retVal.Value.ToString(), typeof(T));
        }

        /// <summary>
        /// Unbound Execute(), returns the first column of results as a string list. You should
        /// use the typed Execute<typeparamref name="T"/> instead unless the return is very simple
        /// </summary>
        /// <returns>First column of results as a string list</returns>
        public List<string> Execute()
        {
            if (!isOpen)
                throw new OracleCallNotConnectedException();

            if (callType == OracleCallType.Function)
                throw new OracleCallException(csImproperFunctionCall);

            OracleDataReader dataReader = command.ExecuteReader();
            List<string> results = new List<string>();
            while (dataReader.Read())
            {
                results.Add(dataReader.GetString(0));
            }
            dataReader.Close();
            return results;
        }

        /// <summary>
        /// Execute the call and return a typed list with their properties filled out.
        /// By default, AutoBind will find similarly-named properties on the type T to
        /// the dataset return columns (ignoring case and underscores) and fill them in.
        /// </summary>
        /// <typeparam name="T">Type for the returned list elements</typeparam>
        /// <returns>Typed list of Ts with their properties filled out from the DB call</returns>
        public List<T> Execute<T>()
        {
            if (!isOpen)
                throw new OracleCallNotConnectedException();

            if (callType == OracleCallType.Function)
                throw new OracleCallException(csImproperFunctionCall);

            if (AutoBind == false && bindingList.Count == 0)
                throw new OracleCallBindingException(csNoBindingsSet);

            OracleDataReader dataReader = command.ExecuteReader();
            List<T> results = new List<T>();

            // Insure all bound columns are available
            List<string> columnNames = new List<string>();
            for (int columnNumber = 0; columnNumber < dataReader.FieldCount; columnNumber++)
                columnNames.Add(dataReader.GetName(columnNumber));
            foreach (ORMBinding binding in bindingList)
            {
                if (!columnNames.Contains(binding.ColumnName))
                    throw new OracleCallException(string.Format(csCannotBindColumn, binding.ColumnName));
            }
            Type thisType = typeof(T);
            if (AutoBind)
                DoAutoBind(dataReader, thisType);

            while (dataReader.Read())
            {
                T thisResult = Activator.CreateInstance<T>();
                for (int columnNumber = 0; columnNumber < dataReader.FieldCount; columnNumber++)
                {
                    ORMBinding binding = bindingList.Where(s => s.ColumnName == dataReader.GetName(columnNumber)).FirstOrDefault();
                    if (!dataReader.IsDBNull(columnNumber) && binding != null)
                    {
                        try
                        {
                            PropertyInfo propertyInfo = thisType.GetProperty(binding.PropertyName);
                            Object value = Convert.ChangeType(dataReader[columnNumber], Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType);
                            propertyInfo.SetValue(thisResult, value, null);
                        }
                        catch (NullReferenceException)
                        {
                            throw new OracleCallBindingException(string.Format(csNoPropertyOnNETType, binding.PropertyName));
                        }
                        catch (ArgumentException e)
                        {
                            throw new OracleCallBindingException(string.Format(csWrongBindingType,
                                binding.ColumnName, e.Message), e);
                        }
                        catch (FormatException e)
                        {
                            throw new OracleCallBindingException(string.Format(csWrongBindingType,
                                binding.ColumnName, e.Message), e);
                        }
                    }
                }
                results.Add(thisResult);
            }
            dataReader.Close();
            return results;
        }

        ~OracleCall()
        {
            Dispose(false);
        }

        // Create bindings for any column names that are the same (ignoring case and underscores) 
        // as property names in the target type
        private void DoAutoBind(OracleDataReader dataReader, Type thisType)
        {
            // Standardize names to be lower-case, without underscores, for matching, 
            // but remember actual property names
            Dictionary<string, string> propertyNames = new Dictionary<string,string>();
            foreach (PropertyInfo propertyInfo in thisType.GetProperties())
                propertyNames.Add(Standardize(propertyInfo.Name), propertyInfo.Name);

            for (int columnNumber = 0; columnNumber < dataReader.FieldCount; columnNumber++)
            {
                string columnName = dataReader.GetName(columnNumber);
                if (bindingList.Where(s => s.ColumnName == columnName).FirstOrDefault() != null)
                    continue; // Explicitly bound already, use the explicit binding
                else
                {
                    string standardName = Standardize(columnName);
                    if (propertyNames.ContainsKey(standardName))
                        bindingList.Add(new ORMBinding() { ColumnName = columnName, PropertyName = propertyNames[standardName]});
                }
            }
        }

        private string Standardize(string fieldName)
        {
            return fieldName.ToLower().Replace("_", "");
        }

        private CommandType GetCommandType(OracleCallType callType)
        {
            switch (callType)
            {
                case OracleCallType.Function:
                case OracleCallType.Procedure:
                    return System.Data.CommandType.StoredProcedure;
                case OracleCallType.Query:
                case OracleCallType.Command:
                default:
                    return CommandType.Text;
            }
        }

        private object SetDBValue(object value, OracleDbType type)
        {
            //TODO: Other more unusual types may require special handling here
            switch (type)
            { 
                case OracleDbType.Date:
                    if (value.GetType() != typeof(DateTime))
                        throw new OracleCallException(csPassAsDateTime);
                    return value;
                default:
                    return value.ToString();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (command != null)
                    command.Dispose();
                if (connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
                if (retVal != null)
                    retVal.Dispose();
            }
        }
    }
}
