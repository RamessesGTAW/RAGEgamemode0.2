using System;
using GTANetworkAPI;
using System.IO;
using System.Reflection;
using MySqlConnector;
using System.Threading.Tasks;

namespace gamemode
{
    public class Main : Script
    {
        public static void ResourceStartLog(string resource)
        {
            NAPI.Util.ConsoleOutput($"[RESOURCE-START] {resource} has started successfully.");
        }
        public static void ServerLog(string message)
        {
            NAPI.Util.ConsoleOutput($"[SERVER-LOG] {message}");
        }

        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart()
        {
            ResourceStartLog("gamemode/Main");
            mysql.mysql.InitConnection();

        }

        [ServerEvent(Event.PlayerConnected)]
        public void OnPlayerConnect(Player player)
        {

            ServerLog($"{player.Name} has connected to the server.");
            PlayerData.Data PlayerObject = new PlayerData.Data(player);
            Vector3 pos = new Vector3(10, 10, 10);
            player.Position = pos;
            player.SetData<PlayerData.Data>(PlayerData.Data.Key, PlayerObject);


        }

        [ServerEvent(Event.PlayerDisconnected)]
        public async void OnPlayerDisconnected(Player player, DisconnectionType type, string reason)
        {
            //string sql = "UPDATE employees SET salary = @salary, department = @department WHERE id = @id;";

            PlayerData.Data PlayerObject = PlayerData.Data.GetDataFromPlayer(player);
            int SqlRow = PlayerObject.SqlRow;

            string SqlQuery = $"UPDATE `accounts` SET PosX = @PosX, PosY = @PosY, PosZ = @PosZ WHERE ID = @id;";
            using (MySqlCommand command = new MySqlCommand(SqlQuery, mysql.mysql.connection))
            {

                Vector3 pos = player.Position;
                command.Parameters.AddWithValue("@PosX", pos.X);
                command.Parameters.AddWithValue("@PosY", pos.Y);
                command.Parameters.AddWithValue("@PosZ", pos.Z);
                command.Parameters.AddWithValue("@id", SqlRow);

                try
                {
                    int rows = await command.ExecuteNonQueryAsync();
                    Main.ServerLog($"{rows} has been inserted. Location updated for {player.Name}");
                }
                catch (Exception e)
                {
                    Main.ServerLog($"{e}");
                }
            }

            if (player.HasData(PlayerData.Data.Key))
            {
                player.ResetData(PlayerData.Data.Key);
            }
        }
    }
}



namespace gamemode.mysql
{
    class mysql
    {
        public static bool IsMysqlConnected = false;
        public static MySqlConnection connection;
        public string Host { set; get; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DB { get; set; }

        public mysql()
        {
            this.Host = "127.0.0.1";
            this.Username = "root";
            this.Password = "";
            this.DB = "gamemode";
        }

        public static void InitConnection()
        {
            String FilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "sql.json");
            mysql sql = new mysql();

            if (File.Exists(FilePath))
            {
                Main.ServerLog("SQL json found, loading data.");
                String SQLConnection = $"Server={sql.Host};Password={sql.Password};User ID={sql.Username};Database={sql.DB}";
                connection = new MySqlConnection(SQLConnection);
                try
                {
                    connection.Open();
                    Main.ServerLog("MySQL connection has been established.");
                    IsMysqlConnected = true;
                }
                catch (Exception e)
                {
                    Main.ServerLog($"MySQL connection failed. {e}");
                }
            }
            else
            {
                Main.ServerLog("sql.json does not exist. Creating..");
                String SQLData = NAPI.Util.ToJson(sql);
                using (StreamWriter writer = new StreamWriter(FilePath))
                {
                    writer.WriteLine(SQLData);
                }
                Main.ServerLog("sql.info has been created. Loading data");
                InitConnection();
            }
        }
    }
}

namespace gamemode.accounts
{
    class accounts : Script
    {

        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart()
        {
            Main.ResourceStartLog("accounts.");
        }

        [ServerEvent(Event.PlayerConnected)]
        public void OnPlayerConnected(Player player)
        {
            player.Transparency = 0;
            Vector3 pos = new Vector3(218, -948, 900);
            player.Position = pos;
            NAPI.ClientEvent.TriggerClientEvent(player, "ShowLoginCEF", true);
            NAPI.ClientEvent.TriggerClientEvent(player, "ShowNativeGUI", false);
        }


        // REGISTER 

        [RemoteEvent("SendRegisterInfoToServerFromClient")]
        public async void SendRegisterInfoToServerFromClient(Player player, string username, string password)
        {
            /*  player.SendChatMessage($"Username: {username}, Password: {password}");
              Main.ServerLog($"{player.Name} tried to register as {username}:{password}.");
              NAPI.ClientEvent.TriggerClientEvent(player, "ShowRegisterCEF", false);
              NAPI.ClientEvent.TriggerClientEvent(player, "ShowNativeGUI", true);*/

            string SqlQuery = $"INSERT INTO `accounts` (Username, Password, PosX, PosY, PosZ) VALUES (@Username, @Password, @PosX, @PosY, @PosZ)";
            using (MySqlCommand command = new MySqlCommand(SqlQuery, mysql.mysql.connection))
            {
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);
                Vector3 pos = new Vector3(-70.213, 9.23, 71.825966);
                command.Parameters.AddWithValue("@PosX", pos.X);
                command.Parameters.AddWithValue("@PosY", pos.Y);
                command.Parameters.AddWithValue("@PosZ", pos.Z);

                try
                {
                    int rows = await command.ExecuteNonQueryAsync();
                    Main.ServerLog($"{rows} has been inserted. {username}");
                }
                catch (Exception e)
                {
                    Main.ServerLog($"{e}");
                }
            }

            NAPI.ClientEvent.TriggerClientEvent(player, "ShowRegisterCEF", false);
            NAPI.ClientEvent.TriggerClientEvent(player, "ShowLoginCEF", true);



        }



        // LOGIN 
        [RemoteEvent("SendLoginInfoToServerFromClient")]
        public async void SendLoginInfoToServerFromClient(Player player, string username, string password)
        {
            string SqlQuery = $"SELECT * FROM `accounts` WHERE `Username` = @Username LIMIT 1;";
            using (MySqlCommand command = new MySqlCommand(SqlQuery, mysql.mysql.connection))
            {
                command.Parameters.AddWithValue("@Username", username);

                using (var reader = await command.ExecuteReaderAsync())
                {


                    if (await reader.ReadAsync())
                    {
                        if (Convert.ToString(reader["password"]) == password)
                        {
                            // correct password logic
                            PlayerData.Data NewPlayer = PlayerData.Data.GetDataFromPlayer(player);
                            NewPlayer.SqlRow = Convert.ToInt32(reader["ID"]);
                            NewPlayer.Username = Convert.ToString(reader["Username"]);
                            NewPlayer.Password = Convert.ToString(reader["Password"]);
                            NewPlayer.AdminLevel = Convert.ToInt32(reader["AdminLevel"]);
                            NewPlayer.CharacterName = Convert.ToString(reader["CharacterName"]);
                            NewPlayer.Cash = Convert.ToInt32(reader["Cash"]);
                            NewPlayer.PlayerNotes = Convert.ToString(reader["PlayerNotes"]);
                            NewPlayer.Health = Convert.ToInt32(reader["Health"]);
                            Vector3 pos = new Vector3();

                            pos.X = (float)reader["PosX"];
                            pos.Y = (float)reader["PosY"];
                            pos.Z = (float)reader["PosZ"];
                            NewPlayer.Position = pos;
                            NewPlayer.IsLoggedIn = true;

                            if (player.HasData(PlayerData.Data.Key))
                            {
                                player.ResetData(PlayerData.Data.Key);
                            }
                            player.SetData<PlayerData.Data>(PlayerData.Data.Key, NewPlayer);
                            player.Transparency = 255;
                            NAPI.Player.SpawnPlayer(player, pos);
                            NAPI.ClientEvent.TriggerClientEvent(player, "ShowLoginCEF", false);
                            NAPI.ClientEvent.TriggerClientEvent(player, "ShowNativeGUI", true);

                        }
                        else
                        {
                            NAPI.ClientEvent.TriggerClientEvent(player, "JSInvalidLoginInfo");
                        }
                    }
                    else
                    {
                        NAPI.ClientEvent.TriggerClientEvent(player, "JSInvalidLoginInfo");
                    }
                }
            }

        }
    }
}

namespace gamemode.PlayerData
{
    class Core : Script
    {
        [ServerEvent(Event.ResourceStart)]
        public void OnResourceStart()
        {
            Main.ResourceStartLog("PlayerData");
            String SQLString =

                   $"CREATE TABLE IF NOT EXISTS `accounts` (" +
                   $"`ID` INT(11) NOT NULL AUTO_INCREMENT," +
                   $"`Username` VARCHAR(64) NOT NULL DEFAULT \"\"," +
                   $"`password` VARCHAR(64) NOT NULL DEFAULT \"\"," +
                   $"`AdminLevel` INT(11) NOT NULL DEFAULT '0'," +
                   $"`CharacterName` INT(11) NOT NULL DEFAULT '0'," +
                   $"`Cash` INT(11) NOT NULL DEFAULT '0'," +
                   $"`PlayerNotes` VARCHAR(255) NOT NULL DEFAULT \"None.\"," +
                   $"`Health` INT(11) NOT NULL DEFAULT '0'," +
                   $"`PosX` FLOAT NOT NULL DEFAULT '0'," +
                   $"`PosY` FLOAT NOT NULL DEFAULT '0'," +
                   $"`PosZ` FLOAT NOT NULL DEFAULT '0'," +
                   $"UNIQUE (Username)," +
                   $"PRIMARY KEY (ID));";
            try
            {
                using (MySqlCommand command = new MySqlCommand(SQLString, mysql.mysql.connection))
                {
                    command.ExecuteNonQuery();
                    Main.ServerLog("CORE SQL Query succeeded.");
                }
            }
            catch (Exception e)
            {
                Main.ServerLog($"Sql Query failed. ref(Core.cs) ({e}");
            }


        }
    }
}

namespace gamemode.PlayerData
{
    class Data
    {
        public static readonly string Key = "PlayerInfoKeyIdentifier";
        public Player ThePlayerData { set; get; }
        public int SqlRow { set; get; }
        public String Username { set; get; }
        public String Password { set; get; }
        public int AdminLevel { set; get; }
        public String CharacterName { set; get; }
        public int Cash { set; get; }
        public String PlayerNotes { set; get; }
        public int Health { set; get; }
        public bool IsLoggedIn { set; get; }
        public Vector3 Position { set; get; }


        public Data(Player player)
        {
            this.ThePlayerData = player;
            this.Username = null;
            this.Password = null;
            this.AdminLevel = 0;
            this.CharacterName = null;
            this.Cash = 0;
            this.PlayerNotes = "None.";
            this.Health = 100;
            this.IsLoggedIn = false;
            Vector3 pos = new Vector3();
        }

        public static async void UpdatePlayerAccountMysql(Data ThePlayer)
        {
            string SqlQuery = $"UPDATE `accounts` SET Username = @Username, password = @password, AdminLevel = @AdminLevel, CharacterName = @CharacterName, Cash = @Cash, PlayerNotes = @PlayerNotes, Health = @Health, PosX = @PosX, PosY = @PosY, PosZ = @PosZ WHERE ID = @id;";
            using (MySqlCommand command = new MySqlCommand(SqlQuery, mysql.mysql.connection))
            {
                command.Parameters.AddWithValue("@id", ThePlayer.SqlRow);
                command.Parameters.AddWithValue("@Username", ThePlayer.Username);
                command.Parameters.AddWithValue("@password", ThePlayer.Password);
                command.Parameters.AddWithValue("@AdminLevel", ThePlayer.AdminLevel);
                command.Parameters.AddWithValue("@CharacterName", ThePlayer.Username);
                command.Parameters.AddWithValue("@Cash", ThePlayer.Cash);
                command.Parameters.AddWithValue("@PlayerNotes", ThePlayer.PlayerNotes);
                command.Parameters.AddWithValue("@Health", ThePlayer.Health);
                command.Parameters.AddWithValue("@PosX", ThePlayer.Position.X);
                command.Parameters.AddWithValue("@PosY", ThePlayer.Position.Y);
                command.Parameters.AddWithValue("@PosZ", ThePlayer.Position.Z);

                try
                {
                    int rows = await command.ExecuteNonQueryAsync();
                    Main.ServerLog($"Amount {rows} rows has been updated.  Full Update for: {ThePlayer.Username} | SQLID: {ThePlayer.SqlRow}");
                }
                catch (Exception e)
                {
                    Main.ServerLog($"{e}");
                }
            }
        }

        public void SetHealth(int healthAmount)
        {
            this.Health = healthAmount;
            ThePlayerData.Health = healthAmount;
        }
        public static void SetCash(Player player, Data PlayerObject, int CashAmount)
        {
            PlayerObject.Cash = CashAmount;
            NAPI.ClientEvent.TriggerClientEvent(player, "SetPlayerCash", CashAmount);
        }
        public void SetAdminLevel(int level)
        {
            this.AdminLevel = level;
        }

        public static Data GetDataFromPlayer(Player player)
        {
            if (player == null)
                return null;
            if (player.HasData(Key))
            {
                return player.GetData<Data>(Key);
            }
            else
            {
                Data NewPlayer = new Data(player);
                player.SetData<Data>(Key, NewPlayer);
                return NewPlayer;

            }
        }

        public async Task<bool> AccountExists()
        {
            String query = $"SELECT COUNT(*) AS AccNo FROM `accounts` WHERE `Username` = @AccountUsername LIMIT 1;";
            using (MySqlCommand command = new MySqlCommand(query, mysql.mysql.connection))
            {
                command.Parameters.AddWithValue("@AccountUsername", this.Username);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        if (Convert.ToInt32(reader["AccNo"]) == 1)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }



    }
}

namespace gamemode.commands
{
    class AdminCommands : Script
    {
        [Command("getpos", Alias = "xyz")]
        public void GetPosCommand(Player player)
        {
            PlayerData.Data PlayerObject = PlayerData.Data.GetDataFromPlayer(player);
            if (PlayerObject.AdminLevel > 1)
            {
                player.SendChatMessage($"Position: {player.Position}");
                NAPI.Util.ConsoleOutput($"Admin Level {PlayerObject.AdminLevel} {PlayerObject.Username} executed 'getpos' command");
            }
            else
            {
                player.SendChatMessage("Insufficient permissions.");
            }
        }

        [Command("claimadmin", Alias = "ca", GreedyArg = true)]

        public void ClaimAdminCommand(Player player, string key)
        {
            PlayerData.Data PlayerObject = PlayerData.Data.GetDataFromPlayer(player);
            string CorrectKey = "IOwnYou";
            if (key == CorrectKey)
            {
                if (PlayerObject.IsLoggedIn && PlayerObject.AdminLevel < 5)
                {
                    PlayerObject.AdminLevel = 5;
                    player.SetData<PlayerData.Data>(PlayerData.Data.Key, PlayerObject);
                    player.SendChatMessage($"You have claimed your Admin Level 5.");
                    NAPI.Util.ConsoleOutput($"{PlayerObject.Username} has claimed Admin Level 5 using \"{key}\"");
                    PlayerData.Data.UpdatePlayerAccountMysql(PlayerObject);


                }
                else
                {
                    player.SendChatMessage($"You already have Admin Level 5.");
                }
            }
            else
            {
                return;
            }
        }

        [Command("giveweapon", Alias = "gw")]
        public void GiveWeaponCommand(Player player, Player target, WeaponHash WeaponHash, int ammo)
        {
            PlayerData.Data PlayerObject = PlayerData.Data.GetDataFromPlayer(player);
            PlayerData.Data TargetObject = PlayerData.Data.GetDataFromPlayer(target);
            if (PlayerObject.IsLoggedIn && PlayerObject.AdminLevel > 3)
            {
                if (TargetObject.IsLoggedIn)
                {
                    target.GiveWeapon(WeaponHash, ammo);
                    player.SendChatMessage($"You have given a {WeaponHash} to {TargetObject.Username} with x{ammo} Ammo.");
                    target.SendChatMessage($"You have recieved a {WeaponHash} with x{ammo} Ammo from Admin Level {PlayerObject.AdminLevel} {PlayerObject.Username}");
                }
                else
                {
                    player.SendChatMessage($"Player is not logged in.");
                }
            }
            else
            {
                player.SendChatMessage("Insufficent permissions.");
            }
        }
        [Command("setcash", Alias = "setmoney")]
        public void SetCashCommand(Player player, Player target, int CashAmount)
        {
            PlayerData.Data PlayerObject = PlayerData.Data.GetDataFromPlayer(player);
            PlayerData.Data TargetObject = PlayerData.Data.GetDataFromPlayer(target);
            if (PlayerObject.IsLoggedIn && PlayerObject.AdminLevel > 3)
            {
                if (TargetObject.IsLoggedIn)
                {
                    PlayerData.Data.SetCash(target, TargetObject, CashAmount);
                    PlayerData.Data.UpdatePlayerAccountMysql(TargetObject);
                    player.SendChatMessage($"You have given ${CashAmount} to {TargetObject.Username}.");
                    target.SendChatMessage($"You have recieved ${CashAmount} from Admin Level {PlayerObject.AdminLevel} {PlayerObject.Username}");
                }
                else
                {
                    player.SendChatMessage($"Player is not logged in.");
                }
            }
            else
            {
                player.SendChatMessage("Insufficent permissions.");
            }

        }
    }
}