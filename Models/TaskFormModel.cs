using System.ComponentModel.DataAnnotations;

namespace toDo.Models
{
    public class TaskFormModel
    {
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Give your task a title.")]
        [StringLength(200, ErrorMessage = "Keep the title under 200 characters.")]
        public string Title { get; set; }

        [StringLength(1000, ErrorMessage = "Keep notes under 1,000 characters.")]
        public string Description { get; set; }
    }
}
