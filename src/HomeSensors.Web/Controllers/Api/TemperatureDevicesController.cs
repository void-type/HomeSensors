using HomeSensors.Model.Repositories;
using HomeSensors.Model.Repositories.Models;
using HomeSensors.Model.Workers;
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
    public async Task<IActionResult> GetAll()
    {
        var readings = await _deviceRepository.GetAll();
        return HttpResponder.Respond(readings);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Save([FromBody] TemperatureDeviceSaveRequest request)
    {
        var result = await _deviceRepository.Save(request);

        await RefreshTopicSubscriptions();

        return HttpResponder.Respond(result);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _deviceRepository.Delete(id);

        await RefreshTopicSubscriptions();

        return HttpResponder.Respond(result);
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
