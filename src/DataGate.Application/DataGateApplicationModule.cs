using DataGate.EntityFrameworkCore;
using DataGate.Promotions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace DataGate;

[DependsOn(
    typeof(DataGateDomainModule),
    typeof(DataGateEntityFrameworkCoreModule),
    typeof(AbpDddApplicationModule))]
public class DataGateApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClient<IElsaWorkflowSignaler, ElsaWorkflowSignaler>();
    }
}
