using Coordina.Api.Core.Modules;
using Coordina.Api.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace Coordina.Api.Tests.Integration.Architecture;

public sealed class ModuleRegistrationTests(ApiTestApplicationFactory factory)
  : ApiTest(factory)
{
  [Fact]
  public void EveryModule_IsRegisteredInProgram()
  {
    var declared = typeof(Program).Assembly
      .GetTypes()
      .Where(type => type is { IsClass: true, IsAbstract: false }
        && typeof(IModule).IsAssignableFrom(type))
      .Select(type => type.FullName)
      .Order(StringComparer.Ordinal)
      .ToArray();
    var registered = Factory.Services
      .GetServices<IModule>()
      .Select(module => module.GetType().FullName)
      .Order(StringComparer.Ordinal)
      .ToArray();

    Assert.NotEmpty(declared);
    Assert.Equal(declared, registered);
  }
}
