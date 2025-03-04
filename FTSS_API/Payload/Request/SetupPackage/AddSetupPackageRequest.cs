using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FTSS_API.Payload.Request.SetupPackage
{
    public class AddSetupPackageRequest
    {
        public string? SetupName { get; set; }

        public string? Description { get; set; }

        public IFormFile? ImageFile { get; set; }
        public string? ProductItemsJson { get; set; }
    }

    public class ProductSetupItem
    {
        public Guid ProductId { get; set; }
        public int? Quantity { get; set; }
    }
}
