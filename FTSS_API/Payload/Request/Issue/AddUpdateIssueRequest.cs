using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class AddUpdateIssueRequest
{
    [Required]
    public string Title { get; set; }

    public string Description { get; set; }

    [Required]
    public Guid IssueCategoryId { get; set; }
    
    public IFormFile? IssueImage { get; set; }

    public List<SolutionRequest> Solutions { get; set; } = new List<SolutionRequest>();
}

public class SolutionRequest
{
    public Guid? Id { get; set; }  // Null for new solutions, has value for existing ones

    [Required]
    public string SolutionName { get; set; }

    public string Description { get; set; }

    public List<Guid> ProductIds { get; set; } = new List<Guid>();
}