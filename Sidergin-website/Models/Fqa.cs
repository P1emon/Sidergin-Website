using System;
using System.Collections.Generic;

namespace Sidergin_website.Models;

public partial class Fqa
{
    public int FqaId { get; set; }

    public string Question { get; set; } = null!;

    public string Answer { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }
}
