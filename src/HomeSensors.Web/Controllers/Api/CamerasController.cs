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
public class CamerasController : ControllerBase
{
    private readonly CameraRepository _cameraRepository;

    public CamerasController(CameraRepository cameraRepository)
    {
        _cameraRepository = cameraRepository;
    }

    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(List<CameraResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAllAsync()
    {
        return await _cameraRepository.GetAllAsync()
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> SaveAsync([FromBody] CameraSaveRequest request)
    {
        return await _cameraRepository.SaveAsync(request)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        return await _cameraRepository.DeleteAsync(id)
            .MapAsync(HttpResponder.Respond);
    }
}
