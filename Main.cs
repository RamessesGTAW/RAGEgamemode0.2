using System;
using RAGE;

namespace AccountPanel
{
    public class Main : Events.Script
    {
        RAGE.Ui.HtmlWindow LoginCEF = null;
        RAGE.Ui.HtmlWindow RegisterCEF = null;

        public Main()
        {
            LoginCEF = new RAGE.Ui.HtmlWindow("package://login.html");
            RegisterCEF = new RAGE.Ui.HtmlWindow("package://register.html");
            LoginCEF.Active = false;
            RegisterCEF.Active = false;
            Events.Add("ShowRegisterCEF", ShowRegisterCEF);
            Events.Add("ShowLoginCEF", ShowLoginCEF);
            Events.Add("ShowNativeGUI", ShowNativeGUI);
            Events.Add("SendRegisterInfoToClientFromCEF", SendRegisterInfoToClientFromCEF);
            Events.Add("SendLoginInfoToClientFromCEF", SendLoginInfoToClientFromCEF);
            Events.Add("JSInvalidLoginInfo", JSInvalidLoginInfo);
            Events.Add("SetPlayerCash", SetPlayerCash);

        }

        public void ShowRegisterCEF(object[] args)
        {
            bool flag = (bool)args[0];
            RegisterCEF.Active = flag;
            RAGE.Elements.Player.LocalPlayer.FreezePosition(flag);
        }
        public void ShowLoginCEF(object[] args)
        {
            bool flag = (bool)args[0];
            LoginCEF.Active = flag;
            RAGE.Elements.Player.LocalPlayer.FreezePosition(flag);

        }
        public void ShowNativeGUI(object[] args)
        {
            bool flag = (bool)args[0];
            RAGE.Ui.Cursor.Visible = !flag;
            Chat.Show(flag);
            RAGE.Game.Ui.DisplayRadar(flag);
        }

        public void SendRegisterInfoToClientFromCEF(object[] args)
        {
            RAGE.Events.CallRemote("SendRegisterInfoToServerFromClient", (string)args[0], (string)args[1]);
        }

        public void SendLoginInfoToClientFromCEF(object[] args)
        {
            RAGE.Events.CallRemote("SendLoginInfoToServerFromClient", (string)args[0], (string)args[1]);
        }
        public void JSInvalidLoginInfo(object[] args)
        {
            LoginCEF.ExecuteJs("invalidLoginInfo()");
        }

        public void SetPlayerCash(object[] args)
        {
            RAGE.Elements.Player.LocalPlayer.SetMoney((int)args[0]);
        }
    }
}
