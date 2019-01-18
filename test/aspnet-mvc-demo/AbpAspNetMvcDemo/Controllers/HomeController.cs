using Abp.DataAccess;
using Abp.Dependency;
using System.Web.Mvc;

namespace AbpAspNetMvcDemo.Controllers
{
    public class HomeController : DemoControllerBase
    {
        public ActionResult Index()
        {
            IDataCommand command = IocManager.Instance.Resolve<IDataCommand>();
            command = command.CreateCommand("GetAllChannelList");
            var list = command.ExecuteEntityList<Channel>();


            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}