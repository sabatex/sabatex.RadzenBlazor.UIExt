using Microsoft.AspNetCore.Components;
using Radzen;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace sabatex.RadzenBlazor;

public class RadzenOdataAdapter : IRadzenDataAdapter
{
    private readonly HttpClient httpClient;
    private readonly Uri baseUri;
    private readonly NavigationManager navigationManager;
    private readonly NotificationService notificationService;


    public RadzenOdataAdapter(HttpClient httpClient, NavigationManager navigationManager, NotificationService notificationService)
    {
        this.httpClient = httpClient;
        this.navigationManager = navigationManager;
        baseUri = new Uri($"{navigationManager.BaseUri}odata/");
        this.notificationService = notificationService;
    }
    public async Task<(IEnumerable<TItem> items,int count)> GetAsync<TItem>(string? filter, string? orderby, string? expand, int? top, int? skip, bool? count, string? format = null, string? select = null) where TItem : class
    {
        try
        {
            var uri = new Uri(baseUri, typeof(TItem).Name);
            uri = Radzen.ODataExtensions.GetODataUri(uri: uri, filter: filter, top: top, skip: skip, orderby: orderby, expand: expand, select: select, count: count);
            Console.Write($"client get - {uri.ToString()}") ;
            
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            var response = await httpClient.SendAsync(httpRequestMessage);
            var result = await Radzen.HttpResponseMessageExtensions.ReadAsync<Radzen.ODataServiceResult<TItem>>(response);
            return (result.Value.AsODataEnumerable(), result.Count);
        }
        catch (Exception ex)
        {
            string error = $"Error method RadzenOdataAdapter.GET<{typeof(TItem).Name}>() -> {ex.Message}";
            throw new Exception(error);
        }
    }

    public async Task<TItem> PostAsync<TItem>(TItem? item) where TItem : class
    {
        var uri = new Uri(baseUri, typeof(TItem).Name);
        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri);
        httpRequestMessage.Content = new StringContent(Radzen.ODataJsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
        var response = await httpClient.SendAsync(httpRequestMessage);
        return await Radzen.HttpResponseMessageExtensions.ReadAsync<TItem>(response);
    }
    public async Task DeleteAsync<TItem>(int id) where TItem : class
    {
        try
        {
            var uri = new Uri(baseUri, $"{typeof(TItem).Name}({id})"); ;
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, uri);
            var responce = await httpClient.SendAsync(httpRequestMessage);
            if (responce?.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return;
             if (responce == null)
                throw new Exception("Невідома помилка при видаленні");
            throw new Exception($"Код відповіді сервера {responce.StatusCode}");
        }
        catch (Exception e)
        {
            string error = $"RadzenOdataAdapter помилка видалення з Entity {typeof(TItem).Name} значення з Id={id}. Помилка: {e.Message}";
            throw new Exception(error);
        }
    }
    public async Task UpdateAsync<TItem>(int id, TItem item) where TItem : class
    {
        try
        {
            var uri = new Uri(baseUri, $"{typeof(TItem).Name}({id})");
            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Patch, uri);
            httpRequestMessage.Content = new StringContent(Radzen.ODataJsonSerializer.Serialize(item), Encoding.UTF8, "application/json");
            var responce = await httpClient.SendAsync(httpRequestMessage);
            if (responce == null)
                throw new Exception("Невідома помилка");    
            if (responce.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new Exception($"Відсутній запис для Entity<{typeof(TItem).Name}> з Id = {id}");
            if (responce.StatusCode == System.Net.HttpStatusCode.BadRequest)
                throw new Exception($"Код відповіді сервера - BadRequest");
        }
        catch (Exception e)
        {
            throw new Exception($"Даны не оновлено! Error:{e.Message}");
         }
        
    }

    public void ExportToExcel<TItem>(Radzen.Query? query = null, string? fileName = null) where TItem : class
    {
        navigationManager.NavigateTo(query != null ? query.ToUrl($"api/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/tg/tgclients/excel(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
    }
    public void ExportToCSV<TItem>(Radzen.Query? query = null, string? fileName = null) where TItem : class
    {
        navigationManager.NavigateTo(query != null ? query.ToUrl($"api/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')") : $"export/tg/tgclients/csv(fileName='{(!string.IsNullOrEmpty(fileName) ? UrlEncoder.Default.Encode(fileName) : "Export")}')", true);
    }

    public async Task FillData<TItem>(RadzenODataCollection<TItem>? dataCollection, LoadDataArgs args, string? expand = null, string? filterFields = null) where TItem : class
    {
        try
        {
            if (dataCollection == null)
                throw new ArgumentNullException(nameof(dataCollection));
            string? filter = null;
            if (filterFields != null && args.Filter !=null)
            {
                var sb = new StringBuilder();
                bool first = true;
                foreach (var field in filterFields.Split(','))
                {
                    if (!first)
                    {
                        sb.Append(" or ");
                        first = false;
                    }
                    sb.Append($"contains({field},'{args.Filter.ToLower()}')");
                 }
                filter= sb.ToString();
            }

            var result = await GetAsync<TItem>(orderby: $"{args.OrderBy}",
                                               top: args.Top,
                                               skip: args.Skip,
                                               count: args.Top != null && args.Skip != null,
                                               expand: expand,
                                               filter: filter);
            dataCollection.Items = result.items;
            dataCollection.Count = result.count;
        }catch(Exception ex)
        {
            string error = $"Помилка зчитування {typeof(TItem).Name} \r\n {ex.Message}";
            notificationService?.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Error",
                Detail = error
            });

        }

    }
}