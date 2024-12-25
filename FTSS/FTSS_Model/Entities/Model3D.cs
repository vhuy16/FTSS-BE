using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class Model3D
{
    public Guid Id { get; set; }

    public string Link { get; set; } = null!;

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
