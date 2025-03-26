using System;
using System.Collections.Generic;

namespace Sidergin_website.Models;

public partial class Contact
{
    public int ContactId { get; set; }

    public string Phone { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Address { get; set; }

    public string? Zalo { get; set; }

    public DateTime? CreatedAt { get; set; }
}
