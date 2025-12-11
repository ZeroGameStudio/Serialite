// Copyright Zero Games. All Rights Reserved.

using ZeroGames.Serialite;

Console.WriteLine("Testing Serialite Public Interface");

Serialiter serializer = new Serialiter { Context = new TestContext() };

// Test the public Deserialize method
TestDeserialize();

Console.WriteLine("All tests completed!");

void TestDeserialize()
{
    try
    {
        Console.WriteLine("\n--- Testing Deserialize Method ---");
        
        // Create a simple test class
        //TestPerson testObject = new TestPersonSub();
        
        // Test deserialization (this will call the lexer and parser internally)
        string serialiteInput = @"(Name=""John"", Age=HAHAHA, Active=true, Time=""1999-10-31T02:00:00"", IntList=[1,2,3], IntSet=[4,5,6,6,], IntMap={1:""df"",3:""4aaa"",})";
        Console.WriteLine($"Input: {serialiteInput}");
        
        var testObject = serializer.Deserialize<TestPerson>(serialiteInput);
        
        Console.WriteLine($"Deserialized: Type={testObject.GetType().Name} Name={testObject.Name}, Age={testObject.Age}, Active={testObject.Active}");
        Console.WriteLine("✅ Deserialize test completed");
    }
    catch (NotImplementedException)
    {
        Console.WriteLine("⚠️  Deserialize method not yet implemented (expected)");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
    }
}

public class TestContext : IContext
{
    public Type GetType(string identifier, Type baseType) => typeof(TestPersonSub);
    public object this[string identifier] => 10;
}

// Simple test class for deserialization
public class TestPerson
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool Active { get; set; }
    public DateTime Time { get; set; }
    public required IReadOnlyList<int32> IntList { get; set; }
    public required IReadOnlySet<int32> IntSet { get; set; }
    public required IReadOnlyDictionary<int32, string> IntMap { get; set; }
}

public class TestPersonSub : TestPerson;


