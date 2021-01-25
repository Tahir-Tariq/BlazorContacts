using BlazorContacts.Server.Services;
using Entities.RequestParameters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BlazorContacts.Server.Controllers
{
    [Route("api/Contacts")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactsController(IContactService contactService)
        {
            _contactService = contactService;
        }

		[HttpGet]
        public IActionResult Get([FromQuery] ContactParameters ContactParameters)
        {
            var Contacts = _contactService.GetContacts(ContactParameters);

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(Contacts.MetaData));

            return Ok(Contacts);
        }
    }
}