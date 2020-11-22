using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using WpfApp1.DataAccessLayer;

namespace WpfApp1.ViewModels
{
  public class TableGeneratorViewModel : BaseViewModel
  {

    internal void GenerateCode()
    {
      try
      {
        if (this.Columns.Count > 0)
        {
          BuildClass();
          if (this.IsUDT)
          {
            this.BuildUDT();
          }
          else
          {
            this.BuildAddSproc();
            this.BuildUpdateSproc();
            this.BuildDeleteSproc();
            this.BuildGetSproc();
          }

          this.TriggerPropertyChanged(() => this.Columns);
        }
        else
        {
          MessageBox.Show("There is nothing to generate");
        }
      }
      catch (Exception e)
      {
        MessageBox.Show(e.Message);
      }
    }

    public TableGeneratorViewModel()
    {
      this.Columns = new ObservableCollection<ViewModels.TableColumnViewModel>();
      this.Tables = new ObservableCollection<ViewModels.TableViewModel>();
      this.Databases = new ObservableCollection<string>();
      this.ConnectionString = "All";
      var db = System.Configuration.ConfigurationManager.AppSettings["DefaultDatabase"]; ;
      if (string.IsNullOrWhiteSpace(db) == false)
      {
        this.Database = db;
      }
    }

    private ViewModels.TableViewModel _table;

    private void BuildClass()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append($"public class {this.Table.Name}").AppendLine();
      sb.Append("{").AppendLine();
      foreach (var entry in this.Columns)
      {
        string cShapeType = entry.DataTypeName;
        if (TypeUtility.SQLToCSharp.TryGetValue(entry.DataTypeName, out cShapeType))
        {
          string friendlyType = cShapeType;
          if (entry.IsNullable && TypeUtility.NullableTypes.Contains(cShapeType))
          {
            friendlyType += "?";
          }

          sb.Append($"\tpublic {friendlyType} {entry.Name} {{get; set;}}").AppendLine();
        }
        else
        {
          if (entry.DataTypeName == "char" && entry.MaxLength > 1)
          {
            sb.Append($"\tpublic {entry.DataTypeName}[] {entry.Name} {{get; set;}}").AppendLine();
          }
          else
          {
            sb.Append($"\tpublic {entry.DataTypeName} {entry.Name} {{get; set;}}").AppendLine();
          }
        }
      }

      sb.Append("}").AppendLine();
      this.POCOClass = sb.ToString();
      sb.Append("}").AppendLine();
    }



    private void BuildAddSproc()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append($"CREATE PROCEDURE [dbo].[{ this.Table.Name}_Add]").AppendLine();
      sb.AppendLine("(");
      foreach (var column in this.Columns)
      {
        sb.Append("\t");
        this.CreateParameter(column, sb);
        if (column.IsIdentity)
        {
          sb.Append(" OUTPUT");
        }

        if (this.Columns.Last() != column)
        {
          sb.AppendLine(",");
        }
        else
        {
          sb.AppendLine();
        }
      }

      sb.AppendLine(")");
      sb.AppendLine();
      sb.AppendLine("AS");
      sb.AppendLine("SET NOCOUNT ON");
      sb.AppendLine("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED");
      sb.AppendLine();
      sb.AppendLine($"INSERT dbo.{this.Table.Name}");
      sb.AppendLine("(");
      var columns = this.Columns.Where(f => f.IsIdentity == false);
      foreach (var column in columns)
      {
        sb.Append("\t");
        sb.Append($"[{column.Name}]");
        if (column != columns.Last())
        {
          sb.AppendLine(",");
        }
        else
        {
          sb.AppendLine();
        }
      }
      sb.Append(")").AppendLine();
      sb.AppendLine("VALUES");
      sb.AppendLine("(");
      foreach (var column in columns)
      {
        sb.Append("\t");
        sb.Append("@" + column.Name);
        if (column != columns.Last())
        {
          sb.AppendLine(",");
        }
        else
        {
          sb.AppendLine();
        }
      }
      sb.Append(")");
      sb.AppendLine();


      var id = this.Columns.FirstOrDefault(f => f.IsIdentity);
      if (id != null)
      {
        sb.AppendLine($"SELECT @{id.Name} = SCOPE_IDENTITY()");
      }

      sb.AppendLine();
      sb.AppendLine("RETURN");
      sb.AppendLine();
      this.AddText = sb.ToString();
    }


    private void BuildGetSproc()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append($"CREATE PROCEDURE [dbo].[{ this.Table.Name}_Get]").AppendLine();
      sb.AppendLine("(");
      var columns = this.Columns.Where(f => f.IsPrimaryKey);
      foreach (var column in columns)
      {
        sb.Append("\t");
        this.CreateParameter(column, sb);
        if (columns.Last() != column)
        {
          sb.AppendLine(",");
        }
        else
        {
          sb.AppendLine();
        }
      }

      sb.AppendLine(")");
      sb.AppendLine();
      sb.AppendLine("AS");
      sb.AppendLine("SET NOCOUNT ON");
      sb.AppendLine("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED");
      sb.AppendLine();
      sb.AppendLine($"SELECT");
      foreach (var column in this.Columns)
      {
        sb.Append("\t");
        sb.Append($"[{column.Name}]");
        if (column != this.Columns.Last())
        {
          sb.AppendLine(",");
        }
        else
        {
          sb.AppendLine();
        }
      }

      var keys = this.Columns.Where(F => F.IsPrimaryKey).ToList();
      if (keys.Count >= 1)
      {
        foreach (var key in keys)
        {
          if (key == keys.First())
          {
            sb.AppendLine($"WHERE {key.Name} = @{key.Name}");
          }
          else
          {
            sb.AppendLine($"AND  {key.Name} = @{key.Name}");
          }
        }
      }

      sb.AppendLine();
      sb.AppendLine("RETURN");
      sb.AppendLine();
      this.GetText = sb.ToString();
    }

    private void BuildDeleteSproc()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append($"CREATE PROCEDURE [dbo].[{ this.Table.Name}_Delete]").AppendLine();
      sb.AppendLine("(");
      var columns = this.Columns.Where(f => f.IsPrimaryKey);

      foreach (var column in columns)
      {
        sb.Append("\t");
        this.CreateParameter(column, sb);
        if (columns.Last() != column)
        {
          sb.AppendLine(",");
        }
        else
        {
          sb.AppendLine();
        }
      }

      sb.AppendLine(")");
      sb.AppendLine();
      sb.AppendLine("AS");
      sb.AppendLine("SET NOCOUNT ON");
      sb.AppendLine("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED");
      sb.AppendLine();
      sb.AppendLine($"DELETE dbo.{this.Table.Name}");
      var keys = this.Columns.Where(F => F.IsPrimaryKey).ToList();
      if (keys.Count >= 1)
      {
        foreach (var key in keys)
        {
          if (key == keys.First())
          {
            sb.AppendLine($"WHERE {key.Name} = @{key.Name}");
          }
          else
          {
            sb.AppendLine($"AND  {key.Name} = @{key.Name}");
          }
        }
      }

      sb.AppendLine();
      sb.AppendLine("RETURN");
      sb.AppendLine();
      this.DeleteText = sb.ToString();
    }


    private void BuildUpdateSproc()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append($"CREATE PROCEDURE [dbo].[{ this.Table.Name}_Update]").AppendLine();
      sb.AppendLine("(");
      foreach (var column in this.Columns)
      {
        sb.Append("\t");
        this.CreateParameter(column, sb);
        if (this.Columns.Last() != column)
        {
          sb.AppendLine(",");
        }
        else
        {
          sb.AppendLine();
        }
      }

      sb.AppendLine(")");
      sb.AppendLine();
      sb.AppendLine("AS");
      sb.AppendLine("SET NOCOUNT ON");
      sb.AppendLine("SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED");
      sb.AppendLine();
      sb.AppendLine($"UPDATE dbo.{this.Table.Name}");
      var columns = this.Columns.Where(f => f.IsIdentity == false);
      sb.AppendLine("SET ");

      foreach (var column in columns)
      {
        sb.Append("\t");
        sb.Append($"[{column.Name}] = @{column.Name}");
        if (column != columns.Last())
        {
          sb.AppendLine(",");
        }
        else
        {
          sb.AppendLine();
        }
      }

      var keys = this.Columns.Where(F => F.IsPrimaryKey).ToList();
      if (keys.Count >= 1)
      {
        foreach (var key in keys)
        {
          if (key == keys.First())
          {
            sb.AppendLine($"WHERE {key.Name} = @{key.Name}");
          }
          else
          {
            sb.AppendLine($"AND  {key.Name} = @{key.Name}");
          }
        }
      }

      sb.AppendLine();
      sb.AppendLine("RETURN");
      sb.AppendLine();
      this.UpdateText = sb.ToString();
    }

    public void CreateParameter(TableColumnViewModel column, StringBuilder sb)
    {
      sb.Append("@");
      sb.Append(column.Name);
      sb.Append(" ");
      switch (column.DataTypeName)
      {
        case "varchar":
        case "nvarchar":
        case "char":
        case "decimal":
          sb.Append(column.DataTypeName.ToUpper());
          sb.Append("(");
          if (column.Precision > 0)
          {
            sb.Append(column.Precision);
            sb.Append(",");
            sb.Append(column.Scale);
          }
          else
          {
            sb.Append(column.MaxLength);
          }
          sb.Append(")");
          break;
        case "xml":
          sb.Append("VARCHAR(MAX)");
          break;
        default:
          sb.Append(column.DataTypeName);
          break;
      }
    }


    private void BuildUDT()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append($"public DataTable ToDataTable (List<{this.Table.Name}> items)").AppendLine();
      sb.Append("{").AppendLine();
      sb.Append("\tDataTable dt = new DataTable(); ").AppendLine();
      foreach (var column in this.Columns)
      {
        sb.AppendFormat("\tdt.Columns.Add(\"{0}\");", column.Name).AppendLine();
      }

      sb.Append("\tforeach (var item in items)").AppendLine();
      sb.Append("\t{").AppendLine();
      var props = string.Join(",", this.Columns.Select(f => " item." + f.Name));
      sb.Append("\t\tdt.Rows.Add(new object[] {" + props + "});").AppendLine();
      sb.Append("\t}").AppendLine();
      sb.Append("\treturn dt;").AppendLine();
      sb.Append("}").AppendLine();
      this.UDPText = sb.ToString();
    }

    public bool IsTable
    {
      get
      {
        return this.Table?.IsTable == true;
      }
    }

    public bool IsUDT
    {
      get
      {
        return this.Table?.IsTable == false;
      }
    }



    public ViewModels.TableViewModel Table
    {
      get { return _table; }
      set
      {
        if (SetIfDirty(ref _table, value))
        {
          this.Columns.Clear();
          if (this.Table != null)
          {
            try
            {
              var columns = SQL.GetTableColumns(this.Database, this.ConnectionString, this.Table.Name, this.Table.IsTable == false);
              columns.ForEach(f => this.Columns.Add(f));
              this.GenerateCode();
            }
            catch (Exception e)
            {
              MessageBox.Show(e.Message);
            }
          }
        }

        this.TriggerPropertyChanged(() => this.IsTable);
        this.TriggerPropertyChanged(() => this.IsUDT);
        this.TriggerPropertyChanged(() => this.Columns);
      }
    }

    private string _database;
    public string Database
    {
      get { return _database; }
      set
      {
        this.Tables.Clear();
        if (SetIfDirty(ref _database, value))
        {
          if (string.IsNullOrWhiteSpace(this.Database) == false)
          {

            try
            {
              var sprocs = SQL.GetTables(this.Database, _connectionString);
              if (sprocs.Count > 0)
              {
                foreach (var entry in sprocs)
                {
                  this.Tables.Add(entry);
                }
              }

            }
            catch (Exception e)
            {
              MessageBox.Show(e.Message);
            }
          }
        }
      }
    }

    private string _connectionString;
    public string ConnectionString
    {
      get { return _connectionString; }
      set
      {
        if (SetIfDirty(ref _connectionString, value))
        {
          try
          {
            this.Databases.Clear();
            var dbs = SQL.GetDatabases(_connectionString);
            if (dbs.Count > 0)
            {
              foreach (var entry in dbs)
              {
                this.Databases.Add(entry);
              }
            }
          }
          catch (Exception e)
          {
            MessageBox.Show(e.Message);
          }
        }
      }
    }

    public ObservableCollection<string> Databases { get; }
    public ObservableCollection<TableViewModel> Tables { get; }
    public ObservableCollection<TableColumnViewModel> Columns { get; }

    private string _addText;
    public string AddText
    {
      get { return _addText; }
      set { SetIfDirty(ref _addText, value); }
    }

    private string _getText;
    public string GetText
    {
      get { return _getText; }
      set { SetIfDirty(ref _getText, value); }
    }

    private string _deleteText;

    public string DeleteText
    {
      get { return _deleteText; }
      set { SetIfDirty(ref _deleteText, value); }
    }

    private string _updateText;
    public string UpdateText
    {
      get { return _updateText; }
      set { SetIfDirty(ref _updateText, value); }
    }

    private string _UDPText;
    public string UDPText
    {
      get { return _UDPText; }
      set
      {
        SetIfDirty(ref _UDPText, value);
      }
    }

    private string _pocoClass;
    public string POCOClass
    {
      get { return _pocoClass; }
      set
      {
        SetIfDirty(ref _pocoClass, value);
      }
    }
  }
}
