using System.Threading.Tasks;
using BlazorContacts.Server.Repository;
using Entities.RequestParameters;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace BlazorContacts.Server.Controllers
{
    [Route("api/Contacts")]
    [ApiController]
    public class ContactsController : ControllerBase
    {
        private readonly IContactRepository _repo;

        public ContactsController(IContactRepository repo)
        {
            _repo = repo;
        }

		[HttpGet]
        public IActionResult Get([FromQuery] ContactParameters ContactParameters)
        {
            var Contacts = _repo.GetContacts(ContactParameters);

            Response.Headers.Add("X-Pagination", JsonConvert.SerializeObject(Contacts.MetaData));

            return Ok(Contacts);
        }
    }
}