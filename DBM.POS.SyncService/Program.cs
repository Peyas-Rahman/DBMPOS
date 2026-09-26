using DBM.POS.Infrastructure.Data;
using DBM.POS.SyncService;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "DBM POS Sync Service");
builder.Services.AddDbContext<POSDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient();
builder.Services.AddHostedService<Worker>();
var host = builder.Build();
await host.RunAsync();
