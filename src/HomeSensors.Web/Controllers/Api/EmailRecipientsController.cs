using HomeSensors.Model.Infrastructure.Emailing.Models;
using HomeSensors.Model.Infrastructure.Emailing.Repositories;
using Microsoft.AspNetCore.Mvc;
using VoidCore.AspNet.ClientApp;
using VoidCore.AspNet.Routing;
using VoidCore.Model.Functional;
using VoidCore.Model.Responses.Collections;
using VoidCore.Model.Responses.Messages;

namespace HomeSensors.Web.Controllers.Api;

[Route(ApiRouteAttribute.BasePath + "/email-recipients")]
public class EmailRecipientsController : ControllerBase
{
    private readonly EmailRecipientRepository _emailRecipientRepository;

    public EmailRecipientsController(EmailRecipientRepository emailRecipientRepository)
    {
        _emailRecipientRepository = emailRecipientRepository;
    }

    [HttpGet]
    [Route("all")]
    [ProducesResponseType(typeof(List<EmailRecipientResponse>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> GetAllAsync()
    {
        return await _emailRecipientRepository.GetAllAsync()
            .MapAsync(HttpResponder.Respond);
    }

    [HttpPost]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> SaveAsync([FromBody] EmailRecipientSaveRequest request)
    {
        return await _emailRecipientRepository.SaveAsync(request)
            .MapAsync(HttpResponder.Respond);
    }

    [HttpDelete]
    [Route("{id}")]
    [ProducesResponseType(typeof(EntityMessage<long>), 200)]
    [ProducesResponseType(typeof(IItemSet<IFailure>), 400)]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        return await _emailRecipientRepository.DeleteAsync(id)
            .MapAsync(HttpResponder.Respond);
    }
}
