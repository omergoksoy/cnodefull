using System;

namespace Notus.Compiler
{
    public class Lexer
    {
        private readonly string source;
        private Marker sourceMarker; // current position in source string
        private char lastChar;

        public Marker TokenMarker { get; set; }

        public string Identifier { get; set; } // Last encountered identifier
        public Value Value { get; set; } // Last number or string

        public void Reset()
        {
            sourceMarker = new Marker(0, 1, 1);
            lastChar = source[0];
        }
        public Lexer(string input)
        {
            source = input;
            sourceMarker = new Marker(0, 1, 1);
            lastChar = source[0];
        }

        public void GoTo(Marker marker)
        {
            sourceMarker = marker;
        }

        public string GetLine(Marker marker)
        {
            Marker oldMarker = sourceMarker;
            marker.Pointer--;
            GoTo(marker);

            string line = "";
            do
            {
                line += GetChar();
            } while (lastChar != '\n' && lastChar != (char)0);

            line.Remove(line.Length - 1);

            GoTo(oldMarker);

            return line;
        }

        char GetChar()
        {
            sourceMarker.Column++;
            sourceMarker.Pointer++;

            if (sourceMarker.Pointer >= source.Length)
                return lastChar = (char)0;
            //Console.WriteLine(lastChar);
            if ((lastChar = source[sourceMarker.Pointer]) == '\n')
            {
                sourceMarker.Column = 1;
                sourceMarker.Line++;
            }
            return lastChar;
        }

        public Token GetToken()
        {
            // skip white chars
            while (lastChar == ' ' || lastChar == '\t' || lastChar == '\r')
                GetChar();

            /*
            if (lastChar == '.')
            {
                GetChar();
            }
            */

            if (char.IsLetter(lastChar) || lastChar=='_')
            {
                Identifier = lastChar.ToString();
                //Console.WriteLine("ch : " + lastChar);
                //Console.WriteLine(Identifier);
                while (char.IsLetterOrDigit(GetChar()) || lastChar=='_')
                    Identifier += lastChar;

                switch (Identifier.ToUpper())
                {
                    case "SET_TIME": return Token.SetTime;
                    case "SET_WALLET_KEY": return Token.SetWalletKey;
                    case "SET_RECEIVER": return Token.SetReceiver;
                    case "SET_PUBLIC_KEY": return Token.SetPublicKey;
                    case "SET_SIGNATURE": return Token.SetSignature;
                    case "AIRDROP": return Token.AirDrop;

                    case "KILL": return Token.Kill;
                    case "SETNAME": return Token.SetName;
                    case "SETVALUE": return Token.SetValue;
                    case "PRINT": return Token.Print;
                    case "IF": return Token.If;
                    case "ENDIF": return Token.EndIf;
                    case "THEN": return Token.Then;
                    case "ELSE": return Token.Else;
                    case "FOR": return Token.For;
                    case "TO": return Token.To;
                    case "NEXT": return Token.Next;
                    case "GOTO": return Token.Goto;
                    case "INPUT": return Token.Input;
                    
                    case "LET": return Token.Let;
                    case "CONST": return Token.Const;

                    case "GOSUB": return Token.Gosub;
                    case "RETURN": return Token.Return;
                    case "END": return Token.End;
                    case "OR": return Token.Or;
                    case "AND": return Token.And;
                    case "NOT": return Token.Not;
                    case "ASSERT": return Token.Assert;
                    case "REM":
                        while (lastChar != '\n') GetChar();
                        GetChar();
                        return GetToken();
                    default:
                        //Console.WriteLine("degisken adi : " + Identifier.ToUpper());
                        return Token.Identifier;
                }
            }

            if (char.IsDigit(lastChar))
            {
                string num = "";
                do { num += lastChar; } while (char.IsDigit(GetChar()) || lastChar == '.');

                double real;
                if (!double.TryParse(num, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out real))
                    throw new Exception("ERROR while parsing number");
                Value = new Value(real);
                return Token.Value;
            }

            Token tok = Token.Unknown;
            switch (lastChar)
            {
                case '\n': tok = Token.NewLine; break;
                case ':': tok = Token.Colon; break;
                case ';': tok = Token.Semicolon; break;
                case ',': tok = Token.Comma; break;
                case '=': tok = Token.Equal; break;
                case '+': tok = Token.Plus; break;
                case '-': tok = Token.Minus; break;
                case '/': tok = Token.Slash; break;
                case '*': tok = Token.Asterisk; break;
                case '^': tok = Token.Caret; break;
                case '(': tok = Token.LParen; break;
                case ')': tok = Token.RParen; break;
                case '\'':
                    // skip comment until new line
                    while (lastChar != '\n') GetChar();
                    GetChar();
                    return GetToken();
                case '<':
                    GetChar();
                    if (lastChar == '>') tok = Token.NotEqual;
                    else if (lastChar == '=') tok = Token.LessEqual;
                    else return Token.Less;
                    break;
                case '>':
                    GetChar();
                    if (lastChar == '=') tok = Token.MoreEqual;
                    else return Token.More;
                    break;
                case '"':
                    string str = "";
                    while (GetChar() != '"')
                    {
                        if (lastChar == '\\')
                        {
                            // parse \n, \t, \\, \"
                            switch (char.ToLower(GetChar()))
                            {
                                case 'n': str += '\n'; break;
                                case 't': str += '\t'; break;
                                case '\\': str += '\\'; break;
                                case '"': str += '"'; break;
                            }
                        }
                        else
                        {
                            str += lastChar;
                        }
                    }
                    Value = new Value(str);
                    tok = Token.Value;
                    break;
                case (char)0:
                    return Token.EOF;
            }

            GetChar();
            return tok;
        }
    }
}
