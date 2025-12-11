// Copyright Zero Games. All Rights Reserved.

namespace ZeroGames.Serialite;

public partial class Serialiter
{

    public string Serialize(object source) => throw new NotImplementedException();

    public void Deserialize(string source, object dest)
    {
        Serialiter serialiter = SelectSerialiter(dest.GetType());
        serialiter.Convert(Parse(Tokenize(source)), dest);
    }

    public object Deserialize(string source, Type baseType)
        => ConvertObject(Parse(Tokenize(source)), baseType);

    public IContext Context { get; init; } = NullContext.Instance;
    public Func<Type, object> ObjectFactory { get; init; } = type => Activator.CreateInstance(type)!;
    public IReadOnlyDictionary<Type, Serialiter>? InnerSerialiters { get; init; }

    private class NullContext : IContext
    {
        public static NullContext Instance { get; } = new NullContext();
        private NullContext(){}
        
        #region IContext Implementations
        
        Type IContext.GetType(string identifier, Type baseType) => baseType;
        object? IContext.this[string identifier] => null;
        
        #endregion
    }
    
}

public static class SerialiterExtensions
{
    extension(Serialiter @this)
    {
        public T Deserialize<T>(string source) where T : notnull
            => (T)@this.Deserialize(source, typeof(T));
    
        public void Deserialize<T>(string source, T dest) where T : class
            => @this.Deserialize(source, dest);

        public void Deserialize<T>(string source, ref T dest) where T : struct
        {
            object boxed = dest;
            @this.Deserialize(source, boxed);
            dest = (T)boxed;
        }
    }
}


