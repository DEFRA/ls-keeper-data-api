using Microsoft.AspNetCore.Builder;

namespace LsKeeperDataApi.Test.Config;

public class EnvironmentTest
{

   [Fact]
   public void IsNotDevModeByDefault()
   { 
       var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
       var isDev = LsKeeperDataApi.Config.Environment.IsDevMode(builder);
       Assert.False(isDev);
   }
}
