using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TR.Connector.Entities;
using TR.Connectors.Api.Entities;

namespace TR.Connector.Services
{
    public class ConnectorService
    {
        private const string ItRoleString = "ItRole";
        private const string RequestRightString = "RequestRight";
        private const string LockString = "Lock";
        private const string UnlockString = "Unlock";
        private const string JsonMediaTypeString = "application/json";

        private const string UsersString = "api/v1/users";
        private const string AllRolesString = "api/v1/roles/all";
        private const string AllRightsString = "api/v1/rights/all";
        private const string LoginString = "api/v1/login";

        public HttpClient Client { get; private set; }

        public ConnectorService(string connectionString)
        {
            SetHttpClient(connectionString);
        }

        public void SetHttpClient(string connectionString)
        {
            var httpClient = new HttpClient();
            string login = string.Empty;
            string password = string.Empty;

            //Парсим строку подключения.
            foreach (var item in connectionString.Split(';'))
            {
                var splittedItem = item.Trim().Split('=');
                if (splittedItem.Length < 2) continue;

                if (splittedItem[0].StartsWith("url")) httpClient.BaseAddress = new Uri(splittedItem[1]);
                if (splittedItem[0].StartsWith("login")) login = splittedItem[1];
                if (splittedItem[0].StartsWith("password")) password = splittedItem[1];
            }

            //Проходим аутентификацию на сервере.
            var content = new StringContent(JsonSerializer.Serialize(new { login, password }), Encoding.UTF8, JsonMediaTypeString);
            var response = httpClient.PostAsync(LoginString, content).Result;
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(response.Content.ReadAsStringAsync().Result);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse?.ResponseData?.AccessToken);
            Client = httpClient;
        }

        public IEnumerable<Permission> GetAllPermissions()
        {
            //Получаем ИТРоли
            var response = Client.GetAsync(AllRolesString).Result;
            var itRoleResponse = JsonSerializer.Deserialize<RoleResponse>(response.Content.ReadAsStringAsync().Result);
            var itRolePermissions = itRoleResponse?.ResponseData?
                .Select(responseData => new Permission($"{ItRoleString},{responseData.Id}", responseData.Name, responseData.CorporatePhoneNumber)) ?? [];

            //Получаем права
            response = Client.GetAsync(AllRightsString).Result;
            var rightResponse = JsonSerializer.Deserialize<RightResponse>(response.Content.ReadAsStringAsync().Result);
            var rightPermissions = rightResponse?.ResponseData?
                .Select(responseData => new Permission($"{RequestRightString},{responseData.Id}", responseData.Name, responseData.Users.ToString() ?? string.Empty)) ?? [];

            return itRolePermissions.Concat(rightPermissions);
        }

        public IEnumerable<string> GetUserPermissions(string userLogin)
        {
            //Получаем ИТРоли
            var response = Client.GetAsync($"{UsersString}/{userLogin}/roles").Result;
            var itRoleResponse = JsonSerializer.Deserialize<UserRoleResponse>(response.Content.ReadAsStringAsync().Result);
            var roleResult = itRoleResponse?.ResponseData?.Select(responseData => $"{ItRoleString},{responseData.Id}") ?? [];

            //Получаем права
            response = Client.GetAsync($"{UsersString}/{userLogin}/rights").Result;
            var rightResponse = JsonSerializer.Deserialize<UserRightResponse>(response.Content.ReadAsStringAsync().Result);
            var rightResult = rightResponse?.ResponseData?.Select(responseData => $"{RequestRightString},{responseData.Id}") ?? [];

            return roleResult.Concat(rightResult);
        }

        public void AddUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            //проверяем что пользователь не залочен.
            var response = Client.GetAsync($"{UsersString}/all").Result;
            var userResponse = JsonSerializer.Deserialize<UserResponse>(response.Content.ReadAsStringAsync().Result);
            var user = (userResponse?.ResponseData?.FirstOrDefault(responseData => responseData.Login == userLogin)) ?? throw new Exception($"Пользователь {userLogin} не найден");

            if (user.Status == LockString) throw new Exception($"Пользователь {userLogin} залочен.");
            if (user.Status == UnlockString)
            {
                foreach (var rightId in rightIds)
                {
                    var rightStr = rightId.Split(',');
                    switch (rightStr[0])
                    {
                        case ItRoleString:
                            Client.PutAsync($"{UsersString}/{userLogin}/add/role/{rightStr[1]}", null).Wait();
                            break;
                        case RequestRightString:
                            Client.PutAsync($"{UsersString}/{userLogin}/add/right/{rightStr[1]}", null).Wait();
                            break;
                        default:
                            throw new Exception($"Тип доступа {rightStr[0]} не определен");
                    }
                }
            }
        }

        public void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            //проверяем что пользователь не залочен.
            var response = Client.GetAsync($"{UsersString}/all").Result;
            var userResponse = JsonSerializer.Deserialize<UserResponse>(response.Content.ReadAsStringAsync().Result);
            var user = (userResponse?.ResponseData?.FirstOrDefault(responseData => responseData.Login == userLogin)) ?? throw new Exception($"Пользователь {userLogin} не найден");

            if (user.Status == LockString) throw new Exception($"Пользователь {userLogin} залочен.");
            if (user.Status == UnlockString)
            {
                foreach (var rightId in rightIds)
                {
                    var rightStr = rightId.Split(',');
                    switch (rightStr[0])
                    {
                        case ItRoleString:
                            Client.DeleteAsync($"{UsersString}/{userLogin}/drop/role/{rightStr[1]}").Wait();
                            break;
                        case RequestRightString:
                            Client.DeleteAsync($"{UsersString}/{userLogin}/drop/right/{rightStr[1]}").Wait();
                            break;
                        default:
                           throw new Exception($"Тип доступа {rightStr[0]} не определен");
                    }
                }
            }
        }

        public static IEnumerable<Property> GetAllProperties()
        {
            return new UserPropertyData().GetType().GetProperties()
                .Where(propertyInfo => !propertyInfo.Name.Equals("login", StringComparison.OrdinalIgnoreCase))
                .Select(propertyInfo => new Property(propertyInfo.Name, propertyInfo.Name));
        }

        public IEnumerable<UserProperty> GetUserProperties(string userLogin)
        {
            var response = Client.GetAsync($"{UsersString}/{userLogin}").Result;
            var userResponse = JsonSerializer.Deserialize<UserPropertyResponse>(response.Content.ReadAsStringAsync().Result);
            var user = userResponse?.ResponseData ?? throw new NullReferenceException($"Пользователь {userLogin} не найден");
            if (user.Status == LockString) throw new Exception($"Невозможно получить свойства, пользователь {userLogin} залочен");

            return user.GetType().GetProperties()
                .Select(property => new UserProperty(property.Name, property.GetValue(user)?.ToString() ?? string.Empty));
        }

        public void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin)
        {
            var response = Client.GetAsync($"{UsersString}/{userLogin}").Result;
            var userResponse = JsonSerializer.Deserialize<UserPropertyResponse>(response.Content.ReadAsStringAsync().Result);
            var user = userResponse?.ResponseData ?? throw new NullReferenceException($"Пользователь {userLogin} не найден");
            if (user.Status == LockString) throw new Exception($"Невозможно обновить свойства, пользователь {userLogin} залочен");

            foreach (var userProp in user.GetType().GetProperties())
            {
                var property = properties.FirstOrDefault(property => property.Name == userProp.Name);
                if (property != null) userProp.SetValue(user, property.Value);
            }

            var content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, JsonMediaTypeString);
            Client.PutAsync($"{UsersString}/edit", content).Wait();
        }

        public bool IsUserExists(string userLogin)
        {
            var response = Client.GetAsync($"{UsersString}/all").Result;
            var userResponse = JsonSerializer.Deserialize<UserResponse>(response.Content.ReadAsStringAsync().Result);
            return userResponse?.ResponseData?.Any(responseData => responseData.Login == userLogin) == true;
        }

        public void CreateUser(UserToCreate user)
        {
            var newUser = new CreateUSerDTO()
            {
                Login = user.Login,
                Password = user.HashPassword,

                LastName = user.Properties.FirstOrDefault(p => p.Name.Equals("lastName", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty,
                FirstName = user.Properties.FirstOrDefault(p => p.Name.Equals("firstName", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty,
                MiddleName = user.Properties.FirstOrDefault(p => p.Name.Equals("middleName", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty,

                TelephoneNumber = user.Properties.FirstOrDefault(p => p.Name.Equals("telephoneNumber", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty,
                IsLead = bool.TryParse(user.Properties.FirstOrDefault(p => p.Name.Equals("isLead", StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty, out bool isLeadValue) && isLeadValue,

                Status = string.Empty
            };

            var content = new StringContent(JsonSerializer.Serialize(newUser), Encoding.UTF8, JsonMediaTypeString);
            Client.PostAsync($"{UsersString}/create", content).Wait();
        }
    }
}
