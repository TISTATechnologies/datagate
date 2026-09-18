using DataGate.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace DataGate;

[DependsOn(
    typeof(DataGateApplicationModule),
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule))]
public class DataGateHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(DataGateApplicationModule).Assembly);
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
            app.UseDeveloperExceptionPage();

        app.UseRouting();
        app.UseStaticFiles();
        app.UseConfiguredEndpoints(endpoints =>
        {
            endpoints.MapGet("/", async http =>
            {
                http.Response.ContentType = "text/html; charset=utf-8";
                await http.Response.SendFileAsync(Path.Combine(env.WebRootPath, "index.html"));
            });
        });

        using var scope = context.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataGateDbContext>();
        db.Database.EnsureCreated();
        // ponytail: EnsureCreated won't alter existing tables — add steward evidence columns if missing
        db.Database.ExecuteSqlRaw("""
            IF COL_LENGTH('PromotionRequests', 'RowCount') IS NULL
            BEGIN
              ALTER TABLE PromotionRequests ADD [RowCount] bigint NOT NULL CONSTRAINT DF_PromotionRequests_RowCount DEFAULT 0;
              ALTER TABLE PromotionRequests ADD RowCountDriftPct decimal(18,2) NOT NULL CONSTRAINT DF_PromotionRequests_Drift DEFAULT 0;
              ALTER TABLE PromotionRequests ADD NullRatePct decimal(18,2) NOT NULL CONSTRAINT DF_PromotionRequests_NullRate DEFAULT 0;
              ALTER TABLE PromotionRequests ADD NewColumns nvarchar(1000) NOT NULL CONSTRAINT DF_PromotionRequests_NewColumns DEFAULT '';
              ALTER TABLE PromotionRequests ADD ContainsSensitiveData bit NOT NULL CONSTRAINT DF_PromotionRequests_Sensitive DEFAULT 0;
              ALTER TABLE PromotionRequests ADD IsRegulatoryReporting bit NOT NULL CONSTRAINT DF_PromotionRequests_Regulatory DEFAULT 0;
            END
            """);
    }
}
