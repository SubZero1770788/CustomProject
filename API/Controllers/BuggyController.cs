using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace API.Controllers
{
    public class BuggyController: BaseAPIController
    {
        private readonly DataContext _context;
          
          public BuggyController(DataContext context)
          {
            _context = context;
          }

          [Authorize]
          [HttpGet("auth")]
          public ActionResult<string> GetSecret()
          {
            return "secret text";
          }
          
          [HttpGet("not-found")]
          public ActionResult<AppUser> GetNoFound()
          {
           var thing = _context.Users.Find(-1);

           if (thing == null) return NotFound();
           return thing;
          }
          
          [HttpGet("server-error")]
          public ActionResult<string> GetServerError()
          {
            var thing = _context.Users.Find(-1);

            var thingtoReturn = thing.ToString();

            return thingtoReturn;
          }
          
          [HttpGet("bad-request")]
          public ActionResult<string> GetBadRequest()
          {
            return BadRequest("Bad request");
          }
    }
}