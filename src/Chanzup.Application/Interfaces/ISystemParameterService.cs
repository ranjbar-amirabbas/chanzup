using Chanzup.Application.DTOs;
using Chanzup.Domain.Entities;

namespace Chanzup.Application.Interfaces;

public interface ISystemParameterService
{
    // Parameter Management
    Task<SystemParameterResponse> GetParameterAsync(string key);
    Task<IEnumerable<SystemParameterResponse>> GetParametersByCategoryAsync(string category);
    Task<IEnumerable<SystemParameterResponse>> GetAllParametersAsync();
    Task<SystemParameterResponse> UpdateParameterAsync(string key, UpdateSystemParameterRequest request, Guid updatedBy);
    Task<SystemParameterResponse> CreateParameterAsync(string key, string value, string description, string category, ParameterType type, Guid createdBy);
    Task DeleteParameterAsync(string key);

    // Typed Parameter Access
    Task<string> GetStringParameterAsync(string key, string defaultValue = "");
    Task<int> GetIntParameterAsync(string key, int defaultValue = 0);
    Task<decimal> GetDecimalParameterAsync(string key, decimal defaultValue = 0m);
    Task<bool> GetBoolParameterAsync(string key, bool defaultValue = false);

    // Parameter Categories
    Task<IEnumerable<string>> GetParameterCategoriesAsync();
    
    // System Health
    Task InitializeDefaultParametersAsync();
    Task<bool> ValidateParametersAsync();
}