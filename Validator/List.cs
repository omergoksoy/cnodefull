using NVG = Notus.Variable.Globals;
using System.Collections.Generic;

namespace Notus.Validator
{
    public static class List
    {
        public static Dictionary<Variable.Enum.NetworkLayer,
                Dictionary<Variable.Enum.NetworkType, List<Variable.Struct.IpInfo>>> Main
        =
        new Dictionary<Variable.Enum.NetworkLayer,
        Dictionary<Variable.Enum.NetworkType, List<Variable.Struct.IpInfo>>>()
        {
            {
                Variable.Enum.NetworkLayer.Layer1,
                    new Dictionary<Variable.Enum.NetworkType,List<Variable.Struct.IpInfo>>(){
                    {
                        Variable.Enum.NetworkType.MainNet,
                        new List<Variable.Struct.IpInfo>()
                        {
                            { new Variable.Struct.IpInfo() { IpAddress = "127.0.0.1", Port = 5000 } },
                            { new Variable.Struct.IpInfo() { IpAddress = "127.0.0.1", Port = 15000 } }
                        }
                    },
                    {
                        Variable.Enum.NetworkType.TestNet,
                        new List<Variable.Struct.IpInfo>()
                        {
                            { new Variable.Struct.IpInfo() { IpAddress = "127.0.0.1", Port = 5001 } },
                            { new Variable.Struct.IpInfo() { IpAddress = "127.0.0.1", Port = 15001 } }
                        }
                    },
                    {
                        Variable.Enum.NetworkType.DevNet,
                        new List<Variable.Struct.IpInfo>()
                        {
                            { new Variable.Struct.IpInfo() { IpAddress = "18.156.37.61", Port = 5002 } },
                            { new Variable.Struct.IpInfo() { IpAddress = "3.125.159.102", Port = 5002 } }
                        }
                    }
                }
            }
        };

        public static List<string> Get(Variable.Enum.NetworkLayer networkLayer, Variable.Enum.NetworkType networkType)
        {
            System.Collections.Generic.List<string> returnList = new();
            for (int i = 0; i < Notus.Validator.List.Main[networkLayer][networkType].Count; i++)
            {
                returnList.Add(Notus.Validator.List.Main[networkLayer][networkType][i].IpAddress);
            }
            return returnList;
        }
        public static void DefineLocalNodes()
        {
            Notus.Validator.List.Main.Clear();
            Notus.Validator.List.Main.Add(Variable.Enum.NetworkLayer.Layer1, new Dictionary<Variable.Enum.NetworkType, List<Variable.Struct.IpInfo>>(){
                {
                    Variable.Enum.NetworkType.MainNet,
                    new List<Variable.Struct.IpInfo>()
                    {
                        { new Variable.Struct.IpInfo() { IpAddress = NVG.Settings.IpInfo.Local, Port = 5000 } },
                        { new Variable.Struct.IpInfo() { IpAddress = NVG.Settings.IpInfo.Local, Port = 15000 } }
                    }
                },
                {
                    Variable.Enum.NetworkType.TestNet,
                    new List<Variable.Struct.IpInfo>()
                    {
                        { new Variable.Struct.IpInfo() { IpAddress = NVG.Settings.IpInfo.Local, Port = 5001 } },
                        { new Variable.Struct.IpInfo() { IpAddress = NVG.Settings.IpInfo.Local, Port = 15001 } }
                    }
                },
                {
                    Variable.Enum.NetworkType.DevNet,
                    new List<Variable.Struct.IpInfo>()
                    {
                        { new Variable.Struct.IpInfo() { IpAddress = NVG.Settings.IpInfo.Local, Port = 5002 } },
                        { new Variable.Struct.IpInfo() { IpAddress = NVG.Settings.IpInfo.Local, Port = 15002 } }
                    }
                }
            });
        }
    }
}
