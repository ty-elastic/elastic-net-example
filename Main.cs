Example example = new Example();

await example.Connect();
await example.WriteDocs();

await example.SynonymSearch();
await example.MultiFieldSearch();
await example.NestedSearchAndSort1();
await example.NestedSearchAndSort2();
await example.ArrayOfArraysSearch();
await example.WildcardSearch();

await example.Aggregations();

await example.RawWildcardSearch();