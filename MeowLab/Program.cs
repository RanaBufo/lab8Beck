using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using MeowLab.Models;

var adminRole = new Role("admin");
var userRole = new Role("user");
var people = new List<Person>
{
     new Person("Liza@gmail.com", "12345",2024,  adminRole),
    new Person("Mila@mail.com", "55555",  2006, userRole),
    new Person("Shurochka@gmail.com", "qwert",  2006, userRole)
};

var builder = WebApplication.CreateBuilder();
builder.Services.AddTransient<IAuthorizationHandler, AgeHandler>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/accessdenied";
    });


builder.Services.AddAuthorization(opts => {
    // устанавливаем ограничение по возрасту
    opts.AddPolicy("AgeLimit", policy => policy.Requirements.Add(new AgeRequirement(18)));
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NonGmailEmailDomainPolicy", policy =>
        policy.RequireAssertion(context =>
        {
            var emailClaims = context.User.FindAll(ClaimTypes.Email);
            if (emailClaims != null && emailClaims.Any())
            {
                foreach (var emailClaim in emailClaims)
                {
                    var email = emailClaim.Value;
                    if (!string.IsNullOrEmpty(email) && !email.EndsWith("@gmail.com"))
                    {
                        Console.WriteLine("Email адрес не заканчивается на '@gmail.com': " + email);
                    }
                    else
                    {
                        Console.WriteLine("Email адрес заканчивается на '@gmail.com': " + email);
                    }
                }
                return true; // Успешно обработаны все email'ы
            }
            else
            {
                Console.WriteLine("Email адреса не найдены");
                return false; // Не найдены email'ы
            }
        }));
});



var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();   

// добавление middleware авторизации 

app.MapGet("/accessdenied", async (HttpContext context) =>
{
    context.Response.StatusCode = 403;
    await context.Response.WriteAsync("Access Denied");
});
app.MapGet("/login", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    // html-форма для ввода логина/пароля
    string loginForm = @"<!DOCTYPE html>
    <html>
    <head>
        <meta charset='utf-8' />
        <title>METANIT.COM</title>
    </head>
    <body>
        <h2>Login Form</h2>
        <form method='post'>
            <p>
                <label>Email</label><br />
                <input name='email' />
            </p>
            <p>
                <label>Password</label><br />
                <input type='password' name='password' />
            </p>
            <input type='submit' value='Login' />
        </form>
    </body>
    </html>";
    await context.Response.WriteAsync(loginForm);
});

app.MapPost("/login", async (string? returnUrl, HttpContext context) =>
{
    // Получаем из формы email и пароль
    var form = context.Request.Form;
    if (!form.ContainsKey("email") || !form.ContainsKey("password"))
        return Results.BadRequest("Email и/или пароль не установлены");

    string email = form["email"];
    string password = form["password"];

    // Находим пользователя 
    Person? person = people.FirstOrDefault(p => p.Email == email && p.Password == password);
    if (person is null) return Results.Unauthorized();

    var claims = new List<Claim>
    {
        new Claim(ClaimsIdentity.DefaultNameClaimType, person.Email),
        new Claim(ClaimsIdentity.DefaultRoleClaimType, person.Role.Name),
        new Claim(ClaimTypes.DateOfBirth, person.Year.ToString()),
        new Claim(ClaimTypes.Email, person.Email) // Добавляем утверждение о типе Email
    };

    var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
    await context.SignInAsync(claimsPrincipal);

    return Results.Redirect(returnUrl ?? "/");
});
// доступ только для роли admin
app.Map("/admin", [Authorize(Roles = "admin")] () => "Admin Panel");
// доступ только для тех, кто соответствует ограничению AgeLimit
app.Map("/age", [Authorize(Policy = "AgeLimit")] () => "Age Limit is passed");
// доступ только для тех, кто соответствует ограничению AgeLimit
app.Map("/eMail", [Authorize(Policy = "NonGmailEmailDomainPolicy")] () => "NonGmailEmailDomainPolicy");



// доступ только для ролей admin и user
app.Map("/", [Authorize(Roles = "admin, user")] (HttpContext context) =>
{
    var login = context.User.FindFirst(ClaimsIdentity.DefaultNameClaimType);
    var role = context.User.FindFirst(ClaimsIdentity.DefaultRoleClaimType);
    return $"Name: {login?.Value}\nRole: {role?.Value}";
});
app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return "Данные удалены";
});

app.Run();
class AgeRequirement : IAuthorizationRequirement
{
    protected internal int Age { get; set; }
    public AgeRequirement(int age) => Age = age;
}
class AgeHandler : AuthorizationHandler<AgeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        AgeRequirement requirement)
    {
        // получаем claim с типом ClaimTypes.DateOfBirth - год рождения
        var yearClaim = context.User.FindFirst(c => c.Type == ClaimTypes.DateOfBirth);
        if (yearClaim is not null)
        {
            // если claim года рождения хранит число
            if (int.TryParse(yearClaim.Value, out var year))
            {
                // и разница между текущим годом и годом рождения больше требуемого возраста
                if ((DateTime.Now.Year - year) >= requirement.Age)
                {
                    context.Succeed(requirement); // сигнализируем, что claim соответствует ограничению
                }
            }
        }
        return Task.CompletedTask;
    }
}