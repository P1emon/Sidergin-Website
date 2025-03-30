using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Faq
{
    [Key]
    [Column("faq_id")]  // Map đúng với database
    public int FaqId { get; set; }

    [Required]
    [Column("question")]
    [MaxLength(1000)]
    public string Question { get; set; }

    [Required]
    [Column("answer")]
    [MaxLength(2000)]
    public string Answer { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
