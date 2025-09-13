// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Serialite;

public interface IContext
{
    Type GetType(string identifier, Type baseType);
    object? this[string identifier] { get; }
}


