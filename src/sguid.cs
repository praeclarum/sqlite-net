using System;

namespace SQLite
{
	/// <summary>
	/// Represents a globally unique identifier (GUID) with a 
	/// shorter string value. Sguid
	/// </summary>
	public struct Sguid
	{
		#region Static

		/// <summary>
		/// A read-only instance of the Sguid class whose value 
		/// is guaranteed to be all zeroes. 
		/// </summary>
		public static readonly Sguid Empty = new Sguid(Guid.Empty);

		#endregion

		#region Fields

		private Guid _guid;

		#endregion

		#region Contructors

		/// <summary>
		/// Creates a Sguid from a base64 encoded string
		/// </summary>
		/// <param name="value">The encoded guid as a 
		/// base64 string</param>
		public Sguid(string value) {
			_guid = Guid.Empty;
			if (!(value [8] == '-' && Guid.TryParse (value, out _guid)) && !string.IsNullOrWhiteSpace (value)) {
				_guid = Decode (value);
			}
		}

		/// <summary>
		/// Creates a Sguid from a Guid
		/// </summary>
		/// <param name="guid">The Guid to encode</param>
		public Sguid(Guid guid) {
			_guid = guid;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Gets/sets the underlying Guid
		/// </summary>
		public Guid Guid {
			get { return _guid; }
			set { _guid = value; }
		}

		/// <summary>
		/// Gets/sets the underlying base64 encoded string
		/// </summary>
		public string Value {
			get { return Encode (_guid); }
			set { _guid = Decode (value); }
		}

		#endregion

		#region ToString

		/// <summary>
		/// Returns the base64 encoded guid as a string
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return Encode (_guid);
		}

		#endregion

		#region Equals

		/// <summary>
		/// Returns a value indicating whether this instance and a 
		/// specified Object represent the same type and value.
		/// </summary>
		/// <param name="obj">The object to compare</param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			return _guid.Equals (obj);
		}

		#endregion

		#region GetHashCode

		/// <summary>
		/// Returns the HashCode for underlying Guid.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			return _guid.GetHashCode();
		}

		#endregion

		#region NewGuid

		/// <summary>
		/// Initialises a new instance of the Sguid class
		/// </summary>
		/// <returns></returns>
		public static Sguid NewGuid() {
			return new Sguid(Guid.NewGuid());
		}

		#endregion

		#region Encode

		/// <summary>
		/// Creates a new instance of a Guid using the string value, 
		/// then returns the base64 encoded version of the Guid.
		/// </summary>
		/// <param name="value">An actual Guid string (i.e. not a Sguid)</param>
		/// <returns></returns>
		public static string Encode(string value) {
			Guid guid = new Guid(value);
			return Encode(guid);
		}

		/// <summary>
		/// Encodes the given Guid as a base64 string that is 22 
		/// characters long.
		/// </summary>
		/// <param name="guid">The Guid to encode</param>
		/// <returns></returns>
		public static string Encode(Guid guid) {
			string encoded = Convert.ToBase64String(guid.ToByteArray());
			return encoded
				.Replace("/", "_")
				.Replace("+", "-")
				.Substring(0, 22);
		}

		#endregion

		#region Decode

		/// <summary>
		/// Decodes the given base64 string
		/// </summary>
		/// <param name="value">The base64 encoded string of a Guid</param>
		/// <returns>A new Guid</returns>
		public static Guid Decode(string value) {
			value = value
				.Replace("_", "/")
				.Replace("-", "+");
			byte[] buffer = Convert.FromBase64String(value + "==");
			return new Guid(buffer);
		}

		#endregion

		#region Operators

		/// <summary>
		/// Determines if both Sguids have the same underlying 
		/// Guid value.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static bool operator ==(Sguid x, Sguid y) {
			if ((object)x == null) return (object)y == null;
			return x._guid == y._guid;
		}

		/// <summary>
		/// Determines if both Sguids do not have the 
		/// same underlying Guid value.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static bool operator !=(Sguid x, Sguid y) {
			return !(x == y);
		}

		/// <summary>
		/// Implicitly converts the Sguid to it's string equivilent
		/// </summary>
		/// <param name="Sguid"></param>
		/// <returns></returns>
		public static implicit operator string(Sguid sguid) {
			return Encode(sguid._guid);
		}

		/// <summary>
		/// Implicitly converts the Sguid to it's Guid equivilent
		/// </summary>
		/// <param name="Sguid"></param>
		/// <returns></returns>
		public static implicit operator Guid(Sguid sguid) {
			return sguid._guid;
		}

		/// <summary>
		/// Implicitly converts the string to a Sguid
		/// </summary>
		/// <param name="Sguid"></param>
		/// <returns></returns>
		public static implicit operator Sguid(string sguid) {
			return new Sguid(sguid);
		}

		/// <summary>
		/// Implicitly converts the Guid to a Sguid 
		/// </summary>
		/// <param name="guid"></param>
		/// <returns></returns>
		public static implicit operator Sguid(Guid guid) {
			return new Sguid(guid);
		}

		#endregion

	}
}
