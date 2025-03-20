using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class BookingDetail
{
    public Guid Id { get; set; }

    public Guid ServicePackageId { get; set; }

    public Guid BookingId { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual ServicePackage ServicePackage { get; set; } = null!;
}
