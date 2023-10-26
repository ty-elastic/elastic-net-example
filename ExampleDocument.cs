using System.Text.Json.Serialization;

public class SubDoc
{
    public int ExampleInt { get; set; }
    public string ExampleKeyword1 { get; set; }
    public string ExampleKeyword2 { get; set; }
}

public class ExampleDocument
{
    public int ExampleInt { get; set; }
    public string ExampleKeyword1 { get; set; }
    public string ExampleKeyword2 { get; set; }
    public string ExampleText { get; set; }
    public string ExampleWildcard { get; set; }
    // by setting "configureOptions = o => o.PropertyNamingPolicy = null", properties will be serialized in PascalCase, unless otherwise noted
    [JsonPropertyName("exampleSubDoc")]
    public SubDoc exampleSubDoc { get; set; }
    public List<SubDoc> ExampleSubDocArray { get; set; }
    public List<SubDoc> ExampleSubDocArrayNested { get; set; }
    public List<List<string>> ExampleArrayofArrays { get; set; }
}