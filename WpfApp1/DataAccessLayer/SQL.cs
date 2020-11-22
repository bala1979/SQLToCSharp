using HQS.Common.Mapping;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp1.ViewModels;

namespace WpfApp1.DataAccessLayer
{
  public static class SQL
  {
    

    public static List<TableViewModel> GetTables(string db, string cstr)
    {
      const string sql = @"
USE [{0}]

SELECT
	S.Name AS Name,
	CAST (1 AS BIT) AS IsTable
FROM SYSOBJECTS AS S 
WHERE xtype = 'U'
UNION 
select 
	S.Name AS Name,
	CAST (0 AS BIT) AS IsTable
from sys.types AS S 
where is_user_defined = 1

			";
      using (var rdr = HQS.Data.DataReaderFactory.Create(string.Format(sql, db), cstr))
      {
        var map = Mapper<TableViewModel>.Create();
        return rdr.Select(f => f.Get(map)).ToList();
      }
    }

    public static List<TableColumnViewModel> GetTableColumns(string db, string cstr, string table, bool isUserDefined)
    {
      string sql = isUserDefined ?
          @"
USE [{0}]
SELECT DISTINCT 
  C.Name AS Name,
  CASE 
		WHEN Y.name = 'sysname' then 'nvarchar'
		ELSE y.name
	END AS DataTypeName,
	C.is_nullable AS IsNullable,
	C.column_id,
	CAST(C.precision AS INT) AS Precision,
	CAST(C.scale AS INT)AS Scale,
  CAST(C.max_length AS INT) AS MaxLength
From sys.table_types  AS T 
join sys.columns AS C
  ON c.object_id = t.type_table_object_id
join sys.types y 
	ON y.system_type_id = c.system_type_id
WHERE t.is_user_defined = 1
  AND t.is_table_type = 1
	and t.name = '{1}'
order by C.column_id asc"
:
@"
USE [{0}]

 SELECT  
   C.COLUMN_NAME AS Name,
	C.DATA_TYPE AS DataTypeName,
  CAST ( (CASE WHEN c.IS_NULLABLE = 'Yes' THEN 1 ELSE 0 END)AS BIT) AS IsNullable,
  CAST(C.NUMERIC_SCALE  AS INT) AS Scale,
  CAST(C.NUMERIC_PRECISION  AS INT) AS Precision,
  C.CHARACTER_OCTET_LENGTH AS MaxLength,
  CAST ( (CASE WHEN KU.COLUMN_NAME IS NULL THEN 0 ELSE 1 END)AS BIT) AS IsPrimaryKey,
  CAST ( (CASE WHEN S.Name IS NULL THEN 0 ELSE 1 END)AS BIT) AS IsIdentity,
  C.ORDINAL_POSITION AS Position
FROM INFORMATION_SCHEMA.COLUMNS AS C 
LEFT OUTER JOIN SYS.IDENTITY_COLUMNS  AS S 
 ON S.name = C.COLUMN_NAME 
 AND OBJECT_NAME(S.object_id) = C.TABLE_NAME
LEFT OUTER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC 
  ON TC.TABLE_NAME = C.TABLE_NAME 
  AND TC.CONSTRAINT_TYPE = 'PRIMARY KEY'
LEFT OUTER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
  ON KU.CONSTRAINT_NAME = TC.CONSTRAINT_NAME
  AND KU.COLUMN_NAME = C.COLUMN_NAME
WHERE C.TABLE_NAME = '{1}'
";
      using (var rdr = HQS.Data.DataReaderFactory.Create(string.Format(sql, db, table), cstr))
      {
        var map = Mapper<TableColumnViewModel>.Create();
        var query = rdr.Select(f => f.Get(map)).ToList();
        return query;
      }
    }

    public static List<string> GetDatabases(string cstr)
    {
      const string sql = @"
SELECT Name 
FROM sys.databases 
where name not in ('tempdb', 'msdb', 'master')";

      using (var rdr = HQS.Data.DataReaderFactory.Create(sql, cstr))
      {
        return rdr.Select(f => f.GetString("Name")).ToList();
      }
    }

    public static List<ViewModels.StoredProcedureViewModel> GetStoredProcedures(string db, string cstr)
    {
      const string sql = @"
USE [{0}]
SELECT  
   ROUTINE_SCHEMA AS [Owner],
   ROUTINE_NAME AS Name 
 FROM  information_schema.routines 
 WHERE routine_type = 'PROCEDURE'";

      using (var rdr = HQS.Data.DataReaderFactory.Create(string.Format(sql, db), cstr))
      {
        var map = Mapper<StoredProcedureViewModel>.Create();
        return rdr.Select(f => f.Get(map)).ToList();
      }
    }


    public static List<StoredProcedureParameterViewModel> GetStoredProcedureParamters(string db, string name, string schema, string cstr)
    {
      const string sql = @"
 USE [{0}]
 SELECT  
   Name as Name,
   type_name(user_type_id) AS Type,
   max_length AS Length,
   CASE 
	  WHEN TYPE_NAME(system_type_id) = 'uniqueidentifier' THEN PRECISION  
      ELSE ODBCPREC(system_type_id, max_length, precision) 
   END AS Precision,
   OdbcScale(system_type_id, scale) AS Scale,
    parameter_id AS [Order]
 FROM SYS.PARAMETERS WHERE OBJECT_ID = OBJECT_ID('{1}.{2}')

";

      using (var rdr = HQS.Data.DataReaderFactory.Create(string.Format(sql, db, schema, name), cstr))
      {
        var map = Mapper<StoredProcedureParameterViewModel>.Create();
        return rdr.Select(f => f.Get(map)).ToList();
      }
    }

    public static List<StoredProcedureColumnViewModel> GetStoredProcedureColumns(string db, string name, string schema, string cstr, List<StoredProcedureParameterViewModel> parameters)
    {
      List<StoredProcedureColumnViewModel> columns = new List<StoredProcedureColumnViewModel>();
      System.Text.StringBuilder sb = new StringBuilder();
      foreach (var param in parameters)
      {
        sb.AppendFormat("{0}={1},", param.Name, param.PassNull ? "NULL" : "'" + param.RunTimeValue + "'");
      }

      if (sb.Length > 0)
      {
        sb.Length--;
      }

      sb.Insert(0, $"SET ROWCOUNT 1 EXEC {db}.{schema}.{name} ");

      using (var con = new SqlConnection(ConfigurationManager.ConnectionStrings[cstr].ConnectionString))
      {
        var cmd = con.CreateCommand();
        cmd.CommandText = sb.ToString();
        cmd.CommandType = System.Data.CommandType.Text;
        cmd.Connection.Open();
        var rdr = cmd.ExecuteReader();
        for (int i = 0; i < rdr.FieldCount; i++)
        {

          columns.Add(new StoredProcedureColumnViewModel
          {
            Name = rdr.GetName(i),
            SpecificFieldType = rdr.GetProviderSpecificFieldType(i).ToString(),
            DataTypeName = rdr.GetDataTypeName(i),
            Type = rdr.GetFieldType(i).ToString(),
            IsNullable = false
          });
        }
      }

      return columns;
    }
  }
}
