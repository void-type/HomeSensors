using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using HomeSensors.Model.Services.Temperature.Poll;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/temperatures-devices")]
public class TemperatureDevicesController : ControllerBase
{
    private readonly TemperatureDeviceRepository _deviceRepository;
    private readonly IHttpContextAccessor _contextAccessor;

    public TemperatureDevicesController(TemperatureDeviceRepository deviceRepository, IHttpContextAccessor contextAccessor)
    {
        _deviceRepository = deviceRepository;
        _contextAccessor = contextAccessor;
    }

    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(List<TemperatureDeviceResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public Task<IActionResult> GetAll()
    {
        return _deviceRepository.GetAll()
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public Task<IActionResult> Save([FromBody] TemperatureDeviceSaveRequest request)
    {
        return _deviceRepository.Save(request)
            .TeeAsync(RefreshTopicSubscriptions)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public Task<IActionResult> Delete(int id)
    {
        return _deviceRepository.Delete(id)
            .TeeAsync(RefreshTopicSubscriptions)
            .MapAsync(HttpResponder.Respond);
    }

    private async Task RefreshTopicSubscriptions()
    {
        var worker = _contextAccessor?
            .HttpContext?
            .RequestServices?
            .GetServices<IHostedService>()?
            .OfType<MqttTemperaturesWorker>()
            .FirstOrDefault();

        if (worker is not null)
        {
            await worker.RefreshTopicSubscriptions();
        }
    }
}
