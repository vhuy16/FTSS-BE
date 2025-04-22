using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class MissionImage
{
    public Guid Id { get; set; }

    public string? LinkImage { get; set; }

    public Guid? MissionId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public string? Type { get; set; }

    public virtual Mission? Mission { get; set; }
}
