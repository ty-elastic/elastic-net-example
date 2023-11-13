using Elastic.Apm.NetCoreAll;

namespace MyApplication
{
  public class Program
  {
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
            .UseAllElasticApm();

    public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();
  }

  public class Startup
    {
        Example example = new Example(); 

        public void ConfigureServices(IServiceCollection services)
        {
        }
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            app.UseRouting();
            app.UseEndpoints(endpoints => { 
                // curl -v http://127.0.0.1:5000/agg
                endpoints.MapGet("/agg", async context => { 
                    Console.WriteLine("GET");
                    var response = await example.AggregationsNoFluentWithTracing();
                    await context.Response.WriteAsync(response);
                }); 
            });

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
            await example.NoFluentSearch();
            await example.NestedSearch();
            await example.HighlightSearch();
        }
    }
}