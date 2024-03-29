using System;
using System.Collections.Generic;
using System.Text.Json;
namespace Notus.Compiler
{
    public class Interpreter
    {
        public delegate void SetDataFunction(string text,Token incomeToken);
        public delegate void PrintFunction(string text);
        public delegate string InputFunction();

        public SetDataFunction setDataFunction;
        public PrintFunction printHandler;
        public InputFunction inputHandler;

        private Lexer lex;
        private Token prevToken; // token before last one
        private Token lastToken; // last seen token

        private Dictionary<string, Value> vars; // all variables are stored here
        private Dictionary<string, Marker> labels; // already seen labels 
        private Dictionary<string, Marker> loops; // for loops

        public delegate Value BasicFunction(Interpreter interpreter, List<Value> args);
        private Dictionary<string, BasicFunction> funcs; // all maped functions

        private int ifcounter; // counter used for matching "if" with "else"

        private Marker lineMarker; // current line marker

        private bool exit; // do we need to exit?

        public void Reset()
        {
            this.lex.Reset();
            this.vars.Clear();
            this.labels.Clear();
            this.loops.Clear();
            this.funcs.Clear();
            this.ifcounter = 0;
        }
        public void SetCode(string input)
        {
            //input = input.Trim();
            this.lex = new Lexer(input);
            this.vars = new Dictionary<string, Value>();
            this.labels = new Dictionary<string, Marker>();
            this.loops = new Dictionary<string, Marker>();
            this.funcs = new Dictionary<string, BasicFunction>();
            this.ifcounter = 0;
            BuiltIns.InstallAll(this); // map all builtins functions
        }
        public Interpreter()
        {

        }
        public Interpreter(string input)
        {
            SetCode(input);
        }

        public Value GetVar(string name)
        {
            if (!vars.ContainsKey(name))
                throw new BasicException("Variable with name " + name + " does not exist.", lineMarker.Line);
            return vars[name];
        }

        public void SetVar(string name, Value val)
        {
            if (!vars.ContainsKey(name)) vars.Add(name, val);
            else vars[name] = val;
        }

        public string GetLine()
        {
            return lex.GetLine(lineMarker);
        }

        public void AddFunction(string name, BasicFunction function)
        {
            if (!funcs.ContainsKey(name)) funcs.Add(name, function);
            else funcs[name] = function;
        }

        void Error(string text)
        {
            int satirNo = 0;
            if (lineMarker.Line != null)
            {
                satirNo = lineMarker.Line;
            }
            throw new BasicException(text, satirNo);
            //throw new BasicException(text, lineMarker.Line);
        }

        void Match(Token tok)
        {
            // check if current token is what we expect it to be
            if (lastToken != tok)
                Error("Expect " + tok.ToString() + " got " + lastToken.ToString());
        }

        public void Exec()
        {
            lineMarker.Line = 0;
            lineMarker.Pointer = 0;
            lineMarker.Column = 0;
            prevToken = Token.Unknown;
            lastToken= Token.Unknown;
            exit = false;
            GetNextToken();
            while (!exit) Line(); // do all lines
            if (lastToken == Token.Kill)
            {
                SetCommonData(lastToken);
            }
        }

        Token GetNextToken()
        {
            prevToken = lastToken;
            lastToken = lex.GetToken();
            if (lastToken == Token.EOF && prevToken == Token.EOF)
                Error("Unexpected end of file");

            return lastToken;
        }

        public Dictionary<string, int>  GetAllCode()
        {
            Dictionary<string, int> codeList = new();
            codeList.Clear();
            exit = false;
            while (lastToken != Token.Kill)
            {
                if (lastToken != Token.Unknown)
                {
                    if (codeList.ContainsKey(lastToken.ToString()) == false)
                    {
                        codeList.Add(lastToken.ToString(), 0);
                    }
                    codeList[lastToken.ToString()] = codeList[lastToken.ToString()] + 1;
                }
                GetNextToken();
            }
            if (lastToken == Token.Kill)
            {
                codeList[lastToken.ToString()] = 1;
            }
            return codeList;
        }
        void Line()
        {
            // skip empty new lines
            while (lastToken == Token.NewLine) GetNextToken();

            if (lastToken == Token.EOF || lastToken == Token.Kill)
            {
                exit = true;
                return;
            }

            lineMarker = lex.TokenMarker; // save current line marker
            Statment(); // evaluate statment

            if (lastToken != Token.NewLine && lastToken != Token.EOF)
                Error("Expect new line got " + lastToken.ToString());
        }

        void Statment()
        {
            Token keyword = lastToken;
            GetNextToken();
            switch (keyword)
            {
                case Token.Kill: 
                    SetCommonData(keyword);
                    End();
                    break;
                case Token.SetTime: SetCommonData(keyword); break;
                case Token.SetReceiver: SetCommonData(keyword); break;
                case Token.SetWalletKey: SetCommonData(keyword); break;
                case Token.SetPublicKey: SetCommonData(keyword); break;
                case Token.SetSignature: SetCommonData(keyword); break;
                case Token.SetName: SetCommonData(keyword); break;
                case Token.AirDrop: SetCommonData(keyword); break;
                case Token.SetValue: SetCommonData(keyword); break;

                case Token.Print: Print(); break;
                case Token.Input: Input(); break;
                
                case Token.Goto: Goto(); break;
                
                case Token.If: If(); break;
                case Token.Else: Else(); break;
                case Token.EndIf: break;

                case Token.For: For(); break;
                case Token.Next: Next(); break;
                
                case Token.Const:
                case Token.Let: Let(); break;

                case Token.Assert: Assert(); break;
                case Token.Identifier:
                    if (lastToken == Token.Equal) Let();
                    else if (lastToken == Token.Colon) Label();
                    else goto default;
                    break;

                case Token.End: End(); break;
                case Token.EOF: End(); break;

                default:
                    Error("Expect keyword got " + keyword.ToString());
                    Console.ReadLine();
                    break;
            }
            if (lastToken == Token.Colon)
            {
                // we can execute more statments in single line if we use ";"
                GetNextToken();
                Statment();
            }
        }

        void Print()
        {
            printHandler?.Invoke(Expr().ToString());
        }

        void SetCommonData(Token sender)
        {
            string paramVal=string.Empty;
            if(sender==Token.Kill){
                paramVal="";
            }else{
                paramVal=Expr().ToString();
            }
            setDataFunction?.Invoke(paramVal,sender);
        }
        void Input()
        {
            while (true)
            {
                Match(Token.Identifier);

                if (!vars.ContainsKey(lex.Identifier)) vars.Add(lex.Identifier, new Value());

                string input = inputHandler?.Invoke();
                double d;
                // try to parse as double, if failed read value as string
                if (double.TryParse(input, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out d))
                    vars[lex.Identifier] = new Value(d);
                else
                    vars[lex.Identifier] = new Value(input);

                GetNextToken();
                if (lastToken != Token.Comma) break;
                GetNextToken();
            }
        }

        void Goto()
        {
            Match(Token.Identifier);
            string name = lex.Identifier;

            if (!labels.ContainsKey(name))
            {
                // if we didn't encaunter required label yet, start to search for it
                while (true)
                {
                    if (GetNextToken() == Token.Colon && prevToken == Token.Identifier)
                    {
                        if (!labels.ContainsKey(lex.Identifier))
                            labels.Add(lex.Identifier, lex.TokenMarker);
                        if (lex.Identifier == name)
                            break;
                    }
                    if (lastToken == Token.EOF)
                    {
                        Error("Cannot find label named " + name);
                    }
                }
            }
            lex.GoTo(labels[name]);
            lastToken = Token.NewLine;
        }

        void If()
        {
            // check if argument is equal to 0
            bool result = (Expr().BinOp(new Value(0), Token.Equal).Real == 1);

            Match(Token.Then);
            GetNextToken();

            if (result)
            {
                // in case "if" evaulate to zero skip to matching else or endif
                int i = ifcounter;
                while (true)
                {
                    if (lastToken == Token.If)
                    {
                        i++;
                    }
                    else if (lastToken == Token.Else)
                    {
                        if (i == ifcounter)
                        {
                            GetNextToken();
                            return;
                        }
                    }
                    else if (lastToken == Token.EndIf)
                    {
                        if (i == ifcounter)
                        {
                            GetNextToken();
                            return;
                        }
                        i--;
                    }
                    GetNextToken();
                }
            }
        }

        void Else()
        {
            // skip to matching endif
            int i = ifcounter;
            while (true)
            {
                if (lastToken == Token.If)
                {
                    i++;
                }
                else if (lastToken == Token.EndIf)
                {
                    if (i == ifcounter)
                    {
                        GetNextToken();
                        return;
                    }
                    i--;
                }
                GetNextToken();
            }
        }

        void Label()
        {
            string name = lex.Identifier;
            if (!labels.ContainsKey(name)) labels.Add(name, lex.TokenMarker);

            GetNextToken();
            Match(Token.NewLine);
        }

        void End()
        {
            exit = true;
        }

        void Let()
        {
            if (lastToken != Token.Equal)
            {
                Match(Token.Identifier);
                GetNextToken();
                Match(Token.Equal);
            }

            string id = lex.Identifier;
            GetNextToken();

            SetVar(id, Expr());
        }

        void For()
        {
            Match(Token.Identifier);
            string var = lex.Identifier;

            GetNextToken();
            Match(Token.Equal);

            GetNextToken();
            Value v = Expr();

            // save for loop marker
            if (loops.ContainsKey(var))
            {
                loops[var] = lineMarker;
            }
            else
            {
                SetVar(var, v);
                loops.Add(var, lineMarker);
            }

            Match(Token.To);

            GetNextToken();
            v = Expr();

            if (vars[var].BinOp(v, Token.More).Real == 1)
            {
                while (true)
                {
                    while (!(GetNextToken() == Token.Identifier && prevToken == Token.Next)) ;
                    if (lex.Identifier == var)
                    {
                        loops.Remove(var);
                        GetNextToken();
                        Match(Token.NewLine);
                        break;
                    }
                }
            }
        }

        void Next()
        {
            // jump to begining of the "for" loop
            Match(Token.Identifier);
            string var = lex.Identifier;
            vars[var] = vars[var].BinOp(new Value(1), Token.Plus);
            lex.GoTo(new Marker(loops[var].Pointer - 1, loops[var].Line, loops[var].Column - 1));
            lastToken = Token.NewLine;
        }

        void Assert()
        {
            bool result = (Expr().BinOp(new Value(0), Token.Equal).Real == 1);

            if (result)
            {
                Error("Assertion fault"); // if out assert evaluate to false, throw error with souce code line
            }
        }

        Value Expr(int min = 0)
        {
            // originally we were using shunting-yard algorithm, but now we parse it recursively 
            Dictionary<Token, int> precedens = new Dictionary<Token, int>()
            {
                { Token.Or, 0 }, { Token.And, 0 },
                { Token.Equal, 1 }, { Token.NotEqual, 1 },
                { Token.Less, 1 }, { Token.More, 1 },
                { Token.LessEqual, 1 },  { Token.MoreEqual, 1 },
                { Token.Plus, 2 }, { Token.Minus, 2 },
                { Token.Asterisk, 3 }, {Token.Slash, 3 },
                { Token.Caret, 4 }
            };

            Value lhs = Primary();

            while (true)
            {
                if (lastToken < Token.Plus || lastToken > Token.And || precedens[lastToken] < min)
                    break;

                Token op = lastToken;
                int prec = precedens[lastToken]; // Operator Precedence
                int assoc = 0; // 0 left, 1 right; Operator associativity
                int nextmin = assoc == 0 ? prec : prec + 1;
                GetNextToken();
                Value rhs = Expr(nextmin);
                lhs = lhs.BinOp(rhs, op);
            }

            return lhs;
        }

        Value Primary()
        {
            Value prim = Value.Zero;

            if (lastToken == Token.Value)
            {
                // number | string
                prim = lex.Value;
                GetNextToken();
            }
            else if (lastToken == Token.Identifier)
            {
                // ident | ident '(' args ')'
                if (vars.ContainsKey(lex.Identifier))
                {
                    prim = vars[lex.Identifier];
                }
                else if (funcs.ContainsKey(lex.Identifier))
                {
                    string name = lex.Identifier;
                    List<Value> args = new List<Value>();
                    GetNextToken();
                    Match(Token.LParen);

                start:
                    if (GetNextToken() != Token.RParen)
                    {
                        args.Add(Expr());
                        if (lastToken == Token.Comma)
                            goto start;
                    }

                    prim = funcs[name](null, args);
                }
                else
                {
                    Error("Undeclared variable " + lex.Identifier);
                }
                GetNextToken();
            }
            else if (lastToken == Token.LParen)
            {
                // '(' expr ')'
                GetNextToken();
                prim = Expr();
                Match(Token.RParen);
                GetNextToken();
            }
            else if (lastToken == Token.Plus || lastToken == Token.Minus || lastToken == Token.Not)
            {
                // unary operator
                // '-' | '+' primary
                Token op = lastToken;
                GetNextToken();
                prim = Primary().UnaryOp(op);
            }
            else
            {
                Error("Unexpexted token in primary!");
            }

            return prim;
        }
    }
}
