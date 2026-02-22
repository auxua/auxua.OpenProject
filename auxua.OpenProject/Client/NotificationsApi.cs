using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace auxua.OpenProject.Client
{
    /// <summary>
    /// Client for interacting with OpenProject in-app notifications
    /// </summary>
    public class NotificationsApi
    {
        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public NotificationsApi(HttpClient http, IAuthProvider? auth)
        {
            _http = http;
            _auth = auth;
        }

        /// <summary>
        /// Retrieve a page of notifications from the OpenProject API.
        /// Supports filtering, sorting and grouping (all parameters are optional).
        /// </summary>
        /// <param name="pageSize">Number of elements per page (OP default: 20).</param>
        /// <param name="offset">Page number (OP default: 1). Note: OpenProject uses "offset" as page number for this endpoint.</param>
        /// <param name="filtersJson">Raw JSON array for "filters" (will be URL-encoded). Example: [{ "readIAN": { "operator": "=", "values": ["f"] } }]</param>
        /// <param name="sortByJson">Raw JSON for "sortBy" (will be URL-encoded). Example: [["id","desc"]]</param>
        /// <param name="groupBy">Grouping criteria, e.g. "reason" or "project".</param>
        public async Task<NotificationCollection> GetNotificationsAsync(
            int pageSize = 100,
            int offset = 1,
            string? filtersJson = null,
            string? sortByJson = null,
            string? groupBy = null,
            bool getDetails = false)
        {
            var url = BuildCollectionUrl(
                "api/v3/notifications",
                pageSize,
                offset,
                filtersJson,
                sortByJson,
                groupBy);

            var body = await REST.RequestHelper.GetAsStringAsync(url, _http, _auth);


            //using var req = new HttpRequestMessage(HttpMethod.Get, url);
            //_auth?.Apply(req);

            //var resp = await _http.SendAsync(req);
            //var body = await resp.Content.ReadAsStringAsync();

            //if (!resp.IsSuccessStatusCode)
            //    throw new ApiException(resp.StatusCode, body);

            var res = JsonConvert.DeserializeObject<NotificationCollection>(body)
                   ?? new NotificationCollection();

            if (!getDetails) return res;

            // Fetch details for each notification in parallel (if requested)
            foreach (var item in res.Elements)
            {
                var details = await GetNotificationAsync(item.Id);
                item.Embedded = details.Embedded; // Assuming details contains the necessary embedded info
            }
            return res;
        }

        /// <summary>
        /// Fetch all notifications by iterating the paginated endpoint.
        /// Keeps filters/sort/group stable across pages.
        /// </summary>
        public Task<List<Notification>> GetAllNotificationsAsync(
            int pageSize = 100,
            string? filtersJson = null,
            string? sortByJson = null,
            string? groupBy = null)
        {
            return PaginationHelper.FetchAllAsync<Notification>(
                async (page, ps) =>
                {
                    //Console.WriteLine("Called!");
                    var collection = await GetNotificationsAsync(ps, page, filtersJson, sortByJson, groupBy);
                    
                    return collection;
                },
                pageSize: pageSize,
                startPage: 1
            );
        }

        /// <summary>
        /// Get a single notification by id.
        /// </summary>
        public async Task<Notification> GetNotificationAsync(int id)
        {
            var url = $"api/v3/notifications/{id}";
            var body = await REST.RequestHelper.GetAsStringAsync(url, _http, _auth);

            //using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/notifications/{id}");
            //_auth?.Apply(req);

            //var resp = await _http.SendAsync(req);
            //var body = await resp.Content.ReadAsStringAsync();

            //if (!resp.IsSuccessStatusCode)
            //    throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<Notification>(body)
                   ?? new Notification();
        }

        /// <summary>
        /// Marks a single notification as read (204 No Content).
        /// </summary>
        public Task MarkAsReadAsync(int id) =>
            REST.RequestHelper.PostNoContentAsync($"api/v3/notifications/{id}/read_ian",_http,_auth);

        /// <summary>
        /// Marks a single notification as unread (204 No Content).
        /// </summary>
        public Task MarkAsUnreadAsync(int id) =>
            REST.RequestHelper.PostNoContentAsync($"api/v3/notifications/{id}/unread_ian", _http, _auth);

        /// <summary>
        /// Marks the notification collection as read (204 No Content).
        /// Can be reduced with filters (query param).
        /// </summary>
        public Task MarkAllAsReadAsync(string? filtersJson = null) =>
            REST.RequestHelper.PostNoContentAsync(BuildBulkUrl("api/v3/notifications/read_ian", filtersJson), _http, _auth);

        /// <summary>
        /// Marks the notification collection as unread (204 No Content).
        /// Can be reduced with filters (query param).
        /// </summary>
        public Task MarkAllAsUnreadAsync(string? filtersJson = null) =>
            REST.RequestHelper.PostNoContentAsync(BuildBulkUrl("api/v3/notifications/unread_ian", filtersJson), _http, _auth);

        /// <summary>
        /// Retrieves a single notification detail (Values::Property) by notification id and detail id.
        /// </summary>
        public async Task<NotificationDetail> GetNotificationDetailAsync(int notificationId, int detailId)
        {
            var url = $"api/v3/notifications/{notificationId}/details/{detailId}";
            var body = await REST.RequestHelper.GetAsStringAsync(url, _http, _auth);

            //using var req = new HttpRequestMessage(HttpMethod.Get, $"api/v3/notifications/{notificationId}/details/{detailId}");
            //_auth?.Apply(req);

            //var resp = await _http.SendAsync(req);
            //var body = await resp.Content.ReadAsStringAsync();

            //if (!resp.IsSuccessStatusCode)
            //    throw new ApiException(resp.StatusCode, body);

            return JsonConvert.DeserializeObject<NotificationDetail>(body)
                   ?? new NotificationDetail();
        }

        

        private static string BuildBulkUrl(string basePath, string? filtersJson)
        {
            if (string.IsNullOrWhiteSpace(filtersJson))
                return basePath;

            return basePath + "?filters=" + Uri.EscapeDataString(filtersJson);
        }

        private static string BuildCollectionUrl(
            string basePath,
            int pageSize,
            int offset,
            string? filtersJson, // Take from Notifcations Model -> NotificationFilters
            string? sortByJson,
            string? groupBy)
        {
            var sb = new StringBuilder();
            sb.Append(basePath);
            sb.Append("?pageSize=").Append(pageSize);
            sb.Append("&offset=").Append(offset);

            if (!string.IsNullOrWhiteSpace(filtersJson))
                sb.Append("&filters=").Append(Uri.EscapeDataString(filtersJson));

            if (!string.IsNullOrWhiteSpace(sortByJson))
                sb.Append("&sortBy=").Append(Uri.EscapeDataString(sortByJson));

            if (!string.IsNullOrWhiteSpace(groupBy))
                sb.Append("&groupBy=").Append(Uri.EscapeDataString(groupBy));

            return sb.ToString();
        }
    }
}