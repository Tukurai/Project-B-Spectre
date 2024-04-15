using Common.DAL;
using Common.DAL.Models;
using Common.Enums;

namespace Common.Services
{
    public class UserService : BaseService
    {
        public SettingsService Settings { get; }
        private LocalizationService Localization { get; }

        public UserService(DepotContext context, SettingsService settings, LocalizationService localization)
            : base(context)
        {
            Localization = localization;
            Settings = settings;
        }

        public (bool Valid, string Message) ValidateUserpass(int userpass)
        {
            var user = Context.Users.FirstOrDefault(user => user.Id == userpass);

            if (user == null)
                return new(false, Localization.Get("User_does_not_exist"));

            return new(true, Localization.Get("User_is_valid"));
        }

        public (bool Valid, string Message) ValidateUserForRole(int userpass, Role allowedRole) => ValidateUserForRole(GetUser(userpass), allowedRole);

        public (bool Valid, string Message) ValidateUserForRole(User? user, Role allowedRole)
        {
            if (user == null)
                return new(false, Localization.Get("User_does_not_exist"));

            if (user.Role < (int)allowedRole)
                return new(false, Localization.Get("User_has_no_access"));

            return new(true, Localization.Get("User_has_access"));
        }

        public User? GetUser(int userpass)
        {
            return Context.Users.Find(userpass);
        }

        public List<User> GetAllUsers()
        {
            return [.. Context.Users];
        }
    }
}
