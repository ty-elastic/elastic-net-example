using System.Text.Json.Serialization;

public class SubDoc
{
    public int ExampleInt { get; set; }
    public string ExampleKeyword1 { get; set; }
    public string ExampleKeyword2 { get; set; }
}
public class Child
{
    public string name { get; set; }
}

public class Parent
{
    public string parentId { get; set; }
    public List<Child> children { get; set; }
}

public class ExampleDocument
{
    public int ExampleInt { get; set; }
    public string ExampleKeyword1 { get; set; }
    public string ExampleKeyword2 { get; set; }
    public string ExampleText { get; set; }
    public string ExampleWildcard { get; set; }
    public SubDoc exampleSubDoc { get; set; }
    public List<SubDoc> ExampleSubDocArray { get; set; }
    public List<SubDoc> ExampleSubDocArrayNested { get; set; }
    public List<List<string>> ExampleArrayofArrays { get; set; }
    public List<Parent> ExampleArrayOfNestedArrays { get; set; }
}