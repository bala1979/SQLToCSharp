using WpfApp1.ViewModels;

namespace WpfApp1.ViewModels
{
  public class StoredProcedureColumnViewModel: BaseViewModel
	{
		public string Name { get; set; }
		public string Type { get; set; }

		private bool _isNullable;
		public bool IsNullable
		{
			get { return _isNullable; }
			set { SetIfDirty(ref _isNullable, value); }
		}

		public string DataTypeName { get; internal set; }
		public string SpecificFieldType { get; internal set; }
	}
}
