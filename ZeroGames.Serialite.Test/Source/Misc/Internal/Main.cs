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
        TestPerson testObject = new TestPersonSub();
        
        // Test deserialization (this will call the lexer and parser internally)
        string serialiteInput = "(Name=\"John\", Age=HAHAHA, Active=true)";
        Console.WriteLine($"Input: {serialiteInput}");
        
        testObject = serializer.Deserialize<TestPerson>(serialiteInput);
        
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
}

public class TestPersonSub : TestPerson;


