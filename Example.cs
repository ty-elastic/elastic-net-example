using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System.Text.Json;
using Elastic.Clients.Elasticsearch.Serialization;
using Newtonsoft.Json;
using System;
using System.Diagnostics;

public class Example
{
    private const string INDEX_NAME = "example";
    private ElasticsearchClient client;

    public async Task Connect()
    {
        var settings = new ElasticsearchClientSettings(
        // new CloudNodePool(
        //     Environment.GetEnvironmentVariable("ES_CLOUD_ID"),
        //     new ApiKey(Environment.GetEnvironmentVariable("ES_API_KEY"))),
            new SingleNodePool(new Uri($"https://{Environment.GetEnvironmentVariable("ES_USER")}:{Environment.GetEnvironmentVariable("ES_PASS")}@{Environment.GetEnvironmentVariable("ES_ENDPOINT")}")));
        // set default serialization to PascalCase (null) or camelCase (JsonNamingPolicy.CamelCase)
        settings.DefaultFieldNameInferrer(p => p);
        //settings.EnableDebugMode();

        client = new ElasticsearchClient(settings);
    }

    public async Task WriteDocs()
    {
        for (int i = 0; i < 4; i++)
        {
            // create a sample doc
            var subDoc = new SubDoc
            {
                ExampleInt = 4567 + i,
                ExampleKeyword1 = "keyword1",
                ExampleKeyword2 = "keyword2"
            };
            var document = new ExampleDocument
            {
                ExampleInt = 1234 + i,
                ExampleKeyword1 = "hamburger",
                ExampleKeyword2 = "hotdog",
                ExampleText = "Hello",
                ExampleWildcard = "ExampleWildcard",
                exampleSubDoc = subDoc,
                ExampleSubDocArray = new List<SubDoc> { subDoc, subDoc },
                ExampleSubDocArrayNested = new List<SubDoc> { subDoc, subDoc },
                ExampleArrayofArrays = new List<List<string>> { new List<string> { "ExampleText1", "ExampleText2" }, new List<string> { "ExampleText3", "ExampleText4" } },
            };
            // write it to ES
            var response = await client
                .IndexAsync(document, INDEX_NAME);
            if (response.IsSuccess())
                Console.WriteLine("indexed:" + response);
            else
                Console.WriteLine(response.DebugInformation);
        }
        var response2 = await client.Indices.RefreshAsync(INDEX_NAME);
        Console.WriteLine(response2.DebugInformation);
        if (response2.IsSuccess())
            Console.WriteLine("refreshed:" + response2);
    }

    public async Task SynonymSearch()
    {
        // query based on synonyms (see "indices.sh" for setup)
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Size(10)
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        .Match(f => f
                            .Field(ff => ff.ExampleText)
                            .Query("hi")
                        )
                    )
                )
            )
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            var doc = response.Documents.FirstOrDefault();
            Debug.Assert(doc != null);
            Console.WriteLine("synonym matched: " + Newtonsoft.Json.JsonConvert.SerializeObject(doc));
        }
    }

    public async Task MultiFieldSearch()
    {
        // search across multiple fields
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Query(q => q
                .MultiMatch(m => m
                    .Fields(new Field(nameof(ExampleDocument.ExampleKeyword1), 1)
                        .And(new Field(nameof(ExampleDocument.ExampleKeyword2), 1))
                    )
                    .Query("hotdog")
                    .Fuzziness(new Fuzziness("AUTO"))
                )
            )
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            var doc = response.Documents.FirstOrDefault();
            Debug.Assert(doc != null);
            Console.WriteLine("multi-field matched: " + Newtonsoft.Json.JsonConvert.SerializeObject(doc));
        }
    }

    public async Task Aggregations()
    {
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Size(0)
            .Query(q => q
                .Bool(b => b
                .Filter(f => f
                            .Term(f => f
                            .Field(ff => ff.exampleSubDoc.ExampleInt)
                                .Value(4567))
                    )
                )
            )
            .Aggregations(agg => agg
                    .Sum("int-total", sum => sum.Field(ff => ff.exampleSubDoc.ExampleInt))
            )
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            var agg = response.Aggregations.GetSum("int-total");
            Debug.Assert(agg != null);
            Console.WriteLine("agg result: " + agg.Value);
        }
    }

    public async Task ArrayOfArraysSearch()
    {
        // search across multiple fields
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Size(10)
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        .Match(f => f
                            .Field(ff => ff.ExampleArrayofArrays)
                            .Query("ExampleText3")
                        )
            )))
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            var doc = response.Documents.FirstOrDefault();
            Debug.Assert(doc != null);
            Console.WriteLine("array of arrays matched: " + Newtonsoft.Json.JsonConvert.SerializeObject(doc));
        }
    }

    public async Task WildcardSearch()
    {
        // search across multiple fields
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Size(10)
            .Query(q => q
                .Wildcard(b => b
                            .Field(ff => ff.ExampleWildcard)
                            .Value("Ex*")
                        )
            )
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            var doc = response.Documents.FirstOrDefault();
            Debug.Assert(doc != null);
            Console.WriteLine("wildcard matched: " + Newtonsoft.Json.JsonConvert.SerializeObject(doc));
        }
    }


    public async Task FilterOnSubDoc()
    {
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Size(10)
            .Query(q => q
                .Bool(b => b
                    .Must(m => m
                        .Match(f => f
                            .Field(ff => ff.ExampleKeyword1)
                            .Query("hamburger")
                        )
                    )
                .Filter(f => f
                    .Bool(b => b
                        .Must(m => m
                            .Term(f => f
                                .Field(ff => ff.exampleSubDoc.ExampleInt)
                                .Value(4567)
                            )
                        )
                    )
                )
                )
            )
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            var doc = response.Documents.FirstOrDefault();
            Debug.Assert(doc != null);
            Console.WriteLine("filter matched: " + Newtonsoft.Json.JsonConvert.SerializeObject(doc));
        }
    }

    public async Task<Elastic.Transport.StringResponse> RawSearch(string search)
    {
        var response = await client.Transport.RequestAsync<StringResponse>(Elastic.Transport.HttpMethod.GET, $"/{INDEX_NAME}/_search", PostData.String(search));
        return response;
    }

    public async Task RawWildcardSearch() {
        var search = """
        {
            "fields": ["ExampleKeyword1"],

            "query": {
                "wildcard": {
                    "ExampleWildcard": 
                    {
                        "value" : "Exa*"
                    }
                }
            },
            "size": 10
        }
        """;
        var response = await RawSearch(search);
        Console.WriteLine("raw matched: " + response);
    }

    public async Task NestedSearchAndSort1()
    {
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Size(10)
            .Query(q => q
                .Nested(n => n
                    .Path(p => p.ExampleSubDocArrayNested)
                    .Query(u => u
                        .Bool(b => b
                            .Must(m => m
                                .Match(f => f
                                    .Field(new Field(nameof(ExampleDocument.ExampleSubDocArrayNested) + "." + nameof(SubDoc.ExampleKeyword2)))
                                    .Query("keyword2")
                                )
                            )
                        )
                    )
                )
            )
            .Sort(new[]
            {
                SortOptions.Field(nameof(ExampleDocument.exampleSubDoc) + "." + nameof(SubDoc.ExampleInt), new FieldSort { Order = SortOrder.Desc })
            })
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            Debug.Assert(response.Documents.Count > 0);
            foreach (var doc in response.Documents)
                Console.WriteLine("filter matched: " + Newtonsoft.Json.JsonConvert.SerializeObject(doc));
        }
    }

    public async Task NestedSearchAndSort2()
    {
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Size(10)
            .Query(q => q
                .Nested(n => n
                    .Path(new Field(nameof(ExampleDocument.ExampleSubDocArrayNested)))
                    .Query(u => u
                        .Bool(b => b
                            .Must(m => m
                                .Match(f => f
                                    .Field(new Field(nameof(ExampleDocument.ExampleSubDocArrayNested) + "." + nameof(SubDoc.ExampleKeyword2)))
                                    .Query("keyword2")
                                )
                            )
                        )
                    )
                )
            )
            .Sort(new[]
            {
                SortOptions.Field(nameof(ExampleDocument.ExampleSubDocArrayNested) + "." + nameof(SubDoc.ExampleInt),
                    new FieldSort
                    {
                        Order = SortOrder.Desc,
                        Nested = new NestedSortValue
                        {
                            Path = new Field(nameof(ExampleDocument.ExampleSubDocArrayNested))
                        }
                    })
            })
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            Debug.Assert(response.Documents.Count > 0);
            foreach (var doc in response.Documents)
                Console.WriteLine("filter matched: " + Newtonsoft.Json.JsonConvert.SerializeObject(doc));
        }
    }
}

