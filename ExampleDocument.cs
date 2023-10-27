using System.Text.Json.Serialization;

public class SubDoc
{
    public int ExampleInt { get; set; }
    public string ExampleKeyword1 { get; set; }
    public string ExampleKeyword2 { get; set; }
}
public class Child
{
    [JsonPropertyName("name")]
    public string name { get; set; }
}

public class Parent
{
    [JsonPropertyName("parentId")]
    public string parentId { get; set; }
    [JsonPropertyName("children")]
    public List<Child> children { get; set; }
}

public class ExampleDocument
{
    public int ExampleInt { get; set; }
    public string ExampleKeyword1 { get; set; }
    public string ExampleKeyword2 { get; set; }
    public string ExampleText { get; set; }
    public string ExampleWildcard { get; set; }
    // by default, we've configured for PascalCase serialization; here we force camelCase
    [JsonPropertyName("exampleSubDoc")]
    public SubDoc exampleSubDoc { get; set; }
    public List<SubDoc> ExampleSubDocArray { get; set; }
    public List<SubDoc> ExampleSubDocArrayNested { get; set; }
    public List<List<string>> ExampleArrayofArrays { get; set; }
    public List<Parent> ExampleArrayOfNestedArrays { get; set; }
}