using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.ViewModels
{
	public class BaseViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged = delegate { };

		public void OnPropertyChanged([CallerMemberName]string propertyName = "")
		{
			this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public bool SetIfDirty<T>(ref T backingField, T newValue, [CallerMemberName]string propertyName = "", Action<string, object> preSetCallback = null)
		{
			if (EqualityComparer<T>.Default.Equals(backingField, newValue))
			{
				return false;
			}

			this.IsDirty = true;
			backingField = newValue;
			if (preSetCallback != null)
			{
				preSetCallback(propertyName, newValue);
			}
			TriggerPropertyChanged(propertyName);
			this.StateChanged(propertyName);
			return true;
		}

		public bool IsDirty
		{
			get;
			private set;
		}


		public void TriggerPropertyChanged<T>(System.Linq.Expressions.Expression<Func<T>> propertyExpression)
		{
			MemberExpression body = propertyExpression.Body as MemberExpression;
			PropertyInfo prop = body.Member as PropertyInfo;
			TriggerPropertyChanged(prop.Name);
			this.StateChanged(prop.Name);
		}

		public void TriggerPropertyChanged(string propertyName)
		{
			this.OnPropertyChanged(propertyName);
		}

		public void StateChanged(string propertyName)
		{
			this.OnStateChanged(propertyName);
		}

		// Override this method if you don't care about what changed. You just need to be flagged that something changed. 
		protected virtual void OnStateChanged(string propertyName) { }

		public ObservableCollection<U> CreateTrackedObservableCollection<U>()
		{
			return this.CreateTrackedObservableCollection(Enumerable.Empty<U>());
		}

		public ObservableCollection<U> CreateTrackedObservableCollection<U>(IEnumerable<U> seed)
		{
			ObservableCollection<U> collection = new ObservableCollection<U>(seed);
			// collection.CollectionChanged += delegate { this.IsDirty = true; };
			return collection;
		}

		public virtual object Clone()
		{
			var copy = this.MemberwiseClone();
			return copy;
		}
	}

	public abstract class AbstractDomainViewModel<T> : BaseViewModel where T : class
	{
		private bool _isMappingCompeted = false;

		public bool IsMappingCompleted
		{
			get { return _isMappingCompeted; }
		}

		public T CopyAndStoreOriginal(T domainInstance)
		{
			return this.OnCopyAndStoreOriginal(domainInstance);
		}

		protected abstract T OnCopyAndStoreOriginal(T domainInstance);

		public void MapViewModelFromDomain(T domainInstance)
		{
			this.OnMapViewModelFromDomain(domainInstance);
			this._isMappingCompeted = true;
		}

		public void Revert() { OnRevert(); }

		protected virtual void OnRevert() { }

		protected abstract void OnMapViewModelFromDomain(T domainInstance);

		public void ApplyViewModelToDomain(T domainInstance)
		{
			this.OnApplyViewModelToDomain(domainInstance);
		}

		protected abstract void OnApplyViewModelToDomain(T domainInstance);
	}
}
