using WpfApp1.ViewModels;

namespace WpfApp1.ViewModels
{
  public class StoredProcedureParameterViewModel: BaseViewModel
	{
		public string Name { get; set; }
		public string Type { get; set; }
		public int Length { get; set; }
		public int Precision { get; set; }
		public int? Scale { get; set; }
		public int Order { get; set; }

		private string _runTimeValue;
		public string RunTimeValue
		{
			get { return _runTimeValue; }
			set { SetIfDirty(ref _runTimeValue, value); }
		}

		private bool _passNull;
		public bool PassNull
		{
			get { return _passNull; }
			set { SetIfDirty(ref _passNull, value); }
		}

		private bool _isNullable;
		public bool IsNullable
		{
			get { return _isNullable; }
			set { SetIfDirty(ref _isNullable, value); }
		}

		public string CSharpPropertyName
		{
			get
			{
				return (this.Name ?? "").Replace("@", "");
			}
		}

		public string FriendlyType { get; internal set; }
	}
}
