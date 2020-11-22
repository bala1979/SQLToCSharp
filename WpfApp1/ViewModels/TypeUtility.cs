using System;
using System.Collections.Generic;

namespace WpfApp1.ViewModels
{
  static class TypeUtility
  {
    internal static Dictionary<string, string> SQLToCSharp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    internal static Dictionary<string, string> SafeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    internal static HashSet<string> NullableTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    internal static HashSet<string> ExcludeNullableCSharpTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    static TypeUtility()
    {
      SafeTypes.Add("System.Int32", "int");
      SafeTypes.Add("System.Int64", "long");
      SafeTypes.Add("System.Guid", "Guid");
      SafeTypes.Add("System.String", "string");
      SafeTypes.Add("System.Boolean", "bool");
      SafeTypes.Add("System.DateTime", "DateTime");
      SafeTypes.Add("System.Decimal", "decimal");
      SafeTypes.Add("System.Double", "double");

      SQLToCSharp.Add("bit", "bool");
      SQLToCSharp.Add("date", "DateTime");
      SQLToCSharp.Add("bigint", "long");
      SQLToCSharp.Add("datetime", "DateTime");
      SQLToCSharp.Add("decimal", "decimal");
      SQLToCSharp.Add("int", "int");
      SQLToCSharp.Add("table", "object");
      SQLToCSharp.Add("tinyint", "int");
      SQLToCSharp.Add("uniqueidentifier", "Guid");
      SQLToCSharp.Add("varbinary", "byte[]");
      SQLToCSharp.Add("varchar", "string");
      SQLToCSharp.Add("nvarchar", "string");
      SQLToCSharp.Add("xml", "string");
      SQLToCSharp.Add("sysname", "string");


      NullableTypes.Add("System.Int32");
      NullableTypes.Add("System.Guid");
      NullableTypes.Add("System.Boolean");
      NullableTypes.Add("System.DateTime");
      NullableTypes.Add("System.Double");
      NullableTypes.Add("System.Decimal");
      NullableTypes.Add("int");
      NullableTypes.Add("byte");
      NullableTypes.Add("double");
      NullableTypes.Add("long");
      NullableTypes.Add("boolean");
      NullableTypes.Add("short");
    }
  }
}
