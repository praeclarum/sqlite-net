using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;

[Generator]
public class SQLiteFastColumnSetterGenerator : IIncrementalGenerator
{
	private static ConcurrentDictionary<IEquatable<ISymbol?>, bool> cachedHasSqliteAttribute = new ();
	private static ConcurrentDictionary<IEquatable<ISymbol?>, bool> cachedHasTableAttribute = new ();
	private static List<string> SQLitePropertyAttributes = default!;
	private static ImmutableHashSet<string> SQLitePropertyFullAttributes = default!;

	static SQLiteFastColumnSetterGenerator ()
	{
		SQLitePropertyAttributes = new() {
			"Column",
			"Indexed",
			"Ignore",
			"Unique",
			"MaxLength",
			"Collation",
			"NotNull",
			"StoreAsText",
			"AutoIncrement",
			"PrimaryKey",
			"NotNull"
		};

		SQLitePropertyFullAttributes = SQLitePropertyAttributes.Select (f => f + "Attribute").ToImmutableHashSet();
	}

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
		// Launch Debugger for Debugging the Analyzer
		// System.Diagnostics.Debugger.Launch();

		// Find all classes with TableAttribute or properties with ColumnAttribute
		var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateClass(s),
                transform: static (ctx, _) => GetClassInfo(ctx))
            .Where(static m => m is not null);

		// Get analyzer config options for accessing MSBuild properties
		var configOptions = context.AnalyzerConfigOptionsProvider;
		
        // Combine with compilation
        var compilationAndClasses = context.CompilationProvider
	        .Combine(classDeclarations.Collect())
	        .Combine(configOptions);

        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Left.Left, source.Left.Right!, source.Right, spc));
    }

    static bool IsCandidateClass(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Check if class has TableAttribute
        if (classDecl.AttributeLists.Any(attrList => 
            attrList.Attributes.Any(attr => attr.Name.ToString ().Contains("Table"))))
        {
            return true;
        }

		// I need to analyse the base class in the semantic model
        if (HasBaseClass (classDecl)) {
	        return true;
        }

        // Check if any property has SQLite Property Attribute
        return classDecl.Members
	        .OfType<PropertyDeclarationSyntax> ()
	        .Any (prop => prop.AttributeLists.Any (attrList =>
		        attrList.Attributes.Any (attr => {
			        var attributeName = attr.Name.ToString ();
			        return SQLitePropertyAttributes.Any (f => attributeName.Contains (f));
		        })));
    }

    static bool HasBaseClass (ClassDeclarationSyntax classDecl)
    {
	    var baseList = classDecl.BaseList;
	    if (baseList == null)
		    return false;

	    return baseList.Types.Count > 0;
    }

    static ClassInfo? GetClassInfo (INamedTypeSymbol? classSymbol)
    {
	    if (classSymbol is null)
		    return null;

	    // Return null if the class is private
	    if (classSymbol.DeclaredAccessibility == Accessibility.Private)
		    return null;

	    if (classSymbol.IsGenericType)
		    return null;

	    var hasSqliteAttributes = HasTableAttribute (classSymbol);
	    if (!hasSqliteAttributes) {
		    hasSqliteAttributes = HasSQLiteAttribute (classSymbol);
		}

	    if (!hasSqliteAttributes) {
			return null;
	    }

	    var properties = new List<PropertyInfo> ();

	    // Iterate through the class hierarchy to get all properties
	    var currentType = classSymbol;
	    while (currentType != null) {
		    foreach (var member in currentType.GetMembers ().OfType<IPropertySymbol> ()) {
			    if (!member.IsReadOnly && !member.IsStatic && !member.IsIndexer && (member.DeclaredAccessibility != Accessibility.Private && member.DeclaredAccessibility != Accessibility.Protected) ) {
				    var ignore = member.GetAttributes ()
					    .Any (attr => IsIgnoreAttribute (attr.AttributeClass));

				    // Include property if not ignored
				    if (!ignore) {
					    var columnName = GetColumnName (member);
					    properties.Add (new PropertyInfo (member.Name, member.Type.ToDisplayString (), columnName, GetEnumInfo(member)));
				    }
			    }
		    }

		    // Move to base type
		    currentType = currentType.BaseType;

		    // Stop at System.Object or if we hit a null base type
		    if (currentType?.SpecialType == SpecialType.System_Object)
			    break;
	    }

	    if (properties.Count == 0)
		    return null;

	    // Handle nested classes by building the full containing type path
	    var containingTypes = new List<string> ();
	    var currentContaining = classSymbol.ContainingType;
	    while (currentContaining != null) {
		    containingTypes.Insert (0, currentContaining.Name);
		    currentContaining = currentContaining.ContainingType;
	    }

	    var fullClassName = containingTypes.Count > 0
		    ? $"{string.Join (".", containingTypes)}.{classSymbol.Name}"
		    : classSymbol.Name;

	    return new ClassInfo (
		    fullClassName,
		    classSymbol.ContainingNamespace?.ToDisplayString () ?? string.Empty,
		    properties);
	}

    private static EnumInfo? GetEnumInfo (IPropertySymbol member)
	{
		var type = member.Type;
		if (type is INamedTypeSymbol named &&
		    named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
			type = named.TypeArguments[0];
		}

		if (type.TypeKind == TypeKind.Enum) {
			var storeAsText = type.GetAttributes ()
				.Any (attr => IsStoreAsTextAttribute(attr.AttributeClass));
			return new EnumInfo (storeAsText);
		}

		return null;
	}

    private static bool HasSQLiteAttribute (INamedTypeSymbol? classSymbol)
    {
	    if (classSymbol != null && cachedHasSqliteAttribute.TryGetValue(classSymbol, out var result)) {
		    return result;
	    }

	    while (true) {
		    if (classSymbol == null || classSymbol.SpecialType == SpecialType.System_Object) {
			    if (classSymbol != null) {
				    cachedHasSqliteAttribute[classSymbol] = false;
			    }
			    return false;
		    }

		    var members = classSymbol.GetMembers();
		    foreach (var member in members) {
				if (member.GetAttributes().Any(attr => IsSQLiteAttribute (attr.AttributeClass)))
				{
					cachedHasSqliteAttribute[classSymbol] = true;
					return true;
				}
		    }
			    
		    classSymbol = classSymbol.BaseType;
	    }
    }

	private static bool HasTableAttribute (INamedTypeSymbol? classSymbol)
    {
	    if (classSymbol != null && cachedHasTableAttribute.TryGetValue (classSymbol, out var result)) {
		    return result;
	    }

		while (true) {
		    if (classSymbol == null || classSymbol.SpecialType == SpecialType.System_Object) {
			    if (classSymbol != null) {
					cachedHasTableAttribute[classSymbol] = false;
			    }
			    return false;
		    }

		    var hasTableAttribute = classSymbol.GetAttributes ()
			    .Any (attr => IsTableAttribute (attr.AttributeClass));
		    if (hasTableAttribute) {
			    cachedHasTableAttribute[classSymbol] = true;
				return true;
		    }

		    classSymbol = classSymbol.BaseType;
	    }
    }

    static ClassInfo? GetClassInfo(GeneratorSyntaxContext context)
    {
	    var classDecl = (ClassDeclarationSyntax)context.Node;
	    var semanticModel = context.SemanticModel;

	    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
	    return GetClassInfo (classSymbol);
    }

    private static bool IsTableAttribute (INamedTypeSymbol? attributeClass)
    {
	    while (true) {
		    if (attributeClass == null) {
			    return false;
		    }

		    if (IsSQLiteNamespace (attributeClass) && attributeClass.Name == "TableAttribute") {
			    return true;
		    }

		    attributeClass = attributeClass.BaseType;
	    }
    }

    private static bool IsStoreAsTextAttribute (INamedTypeSymbol? attributeClass)
    {
	    while (true) {
		    if (attributeClass == null) {
			    return false;
		    }

		    if (IsSQLiteNamespace (attributeClass) && attributeClass.Name == "StoreAsTextAttribute") {
			    return true;
		    }

		    attributeClass = attributeClass.BaseType;
	    }
    }

    private static bool IsIgnoreAttribute (INamedTypeSymbol? attributeClass)
    {
	    while (true) {
		    if (attributeClass == null) {
			    return false;
		    }

		    if (IsSQLiteNamespace (attributeClass) && attributeClass.Name == "IgnoreAttribute") {
			    return true;
		    }

		    attributeClass = attributeClass.BaseType;
	    }
    }

    private static bool IsSQLiteAttribute (INamedTypeSymbol? attributeClass)
    {
	    while (true) {
		    if (attributeClass == null) {
			    return false;
		    }

		    if (IsSQLiteNamespace (attributeClass) && SQLitePropertyFullAttributes.Contains (attributeClass.Name)) {
			    return true;
		    }

		    attributeClass = attributeClass.BaseType;
	    }
    }

    private static bool IsSQLiteNamespace (INamedTypeSymbol attributeClass)
    {
	    return attributeClass.ContainingNamespace.Name == "SQLite";
    }

    static string GetColumnName(IPropertySymbol property)
    {
        // Check for ColumnAttribute with name parameter
        var columnAttr = property.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ContainingNamespace.Name == "SQLite" && attr.AttributeClass?.Name == "ColumnAttribute");

        if (columnAttr?.ConstructorArguments.Length > 0)
        {
            var nameArg = columnAttr.ConstructorArguments[0];
            if (nameArg.Value is string columnName)
                return columnName;
        }

        // Default to property name
        return property.Name;
    }

    static void Execute(
	    Compilation compilation,
	    ImmutableArray<ClassInfo> classes, 
	    AnalyzerConfigOptionsProvider configOptionsProvider,
	    SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
            return;
        
        // Get the assembly name/namespace from the compilation
        var assemblyName = compilation.AssemblyName ?? "Generated";
        var rootNamespace = GetRootNamespace(configOptionsProvider, compilation) ?? assemblyName;

        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine("using SQLite;");
        sb.AppendLine("#pragma warning disable CS0618 // Disable obsolete Warnings");
		sb.AppendLine();
        sb.AppendLine($"namespace {rootNamespace}");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary> SQLite Initializer Class </summary>");
		sb.AppendLine("    [SQLite.Preserve(AllMembers = true)]");
		sb.AppendLine("    public static class SQLiteInitializer");
        sb.AppendLine("    {");
        sb.AppendLine("        private static bool initialized;");
        sb.AppendLine("        /// <summary> Init SQLite Fast Column Setters </summary>");
        sb.AppendLine("#if NET5_0_OR_GREATER");
		sb.AppendLine("        [System.Runtime.CompilerServices.ModuleInitializer]");
		sb.AppendLine("#endif");
		sb.AppendLine("        public static void Init()");
        sb.AppendLine("        {");
        sb.AppendLine ("           if (initialized)");
        sb.AppendLine ("               return;");
        sb.AppendLine ("           initialized = true;");

		foreach (var classInfo in classes)
        {
            foreach (var property in classInfo.Properties)
            {
                var fullTypeName = string.IsNullOrEmpty(classInfo.Namespace) 
                    ? classInfo.ClassName 
                    : $"{classInfo.Namespace}.{classInfo.ClassName}";

                sb.AppendLine($"            SQLiteConnection.RegisterFastColumnSetter(");
                sb.AppendLine($"                typeof({fullTypeName}),");
                sb.AppendLine($"                \"{property.ColumnName}\",");
                sb.AppendLine($"                (obj, stmt, index) => ");
                sb.AppendLine($"                {{");
                sb.AppendLine($"                    var typedObj = ({fullTypeName})obj;");
                sb.AppendLine($"                    var colType = SQLite3.ColumnType(stmt, index);");
                sb.AppendLine($"                    if (colType != SQLite3.ColType.Null)");
                sb.AppendLine($"                    {{");
                
                // Generate appropriate setter based on property type
                GeneratePropertySetter(sb, property);
                
                sb.AppendLine($"                    }}");
                sb.AppendLine($"                }});");
                sb.AppendLine();
            }
        }

        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("SQLiteInitializer.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
    
    static string? GetRootNamespace(AnalyzerConfigOptionsProvider configOptionsProvider, Compilation compilation)
    {
	    if (configOptionsProvider.GlobalOptions.TryGetValue ("build_property.RootNamespace", out var rootNs)) {
			return rootNs;
	    }
			
	    // Fallback to assembly name
	    return compilation.AssemblyName;
    }


    static void GeneratePropertySetter(StringBuilder sb, PropertyInfo property)
    {
        var propertyType = property.TypeName;
        
        // Handle nullable types
        var isNullable = propertyType.Contains("?");
        if (isNullable)
            propertyType = propertyType.Replace("?", "");

        switch (propertyType)
        {
            case "string":
            case "String":
            case "System.String":
                sb.AppendLine($"                        typedObj.{property.PropertyName} = SQLite3.ColumnString(stmt, index);");
                break;

            case "byte":
            case "Byte":
            case "System.Byte":
                sb.AppendLine ($"                        typedObj.{property.PropertyName} = (byte)SQLite3.ColumnInt(stmt, index);");
                break;

			case "short":
            case "Int16":
            case "System.Int16":
                sb.AppendLine ($"                        typedObj.{property.PropertyName} = (short)SQLite3.ColumnInt(stmt, index);");
                break;


			case "int":
            case "Int32":
            case "System.Int32":
                sb.AppendLine($"                        typedObj.{property.PropertyName} = SQLite3.ColumnInt(stmt, index);");
                break;

            case "long":
            case "Int64":
            case "System.Int64":
                sb.AppendLine($"                        typedObj.{property.PropertyName} = SQLite3.ColumnInt64(stmt, index);");
                break;

            case "double":
            case "Double":
            case "System.Double":
                sb.AppendLine($"                        typedObj.{property.PropertyName} = SQLite3.ColumnDouble(stmt, index);");
                break;

            case "decimal":
            case "Decimal":
            case "System.Decimal":
	            sb.AppendLine ($"                        typedObj.{property.PropertyName} = System.Convert.ToDecimal(SQLite3.ColumnDouble(stmt, index));");
	            break;

			case "float":
            case "Single":
            case "System.Single":
                sb.AppendLine($"                        typedObj.{property.PropertyName} = (float)SQLite3.ColumnDouble(stmt, index);");
                break;

            case "bool":
            case "Boolean":
            case "System.Boolean":
                sb.AppendLine($"                        typedObj.{property.PropertyName} = SQLite3.ColumnInt(stmt, index) == 1;");
                break;

            case "DateTime":
            case "System.DateTime":
                sb.AppendLine($"                        typedObj.{property.PropertyName} = new DateTime(SQLite3.ColumnInt64(stmt, index));");
                break;

            case "TimeSpan":
            case "System.TimeSpan":
	            sb.AppendLine ($"                        typedObj.{property.PropertyName} = new TimeSpan(SQLite3.ColumnInt64(stmt, index));");
	            break;

			case "Guid":
            case "System.Guid":
                sb.AppendLine($"                        var text = SQLite3.ColumnString(stmt, index);");
                sb.AppendLine($"                        typedObj.{property.PropertyName} = new Guid(text);");
                break;

            case "byte[]":
                sb.AppendLine($"                        typedObj.{property.PropertyName} = SQLite3.ColumnByteArray(stmt, index);");
                break;

            default:
	            if (property.Enum != null) {
		            // For other types, try to use a generic approach
		            sb.AppendLine ($"                        // Enum setter for {propertyType}");
		            if (property.Enum.StoreAsText) {
			            sb.AppendLine ($"                        var value = SQLite3.ColumnString(stmt, index);");
						sb.AppendLine ($"                        if (value != null)");
						sb.AppendLine ($"                        {{");
						sb.AppendLine ($"                            typedObj.{property.PropertyName} = ({propertyType})Enum.Parse(typeof({propertyType}), value, ignoreCase: true);");
						sb.AppendLine ($"                        }}");
					}
		            else {
			            sb.AppendLine ($"                        var value = SQLite3.ColumnInt(stmt, index);");
			            sb.AppendLine ($"                        typedObj.{property.PropertyName} = ({propertyType})value;");
		            }
	            }
	            else {
		            // For other types, try to use a generic approach
		            sb.AppendLine ($"                        // Generic setter for {propertyType}");
		            sb.AppendLine ($"                        var value = SQLite3.ColumnString(stmt, index);");
		            sb.AppendLine ($"                        if (value != null)");
		            sb.AppendLine ($"                        {{");
		            sb.AppendLine ($"                            typedObj.{property.PropertyName} = ({propertyType})Convert.ChangeType(value, typeof({propertyType}));");
		            sb.AppendLine ($"                        }}");
	            }

	            break;
        }
    }

    record ClassInfo(string ClassName, string Namespace, List<PropertyInfo> Properties);
    record PropertyInfo(string PropertyName, string TypeName, string ColumnName, EnumInfo? Enum);
    record EnumInfo (bool StoreAsText);
}
