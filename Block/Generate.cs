using NVC = Notus.Variable.Constant;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Notus.Block
{
    public class Generate : IDisposable
    {
        private string ValidatorWalletKeyStr = "validatorKey";
        public string ValidatorWalletKey
        {
            set
            {
                ValidatorWalletKeyStr = value;
            }
            get
            {
                return ValidatorWalletKeyStr;
            }
        }
        
        private Notus.Variable.Class.BlockData FillValidatorKeyData(Notus.Variable.Class.BlockData BlockData)
        {
            using (Notus.Nonce.Calculate CalculateObj = new Notus.Nonce.Calculate())
            {
                BlockData.validator.map.block.Clear();
                BlockData.validator.map.data.Clear();
                BlockData.validator.map.info.Clear();
                BlockData.validator.count.Clear();

                int HowManyNonceStep = CalculateObj.NonceStepCount(
                                    BlockData.info.nonce.type,
                                    BlockData.info.nonce.method,
                                    BlockData.info.nonce.difficulty
                                );
                BlockData.validator.map.block.Add(1000, ValidatorWalletKeyStr);
                BlockData.validator.map.data.Add(1000, ValidatorWalletKeyStr);
                BlockData.validator.map.info.Add(1000, ValidatorWalletKeyStr);
                BlockData.validator.count.Add(ValidatorWalletKeyStr, (HowManyNonceStep * 3));
            }
            return BlockData;
        }
        private string GenerateNonce(string PureTextForNonce, Notus.Variable.Class.BlockData BlockData)
        {
            if (BlockData.info.nonce.type == 1)
            {
                return new Notus.Nonce.Calculate().Slide(
                    BlockData.info.nonce.method,
                    PureTextForNonce,
                    BlockData.info.nonce.difficulty
                );
            }
            return new Notus.Nonce.Calculate().Bounce(
                BlockData.info.nonce.method,
                PureTextForNonce,
                BlockData.info.nonce.difficulty
            );
        }

        private bool CheckValidNonce(string PureTextForNonce, Notus.Variable.Class.BlockData BlockData, string NonceValueStr)
        {
            if (BlockData.info.nonce.type == 1)
            {
                return new Notus.Nonce.Verify().Slide(
                    BlockData.info.nonce.method,
                    PureTextForNonce,
                    BlockData.info.nonce.difficulty,
                    NonceValueStr
                );
            }
            else
            {
                return new Notus.Nonce.Verify().Bounce(
                    BlockData.info.nonce.method,
                    PureTextForNonce,
                    BlockData.info.nonce.difficulty,
                    NonceValueStr
                );
            }
        }
        private Notus.Variable.Class.BlockData Make_Info(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = FirstString_Info(BlockData);
            BlockData.nonce.info = GenerateNonce(TmpText, BlockData);
            Notus.HashLib.Sasha hashObj = new Notus.HashLib.Sasha();
            BlockData.hash.info = hashObj.ComputeHash(
                TmpText + NVC.Delimeter + BlockData.nonce.info,
                false
            );
            return BlockData;
        }

        private Notus.Variable.Class.BlockData Make_Validator(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = FirstString_Validator(BlockData);
            Notus.Hash hashObj = new Notus.Hash();
            BlockData.validator.sign = hashObj.CommonHash("sasha", TmpText);

            return BlockData;
        }
        private Notus.Variable.Class.BlockData Make_Data(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = BlockData.cipher.data + NVC.Delimeter + BlockData.cipher.ver;
            Notus.Hash hashObj = new Notus.Hash();

            BlockData.cipher.sign = hashObj.CommonHash("sasha", TmpText);

            TmpText = TmpText + NVC.Delimeter + BlockData.cipher.sign;

            BlockData.nonce.data = GenerateNonce(TmpText, BlockData);

            BlockData.hash.data = new Notus.HashLib.Sasha().ComputeHash(
                TmpText + NVC.Delimeter + BlockData.nonce.data,
                false
            );
            return BlockData;
        }

        private Notus.Variable.Class.BlockData Make_Block(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = BlockData.hash.data + NVC.Delimeter + BlockData.hash.info;
            BlockData.nonce.block = GenerateNonce(TmpText, BlockData);
            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();
            BlockData.hash.block = sashaObj.ComputeHash(
                TmpText + NVC.Delimeter + BlockData.nonce.block,
                false
            );
            return BlockData;
        }

        private Notus.Variable.Class.BlockData Make_FINAL(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = FirstString_Block(BlockData);
            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();

            string sashaText = sashaObj.ComputeHash(TmpText, true);
            string base64Text=Notus.Convert.HexToBase64(sashaText);

            burada Once Final hash'ini azalt ve base64'e çevir
            sonra da sırasıyla diğerlerini de çevir

            Console.WriteLine(sashaText);
            Console.WriteLine(base64Text);

            BlockData.hash.FINAL = sashaObj.ComputeHash(
                TmpText,
                false
            );
            Notus.HashLib.Sasha sashaObj2 = new Notus.HashLib.Sasha();
            BlockData.sign = sashaObj2.ComputeHash(
                BlockData.hash.info + NVC.Delimeter +
                BlockData.hash.data + NVC.Delimeter +
                BlockData.hash.block + NVC.Delimeter +
                BlockData.hash.FINAL,
                false
            );
            return BlockData;
        }

        public Notus.Variable.Class.BlockData Make(Notus.Variable.Class.BlockData BlockData, int BlockVersion = 1000)
        {
            BlockData.info.version = BlockVersion;
            if (BlockVersion == 1000)
            {
                if (BlockData.info.nonce.method == 0)
                {
                    BlockData.info.nonce.method = 1;
                }
                else
                {
                    if (Notus.Variable.Constant.NonceHashLength.ContainsKey(BlockData.info.nonce.method) == false)
                    {
                        BlockData.info.nonce.method = 1;
                    }
                }

                if (BlockData.info.nonce.difficulty == 0)
                {
                    BlockData.info.nonce.difficulty = 1;
                }

                BlockData = FillValidatorKeyData(BlockData);
                BlockData = Make_Data(BlockData);
                BlockData = Make_Info(BlockData);
                BlockData = Make_Block(BlockData);
                BlockData = Make_Validator(BlockData);
                BlockData = Make_FINAL(BlockData);
            }

            return BlockData;
        }


        public bool Verify(Notus.Variable.Class.BlockData BlockData)
        {
            string TmpText = BlockData.cipher.data + NVC.Delimeter + BlockData.cipher.ver;
            Notus.Hash hashObj = new Notus.Hash();
            string ControlStr = hashObj.CommonHash("sasha", TmpText);
            if (string.Equals(BlockData.cipher.sign, ControlStr) == false)
            {
                return false;
            }
            TmpText = TmpText + NVC.Delimeter + BlockData.cipher.sign;
            if (CheckValidNonce(TmpText, BlockData, BlockData.nonce.data) == false)
            {
                return false;
            }
            Notus.HashLib.Sasha sashaObj = new Notus.HashLib.Sasha();
            ControlStr = sashaObj.ComputeHash(
                TmpText + NVC.Delimeter + BlockData.nonce.data,
                false
            );
            if (string.Equals(BlockData.hash.data, ControlStr) == false)
            {
                return false;
            }
            TmpText = FirstString_Info(BlockData);
            if (CheckValidNonce(TmpText, BlockData, BlockData.nonce.info) == false)
            {
                return false;
            }
            Notus.HashLib.Sasha sashaObj2 = new Notus.HashLib.Sasha();
            ControlStr = sashaObj2.ComputeHash(
                TmpText + NVC.Delimeter + BlockData.nonce.info,
                false
            );
            if (string.Equals(BlockData.hash.info, ControlStr) == false)
            {
                return false;
            }
            TmpText = BlockData.hash.data + NVC.Delimeter + BlockData.hash.info;
            if (CheckValidNonce(TmpText, BlockData, BlockData.nonce.block) == false)
            {
                return false;
            }
            Notus.HashLib.Sasha sashaObj3 = new Notus.HashLib.Sasha();
            ControlStr = sashaObj3.ComputeHash(
                TmpText + NVC.Delimeter + BlockData.nonce.block,
                false
            );
            if (string.Equals(BlockData.hash.block, ControlStr) == false)
            {
                return false;
            }
            TmpText = FirstString_Validator(BlockData);
            Notus.Hash hashObj2 = new Notus.Hash();
            ControlStr = hashObj2.CommonHash("sasha", TmpText);
            if (string.Equals(BlockData.validator.sign, ControlStr) == false)
            {
                return false;
            }

            TmpText = FirstString_Block(BlockData);
            Notus.HashLib.Sasha sashaObj4 = new Notus.HashLib.Sasha();
            ControlStr = sashaObj4.ComputeHash(TmpText, false);
            if (string.Equals(BlockData.hash.FINAL, ControlStr) == false)
            {
                return false;
            }
            Notus.HashLib.Sasha sashaObj5 = new Notus.HashLib.Sasha();
            ControlStr = sashaObj5.ComputeHash(
                BlockData.hash.info + NVC.Delimeter +
                BlockData.hash.data + NVC.Delimeter +
                BlockData.hash.block + NVC.Delimeter +
                BlockData.hash.FINAL,
                false
            );
            if (string.Equals(BlockData.sign, ControlStr) == false)
            {
                return false;
            }
            return true;
        }




        private string FirstString_Block(Notus.Variable.Class.BlockData BlockData)
        {
            return
                BlockData.validator.sign + NVC.Delimeter +
                BlockData.prev + NVC.Delimeter +
                BlockData.info.rowNo.ToString() + NVC.Delimeter +
                BlockNonce_GetPrevListStr(BlockData) + NVC.Delimeter +
                BlockData.hash.data + NVC.Delimeter +
                BlockData.hash.info + NVC.Delimeter +
                BlockData.hash.block;
        }
        private string FirstString_Validator(Notus.Variable.Class.BlockData BlockData)
        {
            return
            BlockNonce_ValidatorMapList_IntAndString(BlockData.validator.map.data) +
            NVC.Delimeter +

            BlockNonce_ValidatorMapList_IntAndString(BlockData.validator.map.info) +
            NVC.Delimeter +

            BlockNonce_ValidatorMapList_IntAndString(BlockData.validator.map.block) +
            NVC.Delimeter +

            BlockNonce_ValidatorMapList_IntAndString(BlockData.validator.map.block) +
            NVC.Delimeter +

            BlockNonce_ValidatorMapList_StringAndInt(BlockData.validator.count);
        }
        private string FirstString_Info(Notus.Variable.Class.BlockData BlockData)
        {
            return
            BlockData.info.version.ToString() + NVC.Delimeter +
            BlockData.info.type.ToString() + NVC.Delimeter +
            BlockData.info.uID + NVC.Delimeter +
            BlockData.info.time + NVC.Delimeter +
            BoolToStr(BlockData.info.multi) + NVC.Delimeter +

            BlockData.info.nonce.method.ToString() + NVC.Delimeter +
            BlockData.info.nonce.type.ToString() + NVC.Delimeter +
            BlockData.info.nonce.difficulty.ToString() + NVC.Delimeter +

            BlockData.info.node.id + NVC.Delimeter +
            BoolToStr(BlockData.info.node.master) + NVC.Delimeter +
            BoolToStr(BlockData.info.node.replicant) + NVC.Delimeter +
            BoolToStr(BlockData.info.node.broadcaster) + NVC.Delimeter +
            BoolToStr(BlockData.info.node.validator) + NVC.Delimeter +
            BoolToStr(BlockData.info.node.executor) + NVC.Delimeter +

            BoolToStr(BlockData.info.node.keeper.key) + NVC.Delimeter +
            BoolToStr(BlockData.info.node.keeper.block) + NVC.Delimeter +
            BoolToStr(BlockData.info.node.keeper.file) + NVC.Delimeter +
            BoolToStr(BlockData.info.node.keeper.tor) + NVC.Delimeter;
        }

        private string BlockNonce_ValidatorMapList_StringAndInt(Dictionary<string, int> DicList)
        {
            string TmpStr = "";
            bool isFirst = true;
            foreach (System.Collections.Generic.KeyValuePair<string, int> entry in DicList)
            {
                if (isFirst)
                {
                    TmpStr = $"{entry.Key}={entry.Value}";
                    isFirst = false;
                }
                else
                {
                    TmpStr = TmpStr + $";{entry.Key}={entry.Value}";
                }
            }
            return TmpStr;
        }
        private string BlockNonce_ValidatorMapList_IntAndString(Dictionary<int, string> DicList)
        {
            string TmpStr = "";
            bool isFirst = true;
            foreach (System.Collections.Generic.KeyValuePair<int, string> entry in DicList)
            {
                if (isFirst)
                {
                    TmpStr = $"{entry.Key}={entry.Value}";
                    isFirst = false;
                }
                else
                {
                    TmpStr = TmpStr + $";{entry.Key}={entry.Value}";
                }
            }
            return TmpStr;
        }
        private string BlockNonce_GetPrevListStr(Notus.Variable.Class.BlockData BlockPool)
        {
            string TmpStr = "";
            bool isFirst = true;
            foreach (System.Collections.Generic.KeyValuePair<int, string> entry in BlockPool.info.prevList)
            {
                if (isFirst)
                {
                    TmpStr = $"{entry.Key}={entry.Value}";
                    isFirst = false;
                }
                else
                {
                    TmpStr = TmpStr + $";{entry.Key}={entry.Value}";
                }
            }
            return TmpStr;
        }
        private string BoolToStr(bool tmpBoolVal)
        {
            if (tmpBoolVal == true) { return "1"; }
            return "0";
        }
        public Generate()
        {

        }
        public Generate(string validatorWalletKey)
        {
            ValidatorWalletKeyStr = validatorWalletKey;
        }
        ~Generate()
        {
            Dispose();
        }
        public void Dispose()
        {
        }
    }
}
