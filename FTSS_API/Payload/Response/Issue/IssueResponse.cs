using System;
using System.Collections.Generic;

public class IssueResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Guid? IssueCategoryId { get; set; }
    public string IssueCategoryName { get; set; }
    public string IssueImage { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public bool? IsDelete { get; set; }
    public List<SolutionResponse> Solutions { get; set; } = new List<SolutionResponse>();
    
    // Additional statistics/metrics (optional)
    public int SolutionCount => Solutions?.Count ?? 0;
    public int ProductCount => Solutions?.Sum(s => s.Products?.Count ?? 0) ?? 0;
}

public class SolutionResponse
{
    public Guid Id { get; set; }
    public string SolutionName { get; set; }
    public string Description { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public List<IssueProductResponse> Products { get; set; } = new List<IssueProductResponse>();
}

public class IssueProductResponse
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string ProductDescription { get; set; }
    public string ProductImageUrl { get; set; } // Optional

}