using System;
using JetBrains.Annotations;

namespace SQLite.Net
{
    /// <summary>
    ///     Provide a default implementation of the ContractResolver.
    ///     This Contract Resolver will by default use the Reflection API Activator CreateInstance Method to create
    ///     a valid object.  However you can pass in to the constructor custom functions to handle the resolution.
    /// </summary>
    /// <example>
    ///     var cr = new ContractResolver();
    /// </example>
    /// <example>
    ///     var cr = new ContractResolver(t => container.CanCreate, (t, op) => container.Resolve(t, op));
    /// </example>
    public class ContractResolver : IContractResolver
    {
        private static ContractResolver _current;
        private readonly Func<Type, bool> _canCreate;
        private readonly Func<Type, object[], object> _create;

        /// <summary>
        ///     Default Initializer for a new instance of the <see cref="ContractResolver" /> class.
        ///     This constructor sets the resolver to the .NET Refection based Activator.CreateInstance method
        /// </summary>
        [PublicAPI]
        public ContractResolver() : this(t => true, Activator.CreateInstance)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ContractResolver" /> class.
        ///     This constructor allows for custom functions to be specified to redirect the CanCreate and Create operations to
        ///     a custom IoC container
        /// </summary>
        /// <param name="canCreate">The can create.</param>
        /// <param name="create">The create.</param>
        [PublicAPI]
        public ContractResolver(Func<Type, bool> canCreate, Func<Type, object[], object> create)
        {
            if (canCreate == null)
            {
                throw new ArgumentNullException("canCreate");
            }
            if (create == null)
            {
                throw new ArgumentNullException("create");
            }
            _canCreate = canCreate;
            _create = create;
        }

        /// <summary>
        ///     Provide access to a static default ContractResolver.  This is used to support the original default
        ///     behavior of the library prior to the addition of this functionality
        /// </summary>
        /// <value>The current.</value>
        [PublicAPI]
        public static ContractResolver Current
        {
            get { return _current ?? (_current = new ContractResolver()); }
        }

        /// <summary>
        ///     Gets or sets the can create function method.
        ///     This function take a <see cref="Type" /> object
        /// </summary>
        /// <value>
        ///     Returns true if the type can be resolved.  Note, if the default constructor is used, this will always return
        ///     true
        /// </value>
        [PublicAPI]
        public Func<Type, bool> CanCreate
        {
            get { return _canCreate; }
        }

        /// <summary>
        ///     Gets or sets the create function method.
        ///     This function take a <see cref="Type" /> object and an array of <see cref="Object" /> items that can be passed to
        ///     the constructor
        ///     if the resolve supports it.
        /// </summary>
        /// <value>The create.</value>
        [PublicAPI]
        public Func<Type, object[], object> Create
        {
            get { return _create; }
        }

        /// <summary>
        ///     Creates the object.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="constructorArgs">The constructor arguments.</param>
        /// <returns>System.Object.</returns>
        [PublicAPI]
        public object CreateObject(Type type, object[] constructorArgs = null)
        {
            if (CanCreate == null || CanCreate(type))
            {
                return Create(type, constructorArgs);
            }

            return null;
        }
    }
}