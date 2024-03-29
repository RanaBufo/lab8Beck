/*using Microsoft.AspNetCore.Mvc;
using MeowLab.Models;

namespace MeowLab.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonController : Controller
    {
        private readonly ApplicationContext _db;
        public PersonController(ApplicationContext db)
        {
            _db = db;
        }
        [HttpGet]
        public IResult GetPerson()
        {
            var objCategoryList = _db.Persons.ToList();
            return Results.Json(objCategoryList);
        }
    }
}
*/