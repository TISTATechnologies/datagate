using DataGate;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseAutofac();

await builder.AddApplicationAsync<DataGateHttpApiHostModule>();

var app = builder.Build();
await app.InitializeApplicationAsync();
await app.RunAsync();
