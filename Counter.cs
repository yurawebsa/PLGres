using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CounterPlg
{
    public class Counter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string CounterNumber { get; set; }
        public string Brand { get; set; }
        public string KCM { get; set; }

        // Додано — зв’язок з номерами лічильника
        public virtual ICollection<CounterNumber> CounterNumbers { get; set; } = new List<CounterNumber>();
    }
}
