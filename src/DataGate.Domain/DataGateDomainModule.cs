using DataGate.Promotions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain;
using Volo.Abp.Modularity;

namespace DataGate;

[DependsOn(typeof(AbpDddDomainModule))]
public class DataGateDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<DelegationOfAuthorityPolicy>();
    }
}
