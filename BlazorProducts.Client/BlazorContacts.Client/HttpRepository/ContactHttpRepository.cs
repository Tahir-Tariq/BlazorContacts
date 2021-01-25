using BlazorContacts.Client.Features;
using Entities.Models;
using Entities.RequestFeatures;
using Entities.RequestParameters;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace BlazorContacts.Client.HttpRepository
{
    public class ContactHttpRepository : IContactHttpRepository
    {
        private readonly HttpClient _client;

        public ContactHttpRepository(HttpClient client)
        {
            _client = client;
        }

        public async Task<PagingResponse<Contact>> GetContacts(ContactParameters ContactParameters)
        {
            var queryStringParam = new Dictionary<string, string>
            {
                ["pageNumber"] = ContactParameters.PageNumber.ToString(),
                ["searchTerm"] = ContactParameters.SearchTerm == null ? "" : ContactParameters.SearchTerm
            };

            var response = await _client.GetAsync(QueryHelpers.AddQueryString("https://localhost:5011/api/Contacts", queryStringParam));
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new ApplicationException(content);
            }

            var pagingResponse = new PagingResponse<Contact>
            {
                Items = JsonSerializer.Deserialize<List<Contact>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }),
                MetaData = JsonSerializer.Deserialize<MetaData>(response.Headers.GetValues("X-Pagination").First(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true})
            };
            
            return pagingResponse;
        }
    }
}
