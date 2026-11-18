using System.Reflection;
using AbcPharmacy.Api.Data;
using AbcPharmacy.Api.Infrastructure;
using AbcPharmacy.Api.Models;
using AbcPharmacy.Api.Options;
using AbcPharmacy.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<InventoryOptions>(builder.Configuration.GetSection(InventoryOptions.SectionName));
builder.Services.Configure<DataStoreOptions>(builder.Configuration.GetSection(DataStoreOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);


builder.Services.AddSingleton(sp => new JsonFileStore<Medicine>(ResolveDataFile(sp, "medicines.json")));
builder.Services.AddSingleton(sp => new JsonFileStore<Sale>(ResolveDataFile(sp, "sales.json")));
builder.Services.AddSingleton<IMedicineRepository, JsonMedicineRepository>();
builder.Services.AddSingleton<ISaleRepository, JsonSaleRepository>();
builder.Services.AddSingleton<DataSeeder>();

builder.Services.AddSingleton<InventoryRules>();
builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<ISaleService, SaleService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlFile))
    {
        c.IncludeXmlComments(xmlFile);
    }
});

var app = builder.Build();

await app.Services.GetRequiredService<DataSeeder>().SeedAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();

static string ResolveDataFile(IServiceProvider services, string fileName)
{
    var options = services.GetRequiredService<IOptions<DataStoreOptions>>().Value;
    var environment = services.GetRequiredService<IWebHostEnvironment>();
    var directory = Path.IsPathRooted(options.DataDirectory)
        ? options.DataDirectory
        : Path.Combine(environment.ContentRootPath, options.DataDirectory);
    return Path.Combine(directory, fileName);
}

public partial class Program;
