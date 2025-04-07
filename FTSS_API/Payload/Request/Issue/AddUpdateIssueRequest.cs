using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class AddUpdateIssueRequest
{
    
    public string? Title { get; set; }

    public string? Description { get; set; }

   
    public Guid? IssueCategoryId { get; set; }
    
    public IFormFile? IssueImage { get; set; }

    public List<String>? SolutionsJson { get; set; }
}