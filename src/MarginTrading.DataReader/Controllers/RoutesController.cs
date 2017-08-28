using System.Collections.Generic;
using System.Threading.Tasks;
using MarginTrading.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/routes")]
    public class RoutesController : Controller
    {
        private readonly IMatchingEngineRoutesRepository _routesRepository;

        public RoutesController(IMatchingEngineRoutesRepository routesRepository)
        {
            _routesRepository = routesRepository;
        }

        [HttpGet]
        [Route("")]
        public Task<IEnumerable<IMatchingEngineRoute>> GetAll()
        {
            return _routesRepository.GetAllRoutesAsync();
        }

        [HttpGet]
        [Route("{id}")]
        public Task<IMatchingEngineRoute> GetById(string id)
        {
            return _routesRepository.GetRouteByIdAsync(id);
        }
    }
}