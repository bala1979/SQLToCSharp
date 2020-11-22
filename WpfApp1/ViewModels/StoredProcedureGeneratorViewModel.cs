using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Sql;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WpfApp1.DataAccessLayer;

namespace WpfApp1.ViewModels
{
  public class StoredProcedureGeneratorViewModel : BaseViewModel
  {
    internal void GenerateCode()
    {
      try
      {
        if (this.Columns.Count > 0)
        {
          BuildClass();
          BuildMapper();
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

    public StoredProcedureGeneratorViewModel()
    {
      this.Parameters = new ObservableCollection<ViewModels.StoredProcedureParameterViewModel>();
      this.Procedures = new ObservableCollection<ViewModels.StoredProcedureViewModel>();
      this.Columns = new ObservableCollection<ViewModels.StoredProcedureColumnViewModel>();
      this.Databases = new ObservableCollection<string>();
      this.ConnectionString = "All";
      var db = System.Configuration.ConfigurationManager.AppSettings["DefaultDatabase"]; ;
      if (string.IsNullOrWhiteSpace(db) == false)
      {
        this.Database = db;
      }
    }

    private string _className;

    public string ClassName
    {
      get { return _className; }
      set
      {
        SetIfDirty(ref _className, value);
        if (string.IsNullOrWhiteSpace(value) == false && string.IsNullOrWhiteSpace(this.ViewModelClassName))
        {
          this.ViewModelClassName = value + "ViewModel";
        }
      }
    }

    private string _mapper;
    public string Mapper
    {
      get { return _mapper; }
      set { SetIfDirty(ref _mapper, value); }
    }


    private string _ViewModelClassName;

    public string ViewModelClassName
    {
      get { return _ViewModelClassName; }
      set { SetIfDirty(ref _ViewModelClassName, value); }
    }

    private string _ViewModelClass;

    public string ViewModelClass
    {
      get { return _ViewModelClass; }
      set { SetIfDirty(ref _ViewModelClass, value); }
    }

    private string _pocoClass;

    public string POCOClass
    {
      get { return _pocoClass; }
      set { SetIfDirty(ref _pocoClass, value); }
    }

    private ViewModels.StoredProcedureViewModel _procedure;

    internal void LoadColumns()
    {
      try
      {
        this.Columns.Clear();
        var columns = SQL.GetStoredProcedureColumns(this.Database, this.Procedure.Name, this.Procedure.Owner, this.ConnectionString, this.Parameters.ToList());
        var grp = columns.GroupBy(c => c.Name).Where(f => f.Count() > 1).ToList();
        if (grp.Count > 0)
        {
          MessageBox.Show($"Procedure {Procedure.Name} has multiple columns listed for {string.Join(",", grp.Select(f => f.Key))}");
          return;
        }

        if (columns.Count > 0)
        {
          columns.ForEach(f => this.Columns.Add(f));
          BuildClass();
          BuildMapper();
          this.TriggerPropertyChanged(() => this.Columns);
        }
      }
      catch (Exception e)
      {
        MessageBox.Show(e.Message);
      }
    }

    private void BuildClass()
    {
      StringBuilder sb = new StringBuilder();
      sb.Append($"public class {ClassName}").AppendLine();
      sb.Append("{").AppendLine();
      foreach (var entry in this.Columns)
      {
        var friendlyType = entry.Type;
        if (TypeUtility.SafeTypes.TryGetValue(entry.Type, out friendlyType) == false)
        {
          friendlyType = entry.Type;
        }

        if (entry.IsNullable && TypeUtility.NullableTypes.Contains(entry.Type))
        {
          friendlyType += "?";
        }

        sb.Append($"\tpublic {friendlyType} {entry.Name} {{get; set;}}").AppendLine();
      }

      sb.Append("}").AppendLine();

      this.POCOClass = sb.ToString();

      sb = new StringBuilder();
      sb.Append($"public class {ViewModelClassName}").AppendLine();
      sb.Append("{").AppendLine();
      foreach (var entry in this.Columns)
      {
        var friendlyType = entry.Type;
        if (TypeUtility.SafeTypes.TryGetValue(entry.Type, out friendlyType) == false)
        {
          friendlyType = entry.Type;
        }

        if (entry.IsNullable && TypeUtility.NullableTypes.Contains(entry.Type))
        {
          friendlyType += "?";
        }

        sb.Append($"\tpublic {friendlyType} {entry.Name} {{get; set;}}").AppendLine();
      }

      sb.Append("}").AppendLine();

      this.ViewModelClass = sb.ToString();

    }

    private void BuildMapper()
    {
      StringBuilder sb = new StringBuilder();

      sb.Append($"public class {ClassName}Parameters").AppendLine();
      sb.Append("{").AppendLine();
      foreach (var param in Parameters)
      {
        param.FriendlyType = param.Type;
        string friendlyType = param.Type;
        if (TypeUtility.SQLToCSharp.TryGetValue(param.Type, out friendlyType) == false)
        {
          param.FriendlyType = param.Type;
        }
        else
        {
          param.FriendlyType = friendlyType;
        }


        if (param.IsNullable && TypeUtility.ExcludeNullableCSharpTypes.Contains(friendlyType) == false)
        {
          sb.Append($"\tpublic {param.FriendlyType}? {param.CSharpPropertyName} {{get; set;}}").AppendLine();
        }
        else
        {
          sb.Append($"\tpublic {param.FriendlyType} {param.CSharpPropertyName} {{get; set;}}").AppendLine();
        }
      }
      sb.Append("}").AppendLine();

      sb.Append($"public class {ClassName}Mapper").AppendLine();
      sb.Append("{").AppendLine();

      sb.Append($"public IGetResult<List<{ClassName}>> LoadData({ClassName}Parameters searchParams)").AppendLine();
      sb.Append("\t{").AppendLine();
      sb.AppendLine("\t\treturn this.WrapTryCatch(()=> ");
      sb.AppendLine("\t\t{");
      sb.AppendLine($"\t\t\tusing (var rdr = HQS.Data.DataReaderFactory.Create(\"{this.Database}\", \"{this.Procedure.Name}\", \"CONNECTION STRING NAME\"))").AppendLine();
      sb.AppendLine("\t\t\t{");
      if (Parameters.Count > 0)
      {
        sb.AppendLine("\t\t\t\tvar cmd = rdr.Command as System.Data.SqlClient.SqlCommand;").AppendLine();
        foreach (var parameter in Parameters)
        {
          if (parameter.IsNullable)
          {
            if (TypeUtility.ExcludeNullableCSharpTypes.Contains(parameter.FriendlyType))
            {
              sb.AppendLine($"\t\t\t\tif( searchParams.{parameter.CSharpPropertyName} != null)");
            }
            else
            {
              sb.AppendLine($"\t\t\t\tif( searchParams.{parameter.CSharpPropertyName}.HasValue)");
            }

            sb.AppendLine("\t\t\t\t\t{");
            sb.AppendLine($"\t\t\t\t\t\tcmd.Parameters.AddWithValue(\"{parameter.CSharpPropertyName}\", searchParams.{parameter.CSharpPropertyName});");
            sb.AppendLine("\t\t\t\t\t}");
          }
          else
          {
            sb.AppendLine($"\t\t\t\tcmd.Parameters.AddWithValue(\"{parameter.CSharpPropertyName}\", searchParams.{parameter.CSharpPropertyName});");
          }
        }
        sb.AppendLine($"\t\t\t\tvar map = Mapper<{ClassName}>.Create();");
        sb.AppendLine("\t\t\t\treturn rdr.Select(f => f.Get(map)).ToList();").AppendLine();
      }

      sb.AppendLine("\t\t\t}");
      sb.AppendLine("\t\t});");
      sb.AppendLine("\t}");
      sb.Append("}").AppendLine();


      sb.Append($"internal static {ViewModelClassName} CreateViewModel({ClassName} domain)").AppendLine();
      sb.Append("{").AppendLine();
      sb.Append($"var viewModel = new {ViewModelClassName}();").AppendLine();
      foreach (var entry in this.Columns)
      {
        sb.Append($"\tviewModel.{entry.Name} = domain.{entry.Name};").AppendLine();
      }
      sb.Append("}").AppendLine();

      this.Mapper = sb.ToString();
    }

    public ViewModels.StoredProcedureViewModel Procedure
    {
      get { return _procedure; }
      set
      {
        if (SetIfDirty(ref _procedure, value))
        {
          this.Parameters.Clear();
          if (this.Procedure != null)
          {
            try
            {
              var sprocParams = SQL.GetStoredProcedureParamters(this.Database, this.Procedure.Name, this.Procedure.Owner, this.ConnectionString);
              if (sprocParams.Count > 0)
              {
                foreach (var entry in sprocParams)
                {
                  entry.PassNull = true;
                  this.Parameters.Add(entry);
                }
              }

              this.ViewModelClassName = string.Empty;
              this.ClassName = this.Procedure.Name.Replace("_", "");
            }
            catch (Exception e)
            {
              MessageBox.Show(e.Message);
            }
          }
        }

        this.TriggerPropertyChanged(() => this.Parameters);
      }
    }

    private string _database;
    public string Database
    {
      get { return _database; }
      set
      {
        this.Procedures.Clear();
        if (SetIfDirty(ref _database, value))
        {
          if (string.IsNullOrWhiteSpace(this.Database) == false)
          {

            try
            {
              var sprocs = SQL.GetStoredProcedures(this.Database, _connectionString);
              if (sprocs.Count > 0)
              {
                foreach (var entry in sprocs)
                {
                  this.Procedures.Add(entry);
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
    public ObservableCollection<ViewModels.StoredProcedureParameterViewModel> Parameters { get; }
    public ObservableCollection<StoredProcedureViewModel> Procedures { get; }
    public ObservableCollection<StoredProcedureColumnViewModel> Columns { get; }
  }
}
