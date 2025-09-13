// Copyright Zero Games. All Rights Reserved.

using System.Text;

namespace ZeroGames.Serialite;

partial class Serialiter
{
    
    private enum ETokenType
    {
        Integer,
        Float,
        Bool,
        String,
        Null,
        Identifier,
        IdentifierPath,
        LeftParen,
        RightParen,
        Equals,
        Comma,
        EndOfInput
    }

    private readonly record struct Token(ETokenType Type, string Value);

    private IEnumerable<Token> Tokenize(string source)
    {
        int32 i = 0;
        while (i < source.Length)
        {
            char current = source[i];
            
            // Skip whitespace
            if (char.IsWhiteSpace(current))
            {
                ++i;
                continue;
            }
            
            // Single character tokens
            switch (current)
            {
                case '(':
                {
                    yield return new Token(ETokenType.LeftParen, "(");
                    ++i;
                    break;
                }
                case ')':
                {
                    yield return new Token(ETokenType.RightParen, ")");
                    ++i;
                    break;
                }
                case '=':
                {
                    yield return new Token(ETokenType.Equals, "=");
                    ++i;
                    break;
                }
                case ',':
                {
                    yield return new Token(ETokenType.Comma, ",");
                    ++i;
                    break;
                }
                case '"':
                {
                    // String literal
                    ++i; // Skip opening quote
                    StringBuilder stringBuilder = new();
                    
                    while (i < source.Length && source[i] != '"')
                    {
                        if (source[i] is '\\' && i + 1 < source.Length)
                        {
                            ++i; // Skip backslash
                            char escapedChar = ProcessEscapeSequence(source, ref i);
                            stringBuilder.Append(escapedChar);
                        }
                        else
                        {
                            stringBuilder.Append(source[i]);
                            ++i;
                        }
                    }
                    
                    if (i < source.Length)
                    {
                        ++i; // Skip closing quote
                        yield return new Token(ETokenType.String, stringBuilder.ToString());
                    }
                    break;
                }
                default:
                {
                    // Handle identifiers, numbers, booleans, null
                    if (char.IsLetter(current) || current is '_')
                    {
                        // Identifier or keyword
                        int32 start = i;
                        bool split = false;
                        bool path = false;
                        while (i < source.Length && (char.IsLetterOrDigit(source[i]) || source[i] is '_' or '.'))
                        {
                            if (source[i] is '.')
                            {
                                split = true;
                                path = true;
                            }

                            if (split)
                            {
                                split = false;
                                if (char.IsDigit(source[i]) || source[i] is '.')
                                {
                                    throw new FormatException();
                                }
                            }
                            
                            ++i;
                        }
                        string identifier = source.Substring(start, i - start);
                        
                        // Check for keywords
                        switch (identifier.ToLower())
                        {
                            case "true":
                            case "false":
                            {
                                yield return new Token(ETokenType.Bool, identifier);
                                break;
                            }
                            case "null":
                            {
                                yield return new Token(ETokenType.Null, identifier);
                                break;
                            }
                            default:
                            {
                                yield return new Token(path ? ETokenType.IdentifierPath : ETokenType.Identifier, identifier);
                                break;
                            }
                        }
                    }
                    else if (char.IsDigit(current) || current is '-' or '+')
                    {
                        // Number
                        int32 start = i;
                        bool hasDecimal = false;
                        bool hasExponent = false;
                        
                        if (current is '-' or '+')
                        {
                            ++i;
                        }
                        
                        while (i < source.Length)
                        {
                            char c = source[i];
                            if (char.IsDigit(c))
                            {
                                ++i;
                            }
                            else if (c is '.' && !hasDecimal && !hasExponent)
                            {
                                hasDecimal = true;
                                ++i;
                            }
                            else if ((c is 'e' or 'E') && !hasExponent)
                            {
                                hasExponent = true;
                                ++i;
                                if (i < source.Length && source[i] is '+' or '-')
                                {
                                    ++i;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        
                        string numberStr = source.Substring(start, i - start);
                        ETokenType tokenType = hasDecimal || hasExponent ? ETokenType.Float : ETokenType.Integer;
                        yield return new Token(tokenType, numberStr);
                    }
                    else
                    {
                        throw new FormatException();
                    }
                    break;
                }
            }
        }
        
        yield return new Token(ETokenType.EndOfInput, string.Empty);
    }
    
    private char ProcessEscapeSequence(string source, ref int32 i)
    {
        if (i >= source.Length)
        {
            throw new FormatException("Invalid escape sequence: incomplete escape at end of string");
        }
            
        char escapeChar = source[i];
        ++i;
        
        switch (escapeChar)
        {
            case '"': return '"';
            case '\\': return '\\';
            case 'n': return '\n';
            case 'r': return '\r';
            case 't': return '\t';
            case 'b': return '\b';
            case 'f': return '\f';
            case 'v': return '\v';
            case '0': return '\0';
            case 'x':
                // Hex escape: \x41
                if (i + 1 < source.Length &&
                    IsHexDigit(source[i]) && IsHexDigit(source[i + 1]))
                {
                    string hexStr = source.Substring(i, 2);
                    i += 2;
                    return (char)System.Convert.ToInt32(hexStr, 16);
                }
                throw new FormatException($"Invalid hex escape sequence: \\x{source.Substring(i, Math.Min(2, source.Length - i))}");
            case 'u':
                // Unicode escape: \u0041
                if (i + 3 < source.Length &&
                    IsHexDigit(source[i]) && IsHexDigit(source[i + 1]) &&
                    IsHexDigit(source[i + 2]) && IsHexDigit(source[i + 3]))
                {
                    string hexStr = source.Substring(i, 4);
                    i += 4;
                    return (char)System.Convert.ToInt32(hexStr, 16);
                }
                throw new FormatException($"Invalid unicode escape sequence: \\u{source.Substring(i, Math.Min(4, source.Length - i))}");
            default:
                throw new FormatException($"Invalid escape sequence: \\{escapeChar}");
        }
    }
    
    private bool IsHexDigit(char c)
    {
        return char.IsDigit(c) || c is >= 'a' and <= 'f' || c is >= 'A' and <= 'F';
    }
    
}


