using Radzen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sabatex.RadzenBlazor;

public interface IRadzenDataAdapter
{
    Task<(IEnumerable<TItem> items, int count)> GetAsync<TItem>(string? filter, string? orderby, string? expand, int? top, int? skip, bool? count, string? format=null, string? select=null) where TItem : class;
    Task FillData<TItem>(RadzenODataCollection<TItem>? dataCollection, LoadDataArgs args, string? expand=null,string? filterFields = null) where TItem : class;

    Task<TItem> PostAsync<TItem>(TItem? item) where TItem : class;
    Task DeleteAsync<TItem>(int id) where TItem : class;
    Task UpdateAsync<TItem>(int id, TItem item) where TItem : class;
    void ExportToExcel<TItem>(Radzen.Query? query = null, string? fileName = null) where TItem : class;
    void ExportToCSV<TItem>(Radzen.Query? query = null, string? fileName = null) where TItem : class;
}
