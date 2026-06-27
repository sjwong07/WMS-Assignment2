using System.ComponentModel.DataAnnotations;

namespace WMS_Assignment.Models.Entities
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(20)]
        public string RoleName { get; set; }

        [StringLength(100)]
        public string Description { get; set; }

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}