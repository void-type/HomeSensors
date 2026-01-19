using HomeSensors.Model.Temperature.Models;
using HomeSensors.Model.Temperature.Repositories;
using HomeSensors.Model.Temperature.Workers;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/temperature-devices")]
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
    public async Task<IActionResult> GetAllAsync()
    {
        return await _deviceRepository.GetAllAsync()
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> SaveAsync([FromBody] TemperatureDeviceSaveRequest request)
    {
        return await _deviceRepository.SaveAsync(request)
            .TeeAsync(RefreshTopicSubscriptionsAsync)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        return await _deviceRepository.DeleteAsync(id)
            .TeeAsync(RefreshTopicSubscriptionsAsync)
            .MapAsync(HttpResponder.Respond);
    }

    private async Task RefreshTopicSubscriptionsAsync()
    {
        var worker = _contextAccessor?
            .HttpContext?
            .RequestServices?
            .GetServices<IHostedService>()?
            .OfType<MqttTemperaturesWorker>()
            .FirstOrDefault();

        if (worker is not null)
        {
            await worker.RefreshTopicSubscriptionsAsync();
        }
    }
}
