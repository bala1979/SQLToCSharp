using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp1.ViewModels;

namespace WpfApp1.ViewModels
{
  public class StoredProcedureViewModel: BaseViewModel 
  {
    public string Name { get; set; }
    public string Owner { get; set; }
    public List<StoredProcedureParameterViewModel> Parameters { get; set; }
    public List<StoredProcedureColumnViewModel> Columns { get; set; }
  }
}
