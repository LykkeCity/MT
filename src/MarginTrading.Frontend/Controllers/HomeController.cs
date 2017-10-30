using MarginTrading.Common.Documentation;
using MarginTrading.Frontend.Models;
using MarginTrading.Frontend.Wamp;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Frontend.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        [Route("")]
        [HttpGet]
        public IActionResult Index()
        {
            var doc = new TypeDocGenerator();
            var model = new MethodInfoModel
            {
                Rpc = doc.GetDocumentation(typeof(IRpcMtFrontend)),
                Topic = doc.GetDocumentation(typeof(IWampTopic))
            };

            return View(model);
        }
    }
}
