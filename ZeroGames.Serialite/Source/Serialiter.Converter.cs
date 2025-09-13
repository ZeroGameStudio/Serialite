// Copyright Zero Games. All Rights Reserved.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ZeroGames.Serialite;

partial class Serialiter
{
    
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
            .Where(f => _backingFieldRegex.IsMatch(f.Name) && f.GetCustomAttribute<CompilerGeneratedAttribute>() is not null)
            .Select(f => f.Name.Substring(1, f.Name.IndexOf('>') - 1))
            .ToHashSet();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && backingFields.Contains(p.Name))
            .ToArray();

        if (properties.Length != (ast.Children?.Count ?? 0))
        {
            throw new FormatException();
        }

        // No property, nothing to do.
        if (ast.Children is null)
        {
            return;
        }

        HashSet<string> names = properties.Select(p => p.Name).ToHashSet();
        names.SymmetricExceptWith(ast.Children.Keys);
        if (names.Count is not 0)
        {
            throw new FormatException();
        }
        
        // 2. Fill these properties:
        foreach (PropertyInfo property in properties)
        {
            if (ast.Children?.TryGetValue(property.Name, out Node propertyNode) is not true)
            {
                throw new FormatException($"");
            }
            
            object? value = ConvertValue(propertyNode, property.PropertyType);
            property.SetValue(dest, value);
        }
    }

    private object? ConvertValue(Node node, Type type)
        => node.Type switch
        {
            ENodeType.Number => ConvertNumber(node, type),
            ENodeType.Bool => bool.Parse(node.Value),
            ENodeType.String => node.Value,
            ENodeType.Object => ConvertObject(node, type),
            ENodeType.Null => null,
            ENodeType.Identifier => ConvertIdentifier(node, type),
            _ => throw new FormatException()
        };

    private object ConvertNumber(Node node, Type type)
    {
        string value = node.Value;
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
    
    private object ConvertObject(Node node, Type type)
    {
        if (node.Value.Length > 0)
        {
            type = Context.GetType(node.Value, type);
        }
        
        object result = ObjectFactory(type);
        Convert(node, result);
        return result;
    }
    
    private object? ConvertIdentifier(Node node, Type type)
    {
        string identifier = node.Value;
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
    private static partial Regex _backingFieldRegex { get; }
    
}


