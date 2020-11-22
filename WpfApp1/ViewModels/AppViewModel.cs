namespace WpfApp1.ViewModels
{
  public class AppViewModel : BaseViewModel
  {
    public StoredProcedureGeneratorViewModel StoredProcedures
    {
      get; set;
    }
    public TableGeneratorViewModel Tables { get; internal set; }
  }
}
