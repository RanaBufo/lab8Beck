using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace MeowLab.Models
{
    class Person
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public int Year { get; set; }
        public Role Role { get; set; }
        public Person(string email, string password,int  age, Role role)
        {
            Email = email;
            Password = password;
            Role = role;
            Year = age;
        }
    }
    class Role
    {
        public string Name { get; set; }
        public Role(string name) => Name = name;
    }
}
