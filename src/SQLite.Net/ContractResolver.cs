namespace SQLite.Net
{
	using System;

	public class ContractResolver : IContractResolver
	{
		private static ContractResolver _current;

		public ContractResolver() : this((t) => true, Activator.CreateInstance)
		{
		}

		public ContractResolver(Func<Type, bool> canCreate, Func<Type, object[], object> create)
		{
			CanCreate = canCreate;
			Create = create;
		}

		public Func<Type, bool> CanCreate { get; set; }

		public Func<Type, object[], object> Create { get; set; }

		public static ContractResolver Current
		{
			get
			{
				return _current ?? (_current = new ContractResolver());
			}
		}
	}
}