using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
namespace Lottery
{
    [ApiVersion(1, 14)]
    public class Lottery : TerrariaPlugin
    {
        public override string Author
        {
            get { return "Drenferalis"; }
        }

        public override string Description
        {
            get { return "Adds a playable raffle like lottery."; }
        }
        public override string Name
        {
            get { return "Lottery"; }
        }


        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public Lottery(Main game)
            : base(game)
        {
        }
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                timer.Dispose();
            }
        }
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
        }
        private Timer timer;
        private int interval = 1;
        private bool firstrun = true;
        private int iid;
        private Item item;
        private int prefixId = 0;
        private int itemAmount;
        private Random random = new Random();
        private List<int> users = new List<int>();


        void AGenItems(CommandArgs e)
        {
            iid = random.Next(1, 1600);
            List<Item> matchedItems = TShock.Utils.GetItemByIdOrName(iid.ToString());
            item = matchedItems[0];
            prefixId = random.Next(1, 82);
            itemAmount = item.maxStack;
            e.Player.SendInfoMessage("New Item Generated");
            TSPlayer.All.SendInfoMessage(String.Format("{3} has changed the next lottery item! The next lottery will be for {0} {1}{2}!", itemAmount.ToString(), item.name, itemAmount == 1 ? "" : "s", e.Player.Name.ToString()));
        }
        void LGenItems()
        {
            iid = random.Next(1, 1600);
            List<Item> matchedItems = TShock.Utils.GetItemByIdOrName(iid.ToString());
            item = matchedItems[0];
            prefixId = random.Next(1, 82);
            itemAmount = item.maxStack;
            TSPlayer.All.SendInfoMessage(String.Format("The next lottery will be for {0} {1}{2}!", itemAmount.ToString(), item.name, itemAmount == 1 ? "" : "s"));
        }
        void OnElapsed(object sender, ElapsedEventArgs e)
        {
            if (firstrun)
            {
                firstrun = false;
                interval = 15;
                TSPlayer.All.SendInfoMessage("Lottery Enabled!");
                TSPlayer.All.SendInfoMessage("Type /le to spend 20 Max Life to enter the lottery!");
                TSPlayer.All.SendInfoMessage("You can use /le # to spend 20 Max Life for each ticket you buy!");
                timer.Interval = (60 * interval * 1000);
                LGenItems();
            }
            else
            {
                TSPlayer.All.SendInfoMessage("Lottery Time!");
                TSPlayer.All.SendInfoMessage("Type /le to spend 20 Max Life to enter the lottery!");
                TSPlayer.All.SendInfoMessage("You can use /le # to spend 20 Max Life for each ticket you buy!");
                timer.Interval = (60 * interval * 1000);
                //Do stuff
                //Pull from DB
                if (users.Count > 0)
                {
                    int winner = users[random.Next(0, users.Count - 1)];

                    //Truncate DB
                    users.Clear();
                    //Announce Winners
                    TSPlayer.All.SendInfoMessage(String.Format("{0} won the lottery!", TShock.Players[winner].Name));
                    //Distribute Items
                    TShock.Players[winner].GiveItemCheck(item.type, item.name, item.width, item.height, itemAmount, prefixId);
                }
                else {
                    TSPlayer.All.SendInfoMessage("No one entered last round!");
                    
                }
                //Generate New Items
                LGenItems();

                //End
            }
        }
        void chance(CommandArgs e)
        {
            if (users.Count(x => x == e.Player.Index) <= users.Count() && users.Count() > 0)
            {
                float chances = (users.Count(x => x == e.Player.Index) / users.Count()) * 100;
                e.Player.SendInfoMessage(String.Format("You have a {0}% of winning right now.", chances));
            }
            else
            {
                e.Player.SendInfoMessage(String.Format("You have a 0% of winning right now."));
            }
        }
        void setInterval(CommandArgs e)
        {
            if(e.Parameters.Count > 0){
                short number;
                bool result = Int16.TryParse(e.Parameters[0], out number);
                if(result)
                {
                    if (number > 0)
                    {

                        interval = number;
                        e.Player.SendSuccessMessage(string.Format("Interval changed to {0} minutes. Timer will update after next drawing.", number));
                        return;
                    }
                }
                
            }
            e.Player.SendErrorMessage("There was an error trying to change the timer.");
        }
       void enter (CommandArgs e)
          {
            if (e.Parameters.Count > 0)
            {
                short number;
                bool result = Int16.TryParse(e.Parameters[0], out number);
                if(result)
                {
                    if (number < 0)
                    {
                        e.TPlayer.KillMe(1000,1,false," tried to cheat the system");
                        NetMessage.SendData(16, e.Player.Index, -1, "", e.Player.Index, 0f, 0f, 0f, 0);
                        return;
                    }

                    int ammount = number;
                    int index = e.Player.Index;
                
                    if (e.TPlayer.statLifeMax - (ammount * 20) >= 100)
                    {
                        e.TPlayer.statLifeMax -= ammount * 20;
                        NetMessage.SendData(16, e.Player.Index, -1, "", e.Player.Index, 0f, 0f, 0f, 0);
                        e.Player.SaveServerCharacter();
                    
                        for (int i = 0; i < ammount; i++)
                        {
                            users.Add(e.Player.Index);
                        }
                        e.Player.SendSuccessMessage(String.Format("Entered the lottery {0} times!", ammount.ToString()));
                    }
                    else
                    {
                        e.Player.SendErrorMessage("Failed to enter the lottery");
                    }
                }
                else
                {
                    e.Player.SendErrorMessage("Failed to enter the lottery");
                }
            }
            else
            {
                if (e.TPlayer.statLifeMax - 20 >= 100)
                {
                    e.TPlayer.statLifeMax -=  20; //Make Mods
                    NetMessage.SendData(16, e.Player.Index, -1, "", e.Player.Index, 0f, 0f, 0f, 0);
                    e.Player.SaveServerCharacter();
                    users.Add(e.Player.Index);
                    e.Player.SendSuccessMessage("You entered the lottery!");
                }
                else
                {
                    e.Player.SendErrorMessage("Failed to enter the lottery");
                }
            }
        }
        void OnInitialize(EventArgs e)
        {
            Commands.ChatCommands.Add(new Command("lottery.changeitem", AGenItems, "lotterynew","ln"));
            Commands.ChatCommands.Add(new Command("", enter, "lotteryenter","le"));
            Commands.ChatCommands.Add(new Command("", chance, "lotterychance", "lc"));
            Commands.ChatCommands.Add(new Command("lottery.admin", setInterval, "lotteryinterval", "li"));
            
            timer = new Timer(60 * interval * 1000);
            timer.Elapsed += OnElapsed;
            timer.Start();
        }

    }
}
