using TR.Connector.Services;
using TR.Connectors.Api.Entities;
using TR.Connectors.Api.Interfaces;

namespace TR.Connector
{
    public class Connector : IConnector
    {
        public ILogger Logger { get; set; }
        private ConnectorService ConnectorService { get; set; }

        //Пустой конструктор
        public Connector() {}

        public void StartUp(string connectionString)
        {
            try
            {
                Logger.Debug($"Строка подключения: {connectionString}");
                ConnectorService = new ConnectorService(connectionString);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public IEnumerable<Permission> GetAllPermissions()
        {
            try
            {
                Logger.Debug("Получение всех разрешений");
                return ConnectorService.GetAllPermissions();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return [];
            }
        }

        public IEnumerable<string> GetUserPermissions(string userLogin) 
        {
            try
            {
                Logger.Debug($"Получение разрешений пользователя {userLogin}");
                return ConnectorService.GetUserPermissions(userLogin);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return [];
            }
        }

        public void AddUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                Logger.Debug($"Добавление разрешений для пользователя {userLogin}");
                ConnectorService.AddUserPermissions(userLogin, rightIds);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public void RemoveUserPermissions(string userLogin, IEnumerable<string> rightIds)
        {
            try
            {
                Logger.Debug($"Удаление разрешений для пользователя {userLogin}");
                ConnectorService.RemoveUserPermissions(userLogin, rightIds);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public IEnumerable<Property> GetAllProperties()
        {
            try
            {
                Logger.Debug("Получение всех свойств");
                return ConnectorService.GetAllProperties();
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return [];
            }
        }

        public IEnumerable<UserProperty> GetUserProperties(string userLogin)
        {
            try
            {
                Logger.Debug($"Получение свойств пользователя {userLogin}");
                return ConnectorService.GetUserProperties(userLogin);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return [];
            }
        }

        public void UpdateUserProperties(IEnumerable<UserProperty> properties, string userLogin)
        {
            try
            {
                Logger.Debug($"Обновление свойств пользователя {userLogin}");
                ConnectorService.UpdateUserProperties(properties, userLogin);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        public bool IsUserExists(string userLogin)
        {
            try
            {
                Logger.Debug($"Проверка существование пользователя {userLogin}");
                return ConnectorService.IsUserExists(userLogin);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }

        public void CreateUser(UserToCreate user)
        {
            try
            {
                Logger.Debug($"Создание пользователя {user.Login}");
                ConnectorService.CreateUser(user);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
    }
}
