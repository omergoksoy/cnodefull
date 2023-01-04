using System.Collections.Generic;

namespace Notus.Validator
{
    public static class List
    {
        // güzelhosting için oluşturulmuş liste
        public static readonly Dictionary<Variable.Enum.NetworkLayer,
                Dictionary<Variable.Enum.NetworkType, List<Variable.Struct.IpInfo>>> Main_GuzelHosting  //Main_GuzelHosting
            =
            new Dictionary<Variable.Enum.NetworkLayer,
            Dictionary<Variable.Enum.NetworkType, List<Variable.Struct.IpInfo>>>()
            {
                // layer 1
                {
                    Variable.Enum.NetworkLayer.Layer1,
                        new Dictionary<Variable.Enum.NetworkType,List<Variable.Struct.IpInfo>>(){
                        {
                            Variable.Enum.NetworkType.MainNet,
                            new List<Variable.Struct.IpInfo>()
                            {
                                {
                                    new Variable.Struct.IpInfo()
                                    {
                                        IpAddress = "89.252.134.91",
                                        Port = 5000
                                    }
                                },
                                {
                                    new Variable.Struct.IpInfo()
                                    {
                                        IpAddress = "89.252.184.151",
                                        Port = 5000
                                    }
                                }
                            }
                        },
                        {
                            Variable.Enum.NetworkType.TestNet,
                            new List<Variable.Struct.IpInfo>()
                            {
                                {
                                    new Variable.Struct.IpInfo()
                                    {
                                        IpAddress = "89.252.134.91",
                                        Port = 5001
                                    }
                                },
                                {
                                    new Variable.Struct.IpInfo()
                                    {
                                        IpAddress = "89.252.184.151",
                                        Port = 5001
                                    }
                                }
                            }
                        },
                        {
                            Variable.Enum.NetworkType.DevNet,
                            new List<Variable.Struct.IpInfo>()
                            {
                                {
                                    new Variable.Struct.IpInfo()
                                    {
                                        IpAddress = "89.252.134.91",
                                        Port = 5002
                                    }
                                },
                                {
                                    new Variable.Struct.IpInfo()
                                    {
                                        IpAddress = "89.252.184.151",
                                        Port = 5002
                                    }
                                }
                            }
                        }
                    }
                }
            };


        // aws için oluşturulmuş liste
        public static readonly Dictionary<Variable.Enum.NetworkLayer,
                Dictionary<Variable.Enum.NetworkType, List<Variable.Struct.IpInfo>>> Main //Main_AWS
        =
        new Dictionary<Variable.Enum.NetworkLayer,
        Dictionary<Variable.Enum.NetworkType, List<Variable.Struct.IpInfo>>>()
        {
            // layer 1
            {
                Variable.Enum.NetworkLayer.Layer1,
                    new Dictionary<Variable.Enum.NetworkType,List<Variable.Struct.IpInfo>>(){
                    {
                        Variable.Enum.NetworkType.MainNet,
                        new List<Variable.Struct.IpInfo>()
                        {
                            /*
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.68.233.67", Port = 5000 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.75.243.44", Port = 5000 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.125.159.102",Port = 5000 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "18.156.37.61", Port = 5000 }
                            },
                            */
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.75.110.186", Port = 5000 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "13.229.56.127", Port = 5000 }
                            }
                        }
                    },
                    {
                        Variable.Enum.NetworkType.TestNet,
                        new List<Variable.Struct.IpInfo>()
                        {
                            /*
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.68.233.67", Port = 5001 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.75.243.44", Port = 5001 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.125.159.102",Port = 5001 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "18.156.37.61", Port = 5001 }
                            },
                            */
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.75.110.186", Port = 5001 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "13.229.56.127", Port = 5001 }
                            }
                        }
                    },
                    {
                        Variable.Enum.NetworkType.DevNet,
                        new List<Variable.Struct.IpInfo>()
                        {
                            /*
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.68.233.67", Port = 5002 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.75.243.44", Port = 5002 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.125.159.102",Port = 5002 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "18.156.37.61", Port = 5002 }
                            },
                            */
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "3.75.110.186", Port = 5002 }
                            },
                            {
                                new Variable.Struct.IpInfo() { IpAddress = "13.229.56.127", Port = 5002 }
                            }
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
    }
}
