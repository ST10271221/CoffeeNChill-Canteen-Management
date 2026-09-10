using System;
using System.Collections.Generic;
using System.Text;
using CoffeeNChillCanteen.Repositories;
using CoffeeNChillCanteen.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace CoffeeNChillCanteen
{
    public class HealthFunctions
    {
        private readonly IMenuRepository _menuRepository;
        private readonly IBlobStorageService _blobStorageService;

        public HealthFunctions(
            IMenuRepository menuRepository,
            IBlobStorageService blobStorageService)
        {
            _menuRepository = menuRepository;
            _blobStorageService = blobStorageService;
        }

        [Function("HealthCheck")]
        public async Task<HttpResponseData> HealthCheck(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "health")] HttpRequestData req)
        {
            try
            {
                var menuItems = await _menuRepository.GetAllAsync();
                var documents = await _blobStorageService.ListAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    status = "Healthy",
                    tableStorage = new
                    {
                        status = "Connected",
                        menuItemCount = menuItems.Count
                    },
                    blobStorage = new
                    {
                        status = "Connected",
                        documentCount = documents.Count
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                var response = req.CreateResponse(
                    HttpStatusCode.ServiceUnavailable);

                await response.WriteAsJsonAsync(new
                {
                    status = "Unhealthy",
                    message = "One or more storage services are unavailable.",
                    error = ex.Message
                });

                return response;
            }
        }
    }
}
