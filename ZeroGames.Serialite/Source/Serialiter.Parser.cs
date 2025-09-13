// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Serialite;

partial class Serialiter
{

    private enum ENodeType
    {
        Number,
        Bool,
        String,
        Object,
        Null,
        Identifier,
    }

    private readonly struct Node
    {
        public required ENodeType Type { get; init; }
        public required string Name { get; init; }
        public required string Value { get; init; }
        public Dictionary<string, Node>? Children { get; init; }
    }

    private Node Parse(IEnumerable<Token> tokens)
    {
        using IEnumerator<Token> enumerator = tokens.GetEnumerator();
        Node result = ParseValue(string.Empty, enumerator, out var lastToken);
        if (lastToken.Type is not ETokenType.EndOfInput)
        {
            throw new FormatException();
        }
        return result;
    }

    private Node ParseValue(string name, IEnumerator<Token> enumerator, out Token lastToken)
    {
        enumerator.MoveNext();
        if (name.Length is 0 && enumerator.Current.Type is not (ETokenType.LeftParen or ETokenType.Identifier or ETokenType.IdentifierPath))
        {
            throw new FormatException();
        }
        
        string value = enumerator.Current.Value;
        bool anonymous = true;
        ENodeType type = enumerator.Current.Type switch
        {
            ETokenType.Integer or ETokenType.Float => ENodeType.Number,
            ETokenType.Bool => ENodeType.Bool,
            ETokenType.String => ENodeType.String,
            ETokenType.LeftParen => ENodeType.Object,
            ETokenType.Identifier or ETokenType.IdentifierPath => ENodeType.Identifier,
            ETokenType.Null => ENodeType.Null,
            _ => throw new FormatException()
        };
        
        enumerator.MoveNext();
        lastToken = enumerator.Current;
        
        if (type is ENodeType.Identifier && lastToken.Type is ETokenType.LeftParen)
        {
            type = ENodeType.Object;
            anonymous = false;
        }

        if (type is ENodeType.Object)
        {
            return ParseObject(name, anonymous ? string.Empty : value, enumerator, out lastToken);
        }

        return new()
        {
            Type = type,
            Name = name,
            Value = value,
        };
    }

    private Node ParseObject(string name, string type, IEnumerator<Token> enumerator, out Token lastToken)
    {
        Token current = enumerator.Current;
        if (current.Type is ETokenType.LeftParen)
        {
            enumerator.MoveNext();
            current = enumerator.Current;
        }
        
        string value = type;
        Dictionary<string, Node> children = [];
        while (true)
        {
            // Eat comma.
            if (children.Count > 0 && current.Type is ETokenType.Comma)
            {
                enumerator.MoveNext();
                current = enumerator.Current;
            }
            
            // Type(Name=Value) and Type(Name=Value,) are both legal.
            if (current.Type is ETokenType.RightParen)
            {
                break;
            }
            // Prop is a legal name but Prop.Sub isn't.
            else if (current.Type is ETokenType.Identifier)
            {
                string propertyName = current.Value;
                
                // Eat equals.
                enumerator.MoveNext();
                current = enumerator.Current;
                if (current.Type is not ETokenType.Equals)
                {
                    throw new FormatException();
                }

                children[propertyName] = ParseValue(propertyName, enumerator, out current);
            }
            else
            {
                throw new FormatException();
            }
        }

        enumerator.MoveNext();
        lastToken = enumerator.Current;
        
        return new()
        {
            Type = ENodeType.Object,
            Name = name,
            Value = value,
            Children = children,
        };
    }

}


