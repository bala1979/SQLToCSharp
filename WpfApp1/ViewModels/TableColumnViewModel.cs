using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.ViewModels
{
  public class TableColumnViewModel : BaseViewModel
  {
    public string Name { get; set; }
    public bool IsNullable { get; set; }
    public string DataTypeName { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public int? MaxLength { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public int Position { get; set; }
  }
}
