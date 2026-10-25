using HomeSensors.Model.Cameras.Models;
using HomeSensors.Model.Cameras.Repositories;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/cameras")]
public class CamerasController(CameraRepository cameraRepository) : ControllerBase
{
    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(List<CameraResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return await cameraRepository.GetAllAsync(cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> SaveAsync([FromBody] CameraSaveRequest request, CancellationToken cancellationToken)
    {
        return await cameraRepository.SaveAsync(request, cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> DeleteAsync(long id, CancellationToken cancellationToken)
    {
        return await cameraRepository.DeleteAsync(id, cancellationToken)
            .MapAsync(HttpResponder.Respond);
    }
}
