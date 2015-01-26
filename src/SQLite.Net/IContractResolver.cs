namespace SQLite.Net
{
	using System;

	public interface IContractResolver
	{
		Func<Type, bool> CanCreate { get; set; }
		Func<Type, object[], object> Create { get; set; }
    }
}