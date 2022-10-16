﻿using System.Collections.Generic;

namespace Notus.Validator
{
    //standart güzel hosting sunucuları ile yapılan ayarlar
    public static class List
    {
        // omergoksoy
        // burada liste constant.ListMainNodeIp sabitinden alınıp List içeriğine eklenecek.
        public static readonly Dictionary<Variable.Enum.NetworkLayer,
                Dictionary<Variable.Enum.NetworkType, List<Variable.Struct.IpInfo>>> Main
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
                                        IpAddress = "89.252.134.111",
                                        Port = 5000
                                    }
                                },
                                {
                                    new Variable.Struct.IpInfo()
                                    {
                                        IpAddress = "89.252.159.87",
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
                                        IpAddress = "89.252.134.111",
                                        Port = 5001
                                    }
                                },
                                {
                                    new Variable.Struct.IpInfo()
                                    {
                                        IpAddress = "89.252.159.87",
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
                                        IpAddress = "89.252.134.111",
                                        Port = 5002
                                    }
                                },
                                {
                                    new Variable.Struct.IpInfo()
                                    {
                                        IpAddress = "89.252.159.87",
                                        Port = 5002
                                    }
                                }
                            }
                        }
                    }
                }
            };
    }
}
