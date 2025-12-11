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
        List,
        Map,
        Pair,
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
        enumerator.MoveNext();
        Node result = ParseValue(string.Empty, enumerator, out var lastToken);
        if (lastToken.Type is not ETokenType.EndOfInput)
        {
            throw new FormatException();
        }
        return result;
    }

    private Node ParseValue(string name, IEnumerator<Token> enumerator, out Token lastToken)
    {
        string value = enumerator.Current.Value;
        bool anonymous = true;
        ENodeType type = enumerator.Current.Type switch
        {
            ETokenType.Integer or ETokenType.Float => ENodeType.Number,
            ETokenType.Bool => ENodeType.Bool,
            ETokenType.String => ENodeType.String,
            ETokenType.LeftParen => ENodeType.Object,
            ETokenType.LeftBracket => ENodeType.List,
            ETokenType.LeftBrace => ENodeType.Map,
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

        return type switch
        {
            ENodeType.Object => ParseObject(name, anonymous ? string.Empty : value, enumerator, out lastToken),
            ENodeType.List => ParseList(name, enumerator, out lastToken),
            ENodeType.Map => ParseMap(name, enumerator, out lastToken),
            _ => new() { Type = type, Name = name, Value = value }
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
                enumerator.MoveNext();
                current = enumerator.Current;
                
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

    private Node ParseList(string name, IEnumerator<Token> enumerator, out Token lastToken)
    {
        Token current = enumerator.Current;
        if (current.Type is ETokenType.LeftBracket)
        {
            enumerator.MoveNext();
            current = enumerator.Current;
        }
        
        Dictionary<string, Node> children = [];
        int32 index = 0;
        while (true)
        {
            // Eat comma.
            if (children.Count > 0 && current.Type is ETokenType.Comma)
            {
                enumerator.MoveNext();
                current = enumerator.Current;
            }
            
            // [Value, Value,] and [Value,] are both legal.
            if (current.Type is ETokenType.RightBracket)
            {
                break;
            }
            else
            {
                string indexKey = index.ToString();
                children[indexKey] = ParseValue(indexKey, enumerator, out current);
                ++index;
            }
        }

        enumerator.MoveNext();
        lastToken = enumerator.Current;
        
        return new()
        {
            Type = ENodeType.List,
            Name = name,
            Value = string.Empty,
            Children = children,
        };
    }
    
    private Node ParseMap(string name, IEnumerator<Token> enumerator, out Token lastToken)
    {
        Token current = enumerator.Current;
        if (current.Type is ETokenType.LeftBrace)
        {
            enumerator.MoveNext();
            current = enumerator.Current;
        }
        
        Dictionary<string, Node> children = [];
        while (true)
        {
            // Eat comma.
            if (children.Count > 0 && current.Type is ETokenType.Comma)
            {
                enumerator.MoveNext();
                current = enumerator.Current;
            }
            
            // {Key:Value, Key:Value,} and {Key:Value,} are both legal.
            if (current.Type is ETokenType.RightBrace)
            {
                break;
            }
            // Bool and identifier is not supported for map key.
            else if (current.Type is ETokenType.String or ETokenType.Integer or ETokenType.Float)
            {
                Node key = ParseValue(string.Empty, enumerator, out current);
                
                // Eat colon.
                if (current.Type is not ETokenType.Colon)
                {
                    throw new FormatException();
                }
                enumerator.MoveNext();
                current = enumerator.Current;

                Node value = ParseValue(string.Empty, enumerator, out current);

                children[key.Value] = new Node
                {
                    Type = ENodeType.Pair,
                    Name = string.Empty,
                    Children = new Dictionary<string, Node> { ["Key"] = key, ["Value"] = value },
                    Value = string.Empty,
                };
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
            Type = ENodeType.Map,
            Name = name,
            Value = string.Empty,
            Children = children,
        };
    }

}


