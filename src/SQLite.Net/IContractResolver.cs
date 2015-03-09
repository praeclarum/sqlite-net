using System;
using JetBrains.Annotations;

namespace SQLite.Net
{
    /// <summary>
    ///     Interface for the ContractResolver. This interface provides the contract for resolving interfaces to concreate
    ///     classes during
    ///     creation of an object/complex type
    /// </summary>
    public interface IContractResolver
    {
        /// <summary>
        ///     Gets or sets the can create function method.
        ///     This function take a <see cref="Type" /> object
        /// </summary>
        /// <value>
        ///     Returns true if the type can be resolved.  Note, if the default constructor is used, this will always return
        ///     true
        /// </value>
        [PublicAPI]
        Func<Type, bool> CanCreate { get; }

        /// <summary>
        ///     Gets or sets the create function method.
        ///     This function take a <see cref="Type" /> object and an array of <see cref="Object" /> items that can be passed to
        ///     the constructor
        ///     if the resolve supports it.
        /// </summary>
        /// <value>The create.</value>
        [PublicAPI]
        Func<Type, object[], object> Create { get; }

        /// <summary>
        ///     Creates the object.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="constructorArgs">The constructor arguments.</param>
        /// <returns>System.Object.</returns>
        [PublicAPI]
        object CreateObject(Type type, object[] constructorArgs = null);
    }
}