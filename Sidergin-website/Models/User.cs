using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;

namespace Sidergin_website.Models;

public partial class User
{
    internal string email;
    internal string password;
    internal ClaimsIdentity? name;

    internal int user_id;
    internal string phone;
    internal DateTime created_at;
    internal DateTime updated_at;

    [NotMapped]
    public int UserId { get; set; }

    public string Email { get; set; }

    public string? Password { get; set; }

    public string? Phone { get; set; } = null!;

    public string? Address { get; set; }

    public string? Name { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();


}
