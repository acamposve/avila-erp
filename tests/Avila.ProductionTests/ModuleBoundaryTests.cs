using System.Reflection;
using Avila.Application;
using Avila.Modules.Administration;
using Avila.Modules.Billing;
using Avila.Modules.Inventory;
using Avila.Modules.Sales;

namespace Avila.ProductionTests;

public sealed class ModuleBoundaryTests
{
    [Theory]
    [InlineData(typeof(AdministrationModule), "Avila.Modules.Sales")]
    [InlineData(typeof(AdministrationModule), "Avila.Modules.Billing")]
    [InlineData(typeof(InventoryModule), "Avila.Modules.Sales")]
    [InlineData(typeof(SalesModule), "Avila.Modules.Administration")]
    [InlineData(typeof(SalesModule), "Avila.Modules.Billing")]
    [InlineData(typeof(BillingModule), "Avila.Modules.Sales")]
    [InlineData(typeof(SalesFlowService), "Avila.Modules.Administration")]
    [InlineData(typeof(SalesFlowService), "Avila.Modules.Billing")]
    public void Module_does_not_reference_another_module_implementation(Type type, string forbiddenAssembly)
    {
        var references = type.Assembly.GetReferencedAssemblies()
            .Select(name => name.Name)
            .ToArray();

        Assert.DoesNotContain(forbiddenAssembly, references);
    }
}
