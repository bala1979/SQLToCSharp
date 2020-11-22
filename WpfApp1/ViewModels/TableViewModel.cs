using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.ViewModels
{
  public class TableViewModel
  {
    public string Name { get; set; }
    public bool IsTable { get; set; }
    public List<TableColumnViewModel> Columns { get; set; }
  }
}
