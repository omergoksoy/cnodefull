
namespace Notus.Compiler
{
    public enum Token
    {
        Unknown,
        Setup,

        Kill,
        
        AirDrop,
        SetReceiver,
        SetTime,
        SetWalletKey,
        SetPublicKey,
        SetSignature,

        SetName,
        SetValue,

        Identifier,
        Value,

        //Keywords
        Print,
        If,
        EndIf,
        Then,
        Else,
        For,
        To,
        Next,
        Goto,
        Input,
        
        Const,
        Let,
        Gosub,
        Return,
        Rem,
        End,
        Assert,

        NewLine,
        Colon,
        Semicolon,
        Comma,

        Plus,
        Minus,
        Slash,
        Asterisk,
        Caret,
        Equal,
        Less,
        More,
        NotEqual,
        LessEqual,
        MoreEqual,
        Or,
        And,
        Not,

        LParen,
        RParen,

        EOF = -1   //End Of File
    }
}
