
using Abp.Web.Mvc.Controllers;
using Microsoft.AspNet.Identity;
using System.Web.Mvc;

namespace AbpAspNetMvcDemo.Controllers
{
    public abstract class DemoControllerBase : AbpController
    {
        protected DemoControllerBase()
        {
            LocalizationSourceName = "AbpAspNetMvcDemoModule";
        }

    }
}