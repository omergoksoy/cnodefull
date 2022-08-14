using System.Text.Json;

namespace Notus.Reward
{
    public class Block : IDisposable
    {
        // son blok tipinin oluşturulması zamanı
        public string LastTypeUid = string.Empty;

        // son empty blok zamanı
        public string LastBlockUid = string.Empty;

        public Queue<KeyValuePair<string, string>> RewardList = new Queue<KeyValuePair<string, string>>();
        private bool TimerIsRunning = false;
        private Notus.Threads.Timer TimerObj;
        public void Execute(
            Notus.Variable.Common.ClassSetting objSettings,
            System.Action<Notus.Variable.Class.BlockData>? Func_NewBlockIncome = null
        )
        {
            TimerObj = new Notus.Threads.Timer(60000);
            TimerObj.Start(() =>
            {
                if (TimerIsRunning == false)
                {
                    TimerIsRunning = true;
                    if (LastBlockUid.Length > 0 && LastTypeUid.Length > 0)
                    {
                        string tmpLastTypeStr=Notus.Block.Key.GetTimeFromKey(LastTypeUid).Substring(0, 17);
                        string tmpLastBlockStr=Notus.Block.Key.GetTimeFromKey(LastBlockUid).Substring(0, 17);
                        Console.WriteLine("tmpLastTypeStr : " + tmpLastTypeStr);
                        Console.WriteLine("tmpLastBlockStr : " + tmpLastBlockStr);
                    }
                    Console.WriteLine("LastTypeUid : " + LastTypeUid);
                    Console.WriteLine("LastBlockUid : " + LastBlockUid);
                    Console.WriteLine("RewardList.Count : " + RewardList.Count.ToString());
                    /*
                    Console.WriteLine(JsonSerializer.Serialize(RewardList));
                    //blok zamanı ve utc zamanı çakışıyor
                    DateTime tmpLastTime = Notus.Date.ToDateTime(
                        Obj_Settings.LastBlock.info.time
                    ).AddSeconds(howManySeconds);

                    // get utc time from validatır Queue
                    DateTime utcTime = ValidatorQueueObj.GetUtcTime();
                    if (utcTime > tmpLastTime)
                    {
                        if (ValidatorQueueObj.MyTurn)
                        {
                            if ((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds > 30)
                            {
                                //Console.WriteLine((DateTime.Now - EmptyBlockGeneratedTime).TotalSeconds);
                                EmptyBlockGeneratedTime = DateTime.Now;
                                Notus.Print.Success(Obj_Settings, "Empty Block Executed");
                                Obj_BlockQueue.AddEmptyBlock();
                            }
                            EmptyBlockNotMyTurnPrinted = false;
                        }
                        else
                        {
                            if (EmptyBlockNotMyTurnPrinted == false)
                            {
                                //Notus.Print.Warning(Obj_Settings, "Not My Turn For Empty Block");
                                EmptyBlockNotMyTurnPrinted = true;
                            }
                        }
                    }
                    */
                    TimerIsRunning = false;
                }
            }, true);
        }
        public Block()
        {
        }
        ~Block()
        {
            Dispose();
        }
        public void Dispose()
        {
            if (TimerObj != null)
            {
                TimerObj.Dispose();
            }
        }
    }
}
