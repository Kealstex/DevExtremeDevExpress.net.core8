using System.Data;
using AspNetCoreCustomItemGallery.Models;
using DevExpress.AspNetCore;
using DevExpress.DashboardAspNetCore;
using DevExpress.DashboardCommon;
using DevExpress.DashboardWeb;
using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Excel;
using DevExpress.DataAccess.Sql;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var fileProvider = builder.Environment.ContentRootFileProvider;
var configuration = builder.Configuration;
// Add services to the container.
builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddRazorPages();

builder.Services.AddDevExpressControls();
builder.Services.AddScoped<DashboardConfigurator>((IServiceProvider serviceProvider) =>
{
    DashboardConfigurator configurator = new DashboardConfigurator();
    configurator.SetConnectionStringsProvider(new DashboardConnectionStringsProvider(configuration));
    DashboardFileStorage dashboardFileStorage =
        new DashboardFileStorage(fileProvider.GetFileInfo("App_Data/Dashboards").PhysicalPath);
    configurator.SetDashboardStorage(dashboardFileStorage);
    DataSourceInMemoryStorage dataSourceStorage = new DataSourceInMemoryStorage();

    DashboardSqlDataSource sqlDataSource = new DashboardSqlDataSource("Sales Person", "NWindConnectionString");
    sqlDataSource.DataProcessingMode = DataProcessingMode.Client;
    SelectQuery query = SelectQueryFluentBuilder
        .AddTable("Categories")
        .Join("Products", "CategoryID")
        .SelectAllColumns()
        .Build("Products_Categories");
    sqlDataSource.Queries.Add(query);
    dataSourceStorage.RegisterDataSource("sqlDataSource", sqlDataSource.SaveToXml());

    DashboardExcelDataSource energyStatistics = new DashboardExcelDataSource("Energy Statistics");
    energyStatistics.ConnectionName = "energyStatisticsDataConnection";
    energyStatistics.SourceOptions = new ExcelSourceOptions(new ExcelWorksheetSettings("Map Data"));
    dataSourceStorage.RegisterDataSource("energyStatisticsDataSource", energyStatistics.SaveToXml());

    configurator.DataLoading += Configurator_DataLoading;
    configurator.ConfigureDataConnection += Configurator_ConfigureDataConnection;
    configurator.SetDataSourceStorage(dataSourceStorage);

    return configurator;
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseDevExpressControls();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapDashboardRoute("api/dashboard", "DefaultDashboard");
    endpoints.MapRazorPages();
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
});

app.Run();

void Configurator_ConfigureDataConnection(object sender, ConfigureDataConnectionWebEventArgs e)
{
    if (e.ConnectionName == "localhost_Connection")
    {
        e.ConnectionParameters = new XmlFileConnectionParameters()
        {
            FileName = fileProvider.GetFileInfo("App_Data/Departments.xml").PhysicalPath
        };
    }

    if (e.ConnectionName == "energyStatisticsDataConnection")
    {
        e.ConnectionParameters =
            new ExcelDataSourceConnectionParameters(fileProvider.GetFileInfo("App_Data/EnergyStatistics.xls")
                .PhysicalPath);
    }
}

void Configurator_DataLoading(object sender, DataLoadingWebEventArgs e)
{
    if (e.DataId == "odsTaskData")
    {
        DataSet dataSet = new DataSet();
        dataSet.ReadXml(fileProvider.GetFileInfo("App_Data/GanttData.xml").PhysicalPath);
        e.Data = dataSet.Tables[0];
    }
}
