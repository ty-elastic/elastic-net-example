using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System.Text.Json;
using Elastic.Clients.Elasticsearch.Serialization;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Core.Search;
using System.Text.RegularExpressions;
using Elastic.Clients.Elasticsearch.Core.TermVectors;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Apm.Api;

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
        // set default serialization to use actual class names
        settings.DefaultFieldNameInferrer(p => p);
        settings.EnableDebugMode();

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
                ExampleText = "Hello my name is Bob",
                ExampleSynonym = "Hello",
                ExampleWildcard = "ExampleWildcard",
                exampleSubDoc = subDoc,
                ExampleSubDocArray = new List<SubDoc> { subDoc, subDoc },
                ExampleSubDocArrayNested = new List<SubDoc> { subDoc, subDoc },
                ExampleArrayofArrays = new List<List<string>> { new List<string> { "ExampleText1", "ExampleText2" }, new List<string> { "ExampleText3", "ExampleText4" } },
                ExampleArrayOfNestedArrays = new List<Parent> { 
                    new Parent{parentId="abc", children= new List<Child>{new Child{name="Michael"}, new Child{name= "Scott"}, new Child{name= "Chris"}}},
                    new Parent{parentId="xxx", children= new List<Child>{new Child{name="Jack"}, new Child{name= "Jen"}, new Child{name= "Jeff"}}}
                }
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
                            .Field(ff => ff.ExampleSynonym)
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

    public async Task NoFluentSearch()
    {
        var boolQuery = new BoolQuery
        {
            Must = new Query[]
                {
                    new MatchQuery(Infer.Field<ExampleDocument>(p => p.ExampleSynonym)) { Query = "hi" }
                }
        };

        // query based on synonyms (see "indices.sh" for setup)
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Size(10)
            .Query(boolQuery)
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            var doc = response.Documents.FirstOrDefault();
            Debug.Assert(doc != null);
            Console.WriteLine("matched: " + Newtonsoft.Json.JsonConvert.SerializeObject(doc));
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

    public async Task<string> AggregationsNoFluentWithTracing()
    {
        var transaction = Elastic.Apm.Agent.Tracer.CurrentTransaction;
        ISpan span = transaction.StartSpan("SumAggregation",
            ApiConstants.TypeDb, ApiConstants.SubtypeElasticsearch, ApiConstants.ActionQuery);

        try {
            var boolFilter = new BoolQuery
            {
                Filter = new Query[]
                    {
                        new TermQuery(Infer.Field<ExampleDocument>(p => p.exampleSubDoc.ExampleInt)) { Value = 4567 }
                    }
            };

            var sumAggregation = new SumAggregation("int-total", Infer.Field<ExampleDocument>(p => p.exampleSubDoc.ExampleInt));

            var response = await client.SearchAsync<ExampleDocument>(s => s
                .Index(INDEX_NAME)
                .Size(0)
                .Query(boolFilter)
                .Aggregations(sumAggregation)
            );
            
            // for demo purposes, always log DSL query to trace
            span.CaptureErrorLog(new ErrorLog(response.DebugInformation));
            if (response.IsSuccess())
            {
                var agg = response.Aggregations.GetSum("int-total");
                Debug.Assert(agg != null);
                Console.WriteLine("agg result: " + agg.Value);
                return "agg result: " + agg.Value;
            } else {
                // normally, log the DSL only on error
                span.CaptureErrorLog(new ErrorLog(response.DebugInformation));
            }
        }
        catch (Exception e)
        {
            span.CaptureException(e);
            throw;
        }
        finally
        {
            span.End();
        }
        return "error";
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

    public async Task NestedSearch()
    {
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Size(10)
            .Query(q => q
                .Nested(n => n
                    .Path(p => p.ExampleArrayOfNestedArrays)
                    .Query(u => u
                        .Nested(nn => nn
                            .Path(nameof(ExampleDocument.ExampleArrayOfNestedArrays) + "." + nameof(Parent.children))
                            .Query(uu => uu
                                .Bool(b => b
                                     .Must(m => m
                                        .Match(f => f
                                            .Field(nameof(ExampleDocument.ExampleArrayOfNestedArrays) + "." + nameof(Parent.children) + "." + nameof(Child.name))
                                            .Query("Jack")
                                        )
                                        .Match(f => f
                                            .Field(nameof(ExampleDocument.ExampleArrayOfNestedArrays) + "." + nameof(Parent.children) + "." + nameof(Child.name))
                                            .Query("Jeff")
                                        )
                                     )
                                )
                            )
                        )
                    )
                    .InnerHits(new Elastic.Clients.Elasticsearch.Core.Search.InnerHits())
                )
            )
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            Debug.Assert(response.Documents.Count > 0);
            foreach (var doc in response.Documents)
                Console.WriteLine("filter matched: " + Newtonsoft.Json.JsonConvert.SerializeObject(doc));
        }
    }

    public async Task HighlightSearch()
    {
        // search across multiple fields
        var response = await client.SearchAsync<ExampleDocument>(s => s
            .Index(INDEX_NAME)
            .Query(q => q
                        .Match(f => f
                            .Field(ff => ff.ExampleText)
                            .Query("name")
                        )
            )
            .Fields(fs => fs
                .Field(f => f.ExampleText)
            )
            .Highlight(h => h
                .PreTags(new List<string> {"<tag1>"})
                .PostTags(new List<string> {"</tag1>"})
                .Encoder(HighlighterEncoder.Html)
                .Fields(fs => fs
                    .Add(new Field(nameof(ExampleDocument.ExampleText)), new HighlightField())
                )
            )
        );
        Console.WriteLine(response.DebugInformation);
        if (response.IsSuccess())
        {
            Debug.Assert(response.Hits.Select(d => d.Highlight).Count() > 0);
            foreach (var highlightsInEachHit in response.Hits.Select(d => d.Highlight))
            {
                foreach (var highlightField in highlightsInEachHit)
                {
                    Console.WriteLine("matched w/ highlight: " + Newtonsoft.Json.JsonConvert.SerializeObject(highlightField));
                }
            }
        }
    }
}

