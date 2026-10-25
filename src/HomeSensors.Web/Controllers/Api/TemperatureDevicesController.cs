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
public class TemperatureDevicesController(TemperatureDeviceRepository deviceRepository, IHttpContextAccessor contextAccessor) : ControllerBase
{
    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(List<TemperatureDeviceResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return await deviceRepository.GetAllAsync(cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> SaveAsync([FromBody] TemperatureDeviceSaveRequest request, CancellationToken cancellationToken)
    {
        return await deviceRepository.SaveAsync(request, cancellationToken)
            .TeeAsync(RefreshTopicSubscriptionsAsync)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        return await deviceRepository.DeleteAsync(id, cancellationToken)
            .TeeAsync(RefreshTopicSubscriptionsAsync)
            .MapAsync(HttpResponder.Respond);
    }

    private async Task RefreshTopicSubscriptionsAsync()
    {
        var worker = contextAccessor?
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
