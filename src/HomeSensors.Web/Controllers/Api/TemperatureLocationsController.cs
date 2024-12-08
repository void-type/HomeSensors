using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using HomeSensors.Model.Services.Temperature.Alert;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/temperatures-locations")]
public class TemperatureLocationsController : ControllerBase
{
    private readonly TemperatureLocationRepository _locationRepository;
    private readonly TemperatureAlertsSettings _alertSettings;

    public TemperatureLocationsController(TemperatureLocationRepository locationRepository,
        TemperatureAlertsSettings alertSettings)
    {
        _locationRepository = locationRepository;
        _alertSettings = alertSettings;
    }

    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(List<TemperatureLocationResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public Task<IActionResult> GetAll()
    {
        return _locationRepository.GetAll()
            .MapAsync(HttpResponder.Respond);
    }

    [HttpGet]
    [Route("check-limits")]
    [ProducesResponseType(typeof(List<TemperatureCheckLimitResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public Task<IActionResult> CheckLimits(DateTimeOffset lastCheck)
    {
        return _locationRepository.CheckLimits(lastCheck, _alertSettings.AverageIntervalMinutes)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public Task<IActionResult> Save([FromBody] TemperatureLocationSaveRequest request)
    {
        return _locationRepository.Save(request)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public Task<IActionResult> Delete(int id)
    {
        return _locationRepository.Delete(id)
            .MapAsync(HttpResponder.Respond);
    }
}
