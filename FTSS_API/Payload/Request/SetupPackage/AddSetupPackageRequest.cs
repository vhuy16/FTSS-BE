﻿namespace FTSS_API.Payload.Request.SetupPackage
{
    public class AddSetupPackageRequest
    {
        public string? SetupName { get; set; }

        public string? Description { get; set; }
        public IFormFile? ImageFile { get; set; }
    }
}
