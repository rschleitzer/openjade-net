// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// Token values - Token is aliased to uint in GlobalUsings.cs
public static class Tokens
{
    // tokenUnrecognized must be 0
    public const Token tokenUnrecognized = 0;   // no token could be recognized
    public const Token tokenEe = 1;             // end of entity
    public const Token tokenS = 2;              // RS RE SPACE SEPCHAR
    public const Token tokenRe = 3;             // RE
    public const Token tokenRs = 4;             // RS
    public const Token tokenSpace = 5;          // SPACE
    public const Token tokenSepchar = 6;        // SEPCHAR
    public const Token tokenNameStart = 7;      // X
    public const Token tokenDigit = 8;          // 1
    public const Token tokenLcUcNmchar = 9;     // LCNMCHAR or UCNMCHAR
    public const Token tokenChar = 10;          // a legal data character
    public const Token tokenCharDelim = 11;     // a data character which starts a delimiter
    public const Token tokenIgnoredChar = 12;   // character in ignored marked section
    // delimiters and delimiters in context
    public const Token tokenAnd = 13;
    public const Token tokenCom = 14;
    public const Token tokenCroDigit = 15;
    public const Token tokenCroNameStart = 16;
    public const Token tokenDsc = 17;
    public const Token tokenDso = 18;
    public const Token tokenDtgc = 19;
    public const Token tokenDtgo = 20;
    public const Token tokenEroNameStart = 21;
    public const Token tokenEroGrpo = 22;
    public const Token tokenEtago = 23;
    public const Token tokenEtagoNameStart = 24;
    public const Token tokenEtagoTagc = 25;
    public const Token tokenEtagoGrpo = 26;
    public const Token tokenGrpc = 27;
    public const Token tokenGrpo = 28;
    public const Token tokenHcroHexDigit = 29;
    public const Token tokenLit = 30;
    public const Token tokenLita = 31;
    public const Token tokenMdc = 32;
    public const Token tokenMdoNameStart = 33;
    public const Token tokenMdoMdc = 34;
    public const Token tokenMdoCom = 35;
    public const Token tokenMdoDso = 36;
    public const Token tokenMinus = 37;
    public const Token tokenMinusGrpo = 38;
    public const Token tokenMscMdc = 39;
    public const Token tokenNet = 40;
    public const Token tokenNestc = 41;
    public const Token tokenOpt = 42;
    public const Token tokenOr = 43;
    public const Token tokenPero = 44;
    public const Token tokenPeroNameStart = 45;
    public const Token tokenPeroGrpo = 46;
    public const Token tokenPic = 47;
    public const Token tokenPio = 48;
    public const Token tokenPlus = 49;
    public const Token tokenPlusGrpo = 50;
    public const Token tokenRefc = 51;
    public const Token tokenRep = 52;
    public const Token tokenRni = 53;
    public const Token tokenSeq = 54;
    public const Token tokenStago = 55;
    public const Token tokenStagoNameStart = 56;
    public const Token tokenStagoTagc = 57;
    public const Token tokenStagoGrpo = 58;
    public const Token tokenTagc = 59;
    public const Token tokenVi = 60;
    // short references start here
    public const Token tokenFirstShortref = 61;
}
