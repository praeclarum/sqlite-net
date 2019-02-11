using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite_net
{
	/// <summary>
	/// Class for Contains extension with StringComparison option
	/// </summary>
	public static class StringContainsExtension
	{
		/// <summary>
		/// Contains extension with StringComparison option
		/// </summary>
		/// <param name="source"></param>
		/// <param name="value"></param>
		/// <param name="comparisonType"></param>
		/// <returns></returns>
		public static bool Contains (this string source, string value, StringComparison comparisonType)
		{
			throw new NotImplementedException ("Method not implemented: for sqlite purpose only");
		}
	}
}
