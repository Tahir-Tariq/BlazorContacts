using BlazorContacts.Client.HttpRepository;
using Entities.Models;
using Entities.RequestFeatures;
using Entities.RequestParameters;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorContacts.Client.Pages
{
    public partial class Contacts
    {
        public List<Contact> ContactList { get; set; } = new List<Contact>();
        public MetaData MetaData { get; set; } = new MetaData();

        private ContactParameters _ContactParameters = new ContactParameters();

        [Inject]
        public IContactHttpRepository ContactRepo { get; set; }

        protected async override Task OnInitializedAsync()
        {
            await GetContacts();
        }

        private async Task SelectedPage(int page)
        {
            _ContactParameters.PageNumber = page;
            await GetContacts();
        }

        private async Task GetContacts()
        {
            var pagingResponse = await ContactRepo.GetContacts(_ContactParameters);
            ContactList = pagingResponse.Items;
            MetaData = pagingResponse.MetaData;
        }

        private async Task SearchChanged(string searchTerm)
        {
            Console.WriteLine(searchTerm);
            _ContactParameters.PageNumber = 1;
            _ContactParameters.SearchTerm = searchTerm;
            await GetContacts();
        }
    }
}
