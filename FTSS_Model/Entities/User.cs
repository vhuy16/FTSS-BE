﻿using System;
using System.Collections.Generic;

namespace FTSS_Model.Entities;

public partial class User
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string Gender { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Role { get; set; }

    public string? Status { get; set; }

    public DateTime? CreateDate { get; set; }

    public DateTime? ModifyDate { get; set; }

    public bool? IsDelete { get; set; }

    public string? CityId { get; set; }

    public string? DistrictId { get; set; }

    public string? BankNumber { get; set; }

    public string? BankName { get; set; }

    public string? BankHolder { get; set; }

    public string? WardId { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Mission> Missions { get; set; } = new List<Mission>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Otp> Otps { get; set; } = new List<Otp>();

    public virtual ICollection<ReturnRequest> ReturnRequests { get; set; } = new List<ReturnRequest>();

    public virtual ICollection<SetupPackage> SetupPackages { get; set; } = new List<SetupPackage>();
}
