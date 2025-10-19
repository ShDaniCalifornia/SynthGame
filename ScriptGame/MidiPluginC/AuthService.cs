using Godot;
using Microsoft.EntityFrameworkCore;
using Synthesizer.Database.Contexts;
using Synthesizer.Database.Models;

namespace Synthesizer.ScriptGame.MidiPluginC
{
    public partial class AuthService : Node
    {
        [Signal]
        public delegate void LoginResultEventHandler(bool success, int userId, string username, int level, int exp);

        [Signal]
        public delegate void RegisterResultEventHandler(bool success, int userId, string username, int level, int exp);

        private SynthesizerContext _db = new SynthesizerContext();

        public async void TryLogin(string login, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == login && u.Password == password);
            if (user != null)
            {
                EmitSignal("LoginResult", true, user.IDUsers, user.Username, user.Level, user.Exp);
            }
            else
            {
                EmitSignal("LoginResult", false, 0, "", 0, 0);
            }
        }

        public async void RegisterUser(string username, string login, string password)
        {
            var exists = await _db.Users.AnyAsync(u => u.Login == login);
            if (exists)
            {
                EmitSignal("RegisterResult", false, 0, "", 0, 0);
                return;
            }

            var newUser = new Users
            {
                Login = login,
                Password = password,
                Username = username,
                Exp = 0,
                Level = 1,
                Avatar = null
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            EmitSignal("RegisterResult", true, newUser.IDUsers, newUser.Username, newUser.Level, newUser.Exp);
        }
    }
}
