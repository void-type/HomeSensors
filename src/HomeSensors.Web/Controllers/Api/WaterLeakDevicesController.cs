using HomeSensors.Model.WaterLeak.Models;
using HomeSensors.Model.WaterLeak.Repositories;
using HomeSensors.Model.WaterLeak.Workers;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/water-leak-devices")]
public class WaterLeakDevicesController : ControllerBase
{
    private readonly WaterLeakDeviceRepository _deviceRepository;
    private readonly IHttpContextAccessor _contextAccessor;

    public WaterLeakDevicesController(WaterLeakDeviceRepository deviceRepository, IHttpContextAccessor contextAccessor)
    {
        _deviceRepository = deviceRepository;
        _contextAccessor = contextAccessor;
    }

    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(List<WaterLeakDeviceResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAllAsync()
    {
        return await _deviceRepository.GetAllAsync()
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> SaveAsync([FromBody] WaterLeakDeviceSaveRequest request)
    {
        return await _deviceRepository.SaveAsync(request)
            .TeeAsync(RefreshTopicSubscriptionsAsync)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> DeleteAsync(long id)
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
            .OfType<MqttWaterLeaksWorker>()
            .FirstOrDefault();

        if (worker is not null)
        {
            await worker.RefreshTopicSubscriptionsAsync();
        }
    }
}
