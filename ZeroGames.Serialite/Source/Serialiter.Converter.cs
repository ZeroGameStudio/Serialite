// Copyright Zero Games. All Rights Reserved.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ZeroGames.Serialite;

partial class Serialiter
{
    
    private static Type? GetGenericInstanceOf(Type thisType, Type targetType)
    {
        if (!targetType.IsGenericTypeDefinition)
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        // Check whether any base type of this type is an instance of targetType.
        Type? currentType = thisType;
        while (currentType is not null)
        {
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == targetType)
            {
                return currentType;
            }
				
            currentType = currentType.BaseType;
        }

        // Check whether any implemented interface of this type is an instance of targetType.
        return thisType.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == targetType);
    }
    
    private void Convert(Node ast, object dest)
    {
        Type type = dest.GetType();
        
        if (ast.Type is not ENodeType.Object)
        {
            throw new FormatException("Expected object node for conversion");
        }

        if (ast.Value.Length > 0 && Context.GetType(ast.Value, type) != type)
        {
            throw new FormatException();
        }

        List<Type> allTypes = [type];
        Type? cur = type;
        while ((cur = cur?.BaseType) is not null)
        {
            allTypes.Add(cur);
        }
        
        // 1. Found all settable auto properties (property with compiler generated backing field).
        HashSet<string> backingFields = allTypes
            .SelectMany(t => t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            .Where(f => BackingFieldRegex.IsMatch(f.Name) && f.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
            .Select(f => f.Name.Substring(1, f.Name.IndexOf('>') - 1))
            .ToHashSet();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && backingFields.Contains(p.Name))
            .ToArray();

        HashSet<PropertyInfo> requiredProperties = properties.Where(p => p.GetCustomAttribute<RequiredMemberAttribute>() is not null).ToHashSet();

        HashSet<string> names = properties.Select(p => p.Name).ToHashSet();
        if (ast.Children is not null && names.Union(ast.Children.Keys).Count() != names.Count)
        {
            throw new FormatException();
        }
        
        // 2. Fill these properties:
        foreach (PropertyInfo property in properties)
        {
            if (ast.Children?.TryGetValue(property.Name, out Node propertyNode) is not true)
            {
                continue;
            }
            
            object? value = ConvertValue(propertyNode, property.PropertyType);
            property.SetValue(dest, value);
            requiredProperties.Remove(property);
        }

        if (requiredProperties.Count > 0)
        {
            throw new FormatException($"Missing required properties: {string.Join(", ", requiredProperties.Select(p => p.Name))}.");
        }
    }

    private object? ConvertValue(Node node, Type type)
        => node.Type switch
        {
            ENodeType.Number => ConvertNumber(node.Value, type),
            ENodeType.Bool => bool.Parse(node.Value),
            ENodeType.String => ConvertString(node.Value, type), // Support IParsable.
            ENodeType.Object => ConvertObject(node, type),
            ENodeType.List => ConvertList(node, type),
            ENodeType.Map => ConvertMap(node, type),
            ENodeType.Null => null,
            ENodeType.Identifier => ConvertIdentifier(node.Value, type),
            _ => throw new FormatException()
        };

    private object ConvertNumber(string value, Type type)
    {
        if (type == typeof(uint8))
        {
            return uint8.Parse(value);
        }
        else if (type == typeof(uint16))
        {
            return uint16.Parse(value);
        }
        else if (type == typeof(uint32))
        {
            return uint32.Parse(value);
        }
        else if (type == typeof(uint64))
        {
            return uint64.Parse(value);
        }
        else if (type == typeof(UInt128))
        {
            return UInt128.Parse(value);
        }
        else if (type == typeof(int8))
        {
            return int8.Parse(value);
        }
        else if (type == typeof(int16))
        {
            return int16.Parse(value);
        }
        else if (type == typeof(int32))
        {
            return int32.Parse(value);
        }
        else if (type == typeof(int64))
        {
            return int64.Parse(value);
        }
        else if (type == typeof(UInt128))
        {
            return UInt128.Parse(value);
        }
        else if (type == typeof(float))
        {
            return float.Parse(value);
        }
        else if (type == typeof(double))
        {
            return double.Parse(value);
        }
        else if (type == typeof(decimal))
        {
            return decimal.Parse(value);
        }
        else
        {
            throw new FormatException();
        }
    }

    private object ConvertString(string value, Type type)
    {
        if (type == typeof(string))
        {
            return value;
        }
        else if (type.IsAssignableTo(typeof(IParsable<>).MakeGenericType(type)))
        {
            if (!_parseMethodCache.TryGetValue(type, out var parse))
            {
                parse = type.GetMethod(nameof(IParsable<>.Parse), BindingFlags.Public | BindingFlags.Static, ParseParameterTypes)! ;
                _parseMethodCache[type] = parse;
            }
            
            return parse.Invoke(null, [value, null]) ?? throw new FormatException($"Failed to parse [{value}] as [{type.Name}]");
        }

        throw new NotSupportedException($"Type [{type.Name}] is not parsable from string [{value}].");
    }

    private Serialiter SelectSerialiter(Type type)
    {
        Serialiter serialiter = this;
        if (InnerSerialiters is not null)
        {
            foreach (var (allowedType, innerSerialiter) in InnerSerialiters)
            {
                if (type.IsAssignableTo(allowedType))
                {
                    serialiter = innerSerialiter;
                    break;
                }
            }
        }

        return serialiter;
    }
    
    private object ConvertObject(Node node, Type type)
    {
        Serialiter serialiter = SelectSerialiter(type);
        if (node.Value.Length > 0)
        {
            type = serialiter.Context.GetType(node.Value, type);
        }
        
        object result = serialiter.ObjectFactory(type);
        Convert(node, result);
        return result;
    }

    private object ConvertList(Node node, Type type)
    {
        if (GetGenericInstanceOf(type, typeof(IReadOnlyList<>)) is { } genericListType)
        {
            Type elementType = genericListType.GetGenericArguments()[0];
            Type listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)ObjectFactory(listType);
            foreach (var (_, child) in node.Children ?? [])
            {
                list.Add(ConvertValue(child, elementType));
            }

            return list;
        }
        else if (GetGenericInstanceOf(type, typeof(IReadOnlySet<>)) is { } genericSetType)
        {
            Type elementType = genericSetType.GetGenericArguments()[0];
            Type setType = typeof(HashSet<>).MakeGenericType(genericSetType.GetGenericArguments()[0]);
            object set = ObjectFactory(setType);
            MethodInfo add = setType.GetMethod(nameof(HashSet<>.Add))!;
            foreach (var (_, child) in node.Children ?? [])
            {
                add.Invoke(set, [ConvertValue(child, elementType)]);
            }

            return set;
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    private object ConvertMap(Node node, Type type)
    {
        if (GetGenericInstanceOf(type, typeof(IReadOnlyDictionary<,>)) is { } genericMapType)
        {
            Type keyType = genericMapType.GetGenericArguments()[0];
            Type valueType = genericMapType.GetGenericArguments()[1];
            Type mapType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            var map = (IDictionary)ObjectFactory(mapType);
            foreach (var (_, pair) in node.Children ?? [])
            {
                map[ConvertValue(pair.Children!["Key"], keyType)!] = ConvertValue(pair.Children!["Value"], valueType);
            }

            return map;
        }
        else
        {
            throw new NotSupportedException();
        }
    }
    
    private object? ConvertIdentifier(string identifier, Type type)
    {
        if (type.IsEnum)
        {
            string enumName = type.Name;
            string maybeEnumValue = identifier;
            if (maybeEnumValue.Contains('.'))
            {
                string[] pair = maybeEnumValue.Split('.');
                if (pair.Length is 2 && pair[0] == enumName)
                {
                    maybeEnumValue = pair[1];
                }
            }

            if (Enum.TryParse(type, maybeEnumValue, out var result))
            {
                return result;
            }
        }

        return Context[identifier];
    }
    
    [GeneratedRegex("^<[A-Za-z_][A-Za-z0-9_]*>k__BackingField$")]
    private static partial Regex BackingFieldRegex { get; }

    [field: MaybeNull]
    private static Type[] ParseParameterTypes => field ??= [typeof(string), typeof(IFormatProvider)];
    
    private static readonly Dictionary<Type, MethodInfo> _parseMethodCache = [];
    
}


