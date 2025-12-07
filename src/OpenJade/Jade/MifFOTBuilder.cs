// Copyright (c) 1998 ISOGEN International Corp.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// ``Software''), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ``AS IS'', WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL ISOGEN INTERNATIONAL CORP. BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
// THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Except as contained in this notice, the name of ISOGEN International
// Corp. shall not be used in advertising or otherwise to promote the
// sale, use or other dealings in this Software without prior written
// authorization from ISOGEN International Corp.

// Created by Kathleen Marszalek and Paul Prescod.
// Ported to C# for OpenJade-NET.

namespace OpenJade.Jade;

using OpenSP;
using OpenJade.Style;
using OpenJade.Grove;
using System.Text;
using System.Runtime.InteropServices;
using Char = System.UInt32;
using Boolean = System.Boolean;

// List extension for C++ Vector compatibility
public static class ListExtensions
{
    public static void Resize<T>(this System.Collections.Generic.List<T> list, int newSize) where T : new()
    {
        if (newSize < list.Count)
        {
            list.RemoveRange(newSize, list.Count - newSize);
        }
        else
        {
            while (list.Count < newSize)
            {
                list.Add(new T());
            }
        }
    }
}

// MIF-specific messages
public static class MifMessages
{
    public static readonly MessageType0 missingTableColumnFlowObject = new MessageType0(
        MessageType.Severity.warning, null, 5000, "table cell refers to undefined column");
    public static readonly MessageType2 cannotOpenOutputError = new MessageType2(
        MessageType.Severity.error, null, 5001, "MIF: cannot open output file %1 (%2)");
    public static readonly MessageType1 systemIdNotFilename = new MessageType1(
        MessageType.Severity.error, null, 5002, "MIF: could not convert system identifier %1 to a single filename");
}

// StringHash helper class
public class StringHash
{
    public static ulong hash(string str)
    {
        ulong h = 0;
        foreach (char c in str)
            h = (h << 5) + h + c; // from Chris Torek
        return h;
    }
}

// MifDoc - MIF document model
public class MifDoc : IDisposable
{
    public static MifDoc? CurInstance;

    // Constructor
    public MifDoc(StringC fileLoc, CmdLineApp app)
    {
        App = app;
        RootOutputFileLoc_ = fileLoc.ToString();
        CurTblNum_ = 0;
        CurTextFlow_ = null;
        CurCell_ = null;
        CurPara_ = null;
        NextID_ = 0;
        curOs_ = null;
        BookComponents_ = new System.Collections.Generic.List<BookComponent>();
        TagStreamStack_ = new System.Collections.Generic.List<TagStream>();
        CrossRefInfos_ = new System.Collections.Generic.List<CrossRefInfo>();
        Elements_ = new ElementSet();

        if (RootOutputFileLoc_.Length > 0 && RootOutputFileLoc_[RootOutputFileLoc_.Length - 1] == '\0')
            RootOutputFileLoc_ = RootOutputFileLoc_.Substring(0, RootOutputFileLoc_.Length - 1);

        CurInstance = this;
        enterBookComponent();
    }

    public void Dispose()
    {
        commit();
    }

    public static MifDoc curInstance() { System.Diagnostics.Debug.Assert(CurInstance != null); return CurInstance!; }

    public CmdLineApp App;

    // T_indent struct
    public struct T_indent
    {
        public long data;
        public T_indent(int i) { data = i; }
        public static implicit operator int(T_indent i) => (int)i.data;
    }

    // T_dimension struct
    public struct T_dimension
    {
        public long data;
        public T_dimension(long u = 0) { data = u; }
        public static implicit operator long(T_dimension d) => d.data;
        public static implicit operator T_dimension(long d) => new T_dimension(d);
        public static implicit operator T_dimension(int d) => new T_dimension(d);
        public static T_dimension operator -(T_dimension a, T_dimension b) => new T_dimension(a.data - b.data);
        public static T_dimension operator /(T_dimension a, int b) => new T_dimension(a.data / b);
    }

    // T_string class - MIF string type
    public class T_string
    {
        private StringBuilder sb_;

        public T_string() { sb_ = new StringBuilder(); }
        public T_string(string s) { sb_ = new StringBuilder(s); }
        public T_string(T_string other) { sb_ = new StringBuilder(other.sb_.ToString()); }

        public int size() => sb_.Length;
        public char this[int index] => sb_[index];
        public void resize(int newSize)
        {
            if (newSize < sb_.Length)
                sb_.Length = newSize;
            else
                while (sb_.Length < newSize)
                    sb_.Append('\0');
        }

        public static T_string operator +(T_string s, char c)
        {
            var result = new T_string(s);
            result.sb_.Append(c);
            return result;
        }

        public void append(string s, int len)
        {
            sb_.Append(s, 0, len);
        }

        public void escapeSpecialChars()
        {
            T_string newValue = new T_string();
            bool changes = false;
            for (int i = 0; i < size(); i++)
            {
                switch (this[i])
                {
                    case '\n':
                    case '\r': changes = true; break;
                    case '\t': newValue.sb_.Append("\\t"); changes = true; break;
                    case '>': newValue.sb_.Append("\\>"); changes = true; break;
                    case '\'': newValue.sb_.Append("\\q"); changes = true; break;
                    case '`': newValue.sb_.Append("\\Q"); changes = true; break;
                    case '\\': newValue.sb_.Append("\\\\"); changes = true; break;
                    default:
                        if ((uint)this[i] >= 0x80)
                        {
                            changes = true;
                            MifDoc.CurInstance?.outHexChar((uint)this[i], newValue);
                        }
                        else
                            newValue.sb_.Append(this[i]);
                        break;
                }
            }
            if (changes)
                sb_ = newValue.sb_;
        }

        public override string ToString() => sb_.ToString();
        public static implicit operator string(T_string s) => s.ToString();

        public static bool operator ==(T_string? a, T_string? b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return a.ToString() == b.ToString();
        }
        public static bool operator !=(T_string? a, T_string? b) => !(a == b);
        public override bool Equals(object? obj) => obj is T_string other && this == other;
        public override int GetHashCode() => sb_.ToString().GetHashCode();
    }

    public class T_tagstring : T_string
    {
        public T_tagstring() : base() { }
        public T_tagstring(string s) : base(s) { }
    }

    public class T_pathname : T_string
    {
        public T_pathname() : base() { }
        public T_pathname(string s) : base(s) { }
    }

    public class T_keyword : T_string
    {
        public T_keyword() : base() { }
        public T_keyword(string s) : base(s) { }
    }

    // T_boolean struct
    public struct T_boolean
    {
        public bool data;
        public T_boolean(bool b = false) { data = b; }
        public static implicit operator bool(T_boolean b) => b.data;
        public static implicit operator T_boolean(bool b) => new T_boolean(b);
    }

    public struct T_WH
    {
        public T_dimension w, h;
        public T_WH(T_dimension w_, T_dimension h_) { w = w_; h = h_; }
    }

    public struct T_XY
    {
        public T_dimension x, y;
        public T_XY(T_dimension x_, T_dimension y_) { x = x_; y = y_; }
    }

    public struct T_LTRB
    {
        public T_dimension l, t, r, b;
        public T_LTRB(T_dimension l_, T_dimension t_, T_dimension r_, T_dimension b_)
        {
            l = l_; t = t_; r = r_; b = b_;
        }

        public static bool operator !=(T_LTRB o1, T_LTRB o2)
        {
            return o1.l != o2.l || o1.t != o2.t || o1.r != o2.r || o1.b != o2.b;
        }
        public static bool operator ==(T_LTRB o1, T_LTRB o2) => !(o1 != o2);
        public override bool Equals(object? obj) => obj is T_LTRB other && this == other;
        public override int GetHashCode() => HashCode.Combine(l, t, r, b);
    }

    public struct T_LTWH
    {
        public T_dimension l, t, w, h;
        public T_LTWH(T_dimension l_, T_dimension t_, T_dimension w_, T_dimension h_)
        {
            l = l_; t = t_; w = w_; h = h_;
        }
    }

    public struct T_XYWH
    {
        public T_dimension x, y, w, h;
    }

    public const int T_integer = 0; // placeholder
    public const int T_ID = 0; // placeholder
    public const int T_degrees = 0; // placeholder

    // Special characters
    public static readonly string sTab = "Tab";
    public static readonly string sHardSpace = "HardSpace";
    public static readonly string sSoftHyphen = "SoftHyphen";
    public static readonly string sHardHyphen = "HardHyphen";
    public static readonly string sHardReturn = "HardReturn";
    public static readonly string sNumberSpace = "NumberSpace";
    public static readonly string sThinSpace = "ThinSpace";
    public static readonly string sEnSpace = "EnSpace";
    public static readonly string sEmSpace = "EmSpace";
    public static readonly string sCent = "Cent";
    public static readonly string sPound = "Pound";
    public static readonly string sYen = "Yen";
    public static readonly string sEnDash = "EnDash";
    public static readonly string sEmDash = "EmDash";
    public static readonly string sDagger = "Dagger";
    public static readonly string sDoubleDagger = "DoubleDagger";
    public static readonly string sBullet = "Bullet";

    // For tagged text flows
    public static readonly string sA = "A";

    // FWeight
    public static readonly string sRegular = "Regular";
    public static readonly string sBold = "Bold";

    // FAngle
    public static readonly string sItalic = "Italic";

    // FColor
    public static readonly string sWhite = "White";
    public static readonly string sBlack = "Black";

    // FUnderlining
    public static readonly string sFNoUnderlining = "FNoUnderlining";
    public static readonly string sFSingle = "FSingle";
    public static readonly string sFDouble = "FDouble";
    public static readonly string sFNumeric = "FNumeric";

    // FPosition
    public static readonly string sFNormal = "FNormal";
    public static readonly string sFSuperscript = "FSuperscript";
    public static readonly string sFSubscript = "FSubscript";

    // FCase
    public static readonly string sFAsTyped = "FAsTyped";
    public static readonly string sFSmallCaps = "FSmallCaps";
    public static readonly string sFLowercase = "FLowercase";
    public static readonly string sFUppercase = "FUppercase";

    // FFamily
    public static readonly string sTimesNewRoman = "Times New Roman";

    // Paragraph formats
    public static readonly string sNONE = "";
    public static readonly string sDefaultPgfFormat = "Default Pgf Format";
    public static readonly string sDefaultTblFormat = "Default Tbl Format";
    public static readonly string sHeader = "Header";
    public static readonly string sFooter = "Footer";
    public static readonly string sSPACE = " ";

    // PgfAlignment
    public static readonly string sLeftRight = "LeftRight";
    public static readonly string sLeft = "Left";
    public static readonly string sRight = "Right";
    public static readonly string sCenter = "Center";

    // PgfLineSpacing
    public static readonly string sFixed = "Fixed";
    public static readonly string sProportional = "Proportional";

    // PgfCellAlignment
    public static readonly string sTop = "Top";
    public static readonly string sMiddle = "Middle";
    public static readonly string sBottom = "Bottom";

    // PgfPlacement
    public static readonly string sAnywhere = "Anywhere";
    public static readonly string sColumnTop = "ColumnTop";
    public static readonly string sPageTop = "PageTop";
    public static readonly string sLPageTop = "LPageTop";
    public static readonly string sRPageTop = "RPageTop";

    // PgfPlacementStyle
    public static readonly string sNormal = "Normal";
    public static readonly string sStraddle = "Straddle";

    // PgfLanguage
    public static readonly string sNoLanguage = "NoLanguage";
    public static readonly string sUSEnglish = "USEnglish";
    public static readonly string sUKEnglish = "UKEnglish";
    public static readonly string sGerman = "German";
    public static readonly string sSwissGerman = "SwissGerman";
    public static readonly string sFrench = "French";
    public static readonly string sCanadianFrench = "CanadianFrench";
    public static readonly string sSpanish = "Spanish";
    public static readonly string sCatalan = "Catalan";
    public static readonly string sItalian = "Italian";
    public static readonly string sPortuguese = "Portuguese";
    public static readonly string sBrazilian = "Brazilian";
    public static readonly string sDanish = "Danish";
    public static readonly string sDutch = "Dutch";
    public static readonly string sNorwegian = "Norwegian";
    public static readonly string sNynorsk = "Nynorsk";
    public static readonly string sFinnish = "Finnish";
    public static readonly string sSwedish = "Swedish";

    // TStype
    public static readonly string sDecimal = "Decimal";

    // DPageNumStyle
    public static readonly string sArabic = "Arabic";
    public static readonly string sUCRoman = "UCRoman";
    public static readonly string sLCRoman = "LCRoman";
    public static readonly string sUCAlpha = "UCAlpha";
    public static readonly string sLCAlpha = "LCAlpha";

    // DParity
    public static readonly string sFirstLeft = "FirstLeft";
    public static readonly string sFirstRight = "FirstRight";

    // PageType
    public static readonly string sLeftMasterPage = "LeftMasterPage";
    public static readonly string sRightMasterPage = "RightMasterPage";
    public static readonly string sOtherMasterPage = "OtherMasterPage";
    public static readonly string sBodyPage = "BodyPage";

    // PageTag
    public static readonly string sFirst = "First";

    // TblAlignment
    public static readonly string sInside = "Inside";
    public static readonly string sOutside = "Outside";

    // TblTitlePlacement
    public static readonly string sNone = "None";

    // HeadCap/TailCap
    public static readonly string sButt = "Butt";
    public static readonly string sRound = "Round";
    public static readonly string sSquare = "Square";

    // FrameType
    public static readonly string sInline = "Inline";
    public static readonly string sRunIntoParagraph = "RunIntoParagraph";
    public static readonly string sBelow = "Below";

    // XRef format
    public static readonly string sPageNumXRefFormatName = "Page Number Format";

    public static char escapeChar() => '\x0';

    // FontFormat class
    public class FontFormat
    {
        [Flags]
        public enum Flags
        {
            fNone = 0,
            fFFamily = 0x1,
            fFAngle = 0x2,
            fFWeight = 0x4,
            fFVar = 0x8,
            fFSize = 0x10,
            fFColor = 0x20,
            fFUnderlining = 0x40,
            fFOverline = 0x80,
            fFStrike = 0x100,
            fFPosition = 0x200,
            fFPairKern = 0x400,
            fFCase = 0x800,
            fFDX = 0x1000,
            fFDY = 0x2000,
            fFDW = 0x4000,
            fAll = 0x7FFF
        }

        public uint setProperties;

        public FontFormat() { setProperties = 0; }

        // Font name
        public string FFamily = "";
        public string FAngle = "";
        public string FWeight = "";
        public string FVar = "";

        // Font size and color
        public long FSize;
        public string FColor = "";

        // Font style
        public string FUnderlining = "";
        public bool FOverline;
        public bool FStrike;
        public string FPosition = "";
        public bool FPairKern;
        public string FCase = "";

        // Kerning information
        public double FDX;
        public double FDY;
        public double FDW;

        public void setFFamily(string p) { FFamily = p; setProperties |= (uint)Flags.fFFamily; }
        public void setFAngle(string p) { FAngle = p; setProperties |= (uint)Flags.fFAngle; }
        public void setFWeight(string p) { FWeight = p; setProperties |= (uint)Flags.fFWeight; }
        public void setFVar(string p) { FVar = p; setProperties |= (uint)Flags.fFVar; }
        public void setFSize(long p) { FSize = p; setProperties |= (uint)Flags.fFSize; }
        public void setFColor(string p) { FColor = p; setProperties |= (uint)Flags.fFColor; }
        public void setFUnderlining(string p) { FUnderlining = p; setProperties |= (uint)Flags.fFUnderlining; }
        public void setFOverline(bool p) { FOverline = p; setProperties |= (uint)Flags.fFOverline; }
        public void setFStrike(bool p) { FStrike = p; setProperties |= (uint)Flags.fFStrike; }
        public void setFPosition(string p) { FPosition = p; setProperties |= (uint)Flags.fFPosition; }
        public void setFPairKern(bool p) { FPairKern = p; setProperties |= (uint)Flags.fFPairKern; }
        public void setFCase(string p) { FCase = p; setProperties |= (uint)Flags.fFCase; }
        public void setFDX(double p) { FDX = p; setProperties |= (uint)Flags.fFDX; }
        public void setFDY(double p) { FDY = p; setProperties |= (uint)Flags.fFDY; }
        public void setFDW(double p) { FDW = p; setProperties |= (uint)Flags.fFDW; }

        public void setDSSSLDefaults()
        {
            setFFamily(sTimesNewRoman);
            setFAngle(sRegular);
            setFWeight(sRegular);
            setFVar(sRegular);
            setFSize(10000);
            setFColor(sBlack);
            setFUnderlining(sFNoUnderlining);
            setFOverline(false);
            setFStrike(false);
            setFPosition(sFNormal);
            setFPairKern(false);
            setFCase(sFAsTyped);
            setFDX(0);
            setFDY(0);
            setFDW(0);
        }

        public enum FontStatement { stPgfFont, stFont }

        public uint compare(FontFormat f)
        {
            uint differingProperties = 0;
            if (FFamily != f.FFamily) differingProperties |= (uint)Flags.fFFamily;
            if (FAngle != f.FAngle) differingProperties |= (uint)Flags.fFAngle;
            if (FWeight != f.FWeight) differingProperties |= (uint)Flags.fFWeight;
            if (FVar != f.FVar) differingProperties |= (uint)Flags.fFVar;
            if (FSize != f.FSize) differingProperties |= (uint)Flags.fFSize;
            if (FColor != f.FColor) differingProperties |= (uint)Flags.fFColor;
            if (FUnderlining != f.FUnderlining) differingProperties |= (uint)Flags.fFUnderlining;
            if (FOverline != f.FOverline) differingProperties |= (uint)Flags.fFOverline;
            if (FStrike != f.FStrike) differingProperties |= (uint)Flags.fFStrike;
            if (FPosition != f.FPosition) differingProperties |= (uint)Flags.fFPosition;
            if (FPairKern != f.FPairKern) differingProperties |= (uint)Flags.fFPairKern;
            if (FCase != f.FCase) differingProperties |= (uint)Flags.fFCase;
            if (FDX != f.FDX) differingProperties |= (uint)Flags.fFDX;
            if (FDY != f.FDY) differingProperties |= (uint)Flags.fFDY;
            if (FDW != f.FDW) differingProperties |= (uint)Flags.fFDW;
            return differingProperties;
        }

        public void setFrom(FontFormat f, uint properties)
        {
            if ((properties & (uint)Flags.fFFamily) != 0) FFamily = f.FFamily;
            if ((properties & (uint)Flags.fFAngle) != 0) FAngle = f.FAngle;
            if ((properties & (uint)Flags.fFWeight) != 0) FWeight = f.FWeight;
            if ((properties & (uint)Flags.fFVar) != 0) FVar = f.FVar;
            if ((properties & (uint)Flags.fFSize) != 0) FSize = f.FSize;
            if ((properties & (uint)Flags.fFColor) != 0) FColor = f.FColor;
            if ((properties & (uint)Flags.fFUnderlining) != 0) FUnderlining = f.FUnderlining;
            if ((properties & (uint)Flags.fFOverline) != 0) FOverline = f.FOverline;
            if ((properties & (uint)Flags.fFStrike) != 0) FStrike = f.FStrike;
            if ((properties & (uint)Flags.fFPosition) != 0) FPosition = f.FPosition;
            if ((properties & (uint)Flags.fFPairKern) != 0) FPairKern = f.FPairKern;
            if ((properties & (uint)Flags.fFCase) != 0) FCase = f.FCase;
            if ((properties & (uint)Flags.fFDX) != 0) FDX = f.FDX;
            if ((properties & (uint)Flags.fFDY) != 0) FDY = f.FDY;
            if ((properties & (uint)Flags.fFDW) != 0) FDW = f.FDW;
        }

        public void @out(MifOutputByteStream os, uint properties, FontStatement fontStatement)
        {
            string tagName = fontStatement == FontStatement.stPgfFont ? "PgfFont" : "Font";
            if (properties != 0)
            {
                os.stream().operatorOutput('\n');
                os.indent();
                os.stream().operatorOutput($"<{tagName} ");
                os.indent();
                if ((properties & (uint)Flags.fFFamily) != 0)
                    os.stream().operatorOutput($"\n <FFamily `{FFamily}'>");
                if ((properties & (uint)Flags.fFAngle) != 0)
                    os.stream().operatorOutput($"\n <FAngle `{FAngle}'>");
                if ((properties & (uint)Flags.fFWeight) != 0)
                    os.stream().operatorOutput($"\n <FWeight `{FWeight}'>");
                if ((properties & (uint)Flags.fFVar) != 0)
                    os.stream().operatorOutput($"\n <FVar `{FVar}'>");
                if ((properties & (uint)Flags.fFSize) != 0)
                    os.stream().operatorOutput($"\n <FSize {formatDimension(FSize)}>");
                if ((properties & (uint)Flags.fFColor) != 0)
                    os.stream().operatorOutput($"\n <FColor `{FColor}'>");
                if ((properties & (uint)Flags.fFUnderlining) != 0)
                    os.stream().operatorOutput($"\n <FUnderlining {FUnderlining}>");
                if ((properties & (uint)Flags.fFOverline) != 0)
                    os.stream().operatorOutput($"\n <FOverline {(FOverline ? "Yes" : "No")}>");
                if ((properties & (uint)Flags.fFStrike) != 0)
                    os.stream().operatorOutput($"\n <FStrike {(FStrike ? "Yes" : "No")}>");
                if ((properties & (uint)Flags.fFPosition) != 0)
                    os.stream().operatorOutput($"\n <FPosition {FPosition}>");
                if ((properties & (uint)Flags.fFPairKern) != 0)
                    os.stream().operatorOutput($"\n <FPairKern {(FPairKern ? "Yes" : "No")}>");
                if ((properties & (uint)Flags.fFCase) != 0)
                    os.stream().operatorOutput($"\n <FCase {FCase}>");
                if ((properties & (uint)Flags.fFDX) != 0)
                    os.stream().operatorOutput($"\n <FDX {FDX:F6}>");
                if ((properties & (uint)Flags.fFDY) != 0)
                    os.stream().operatorOutput($"\n <FDY {FDY:F6}>");
                if ((properties & (uint)Flags.fFDW) != 0)
                    os.stream().operatorOutput($"\n <FDW {FDW:F6}>");
                os.undent();
                os.stream().operatorOutput("\n >");
                os.undent();
            }
        }

        private static string formatDimension(long val)
        {
            long wholePart = val / 1000;
            long fracPart = Math.Abs(val % 1000);
            if (fracPart == 0)
                return $"{wholePart}pt";
            string fracStr = fracPart.ToString("D3").TrimEnd('0');
            return $"{wholePart}.{fracStr}pt";
        }

        public void @out(MifOutputByteStream os, FontStatement fontStatement)
        {
            @out(os, setProperties, fontStatement);
        }

        public void ffOut(MifOutputByteStream os, uint properties, FontStatement fontStatement)
        {
            @out(os, properties, fontStatement);
        }

        public void ffOut(MifOutputByteStream os, FontStatement fontStatement)
        {
            @out(os, fontStatement);
        }

        public void updateFrom(FontFormat f)
        {
            uint differingProperties = compare(f);
            setFrom(f, differingProperties);
            setProperties = differingProperties;
        }

        public void ffUpdateFrom(FontFormat f) { updateFrom(f); }
        public void clearSetProperties() { setProperties = 0; }
        public ref uint ffSetProperties() => ref setProperties;
    }

    // TabStop struct
    public class TabStop
    {
        [Flags]
        public enum Flags
        {
            fNone = 0,
            fTSX = 0x1,
            fTSType = 0x2,
            fTSLeaderStr = 0x4,
            fTSDecimalChar = 0x8,
            fAll = 0xF
        }

        public uint setProperties;
        public T_dimension TSX;
        public string TSType = "";
        public string TSLeaderStr = "";
        public int TSDecimalChar;

        public TabStop(string type = "Left", long x = 0, string tSLeaderStr = " ")
        {
            TSType = type;
            TSX = new T_dimension(x);
            setProperties = (uint)(Flags.fTSType | Flags.fTSX);
            setTSLeaderStr(tSLeaderStr);
        }

        public void setTSX(T_dimension p) { TSX = p; setProperties |= (uint)Flags.fTSX; }
        public void setTSType(string p) { TSType = p; setProperties |= (uint)Flags.fTSType; }
        public void setTSLeaderStr(string p) { TSLeaderStr = p; setProperties |= (uint)Flags.fTSLeaderStr; }
        public void setTSDecimalChar(int p) { TSDecimalChar = p; setProperties |= (uint)Flags.fTSDecimalChar; }

        public void @out(MifOutputByteStream os)
        {
            os.stream().operatorOutput("\n <TabStop ");
            os.indent();
            if ((setProperties & (uint)Flags.fTSX) != 0)
                os.stream().operatorOutput($"\n  <TSX {formatDimension(TSX)}>");
            if ((setProperties & (uint)Flags.fTSType) != 0)
                os.stream().operatorOutput($"\n  <TSType {TSType}>");
            if ((setProperties & (uint)Flags.fTSLeaderStr) != 0)
                os.stream().operatorOutput($"\n  <TSLeaderStr `{TSLeaderStr}'>");
            if ((setProperties & (uint)Flags.fTSDecimalChar) != 0)
                os.stream().operatorOutput($"\n  <TSDecimalChar {TSDecimalChar}>");
            os.undent();
            os.stream().operatorOutput("\n >");
        }

        private static string formatDimension(long val)
        {
            long wholePart = val / 1000;
            long fracPart = Math.Abs(val % 1000);
            if (fracPart == 0)
                return $"{wholePart}pt";
            string fracStr = fracPart.ToString("D3").TrimEnd('0');
            return $"{wholePart}.{fracStr}pt";
        }
    }

    // ParagraphFormat class
    public class ParagraphFormat : FontFormat
    {
        [Flags]
        public new enum Flags : uint
        {
            fNone = 0,
            fPgfFIndent = 0x1,
            fPgfLIndent = 0x2,
            fPgfRIndent = 0x4,
            fPgfAlignment = 0x8,
            fPgfSpBefore = 0x10,
            fPgfSpAfter = 0x20,
            fPgfLineSpacing = 0x40,
            fPgfLeading = 0x80,
            fPgfWithPrev = 0x100,
            fPgfWithNext = 0x200,
            fPgfBlockSize = 0x400,
            fPgfAutoNum = 0x800,
            fPgfNumFormat = 0x1000,
            fPgfNumberFont = 0x2000,
            fPgfHyphenate = 0x4000,
            fHyphenMaxLines = 0x8000,
            fHyphenMinPrefix = 0x10000,
            fHyphenMinSuffix = 0x20000,
            fHyphenMinWord = 0x40000,
            fPgfLetterSpace = 0x80000,
            fPgfLanguage = 0x100000,
            fPgfCellAlignment = 0x200000,
            fPgfCellMargins = 0x400000,
            fPgfCellLMarginFixed = 0x800000,
            fPgfCellTMarginFixed = 0x1000000,
            fPgfCellRMarginFixed = 0x2000000,
            fPgfCellBMarginFixed = 0x4000000,
            fPgfTag = 0x8000000,
            fTabStops = 0x10000000,
            fPgfPlacement = 0x20000000,
            fPgfNumTabs = 0x40000000,
            fPgfPlacementStyle = 0x80000000,
            fAll = 0xFFFFFFFF
        }

        public new uint setProperties;

        public ParagraphFormat() { setProperties = 0; }

        // Basic properties
        public string PgfTag = "";
        public long PgfFIndent;
        public long PgfLIndent;
        public long PgfRIndent;
        public string PgfAlignment = "";
        public long PgfSpBefore;
        public long PgfSpAfter;
        public string PgfLineSpacing = "";
        public long PgfLeading;
        public int PgfNumTabs;
        public System.Collections.Generic.List<TabStop> TabStops = new();

        // Pagination properties
        public string PgfPlacement = "";
        public string PgfPlacementStyle = "";
        public bool PgfWithPrev;
        public bool PgfWithNext;
        public int PgfBlockSize;

        // Numbering properties
        public bool PgfAutoNum;
        public string PgfNumFormat = "";
        public string PgfNumberFont = "";

        // Advanced properties
        public bool PgfHyphenate;
        public int HyphenMaxLines;
        public int HyphenMinPrefix;
        public int HyphenMinSuffix;
        public int HyphenMinWord;
        public bool PgfLetterSpace;
        public string PgfLanguage = "";

        // Table cell properties
        public string PgfCellAlignment = "";
        public T_LTRB PgfCellMargins;
        public bool PgfCellLMarginFixed;
        public bool PgfCellTMarginFixed;
        public bool PgfCellRMarginFixed;
        public bool PgfCellBMarginFixed;

        public void setPgfTag(string p) { PgfTag = p; setProperties |= (uint)Flags.fPgfTag; }
        public void setPgfFIndent(long p) { PgfFIndent = p; setProperties |= (uint)Flags.fPgfFIndent; }
        public void setPgfLIndent(long p) { PgfLIndent = p; setProperties |= (uint)Flags.fPgfLIndent; }
        public void setPgfRIndent(long p) { PgfRIndent = p; setProperties |= (uint)Flags.fPgfRIndent; }
        public void setPgfAlignment(string p) { PgfAlignment = p; setProperties |= (uint)Flags.fPgfAlignment; }
        public void setPgfSpBefore(long p) { PgfSpBefore = p; setProperties |= (uint)Flags.fPgfSpBefore; }
        public void setPgfSpAfter(long p) { PgfSpAfter = p; setProperties |= (uint)Flags.fPgfSpAfter; }
        public void setPgfLineSpacing(string p) { PgfLineSpacing = p; setProperties |= (uint)Flags.fPgfLineSpacing; }
        public void setPgfLeading(long p) { PgfLeading = p; setProperties |= (uint)Flags.fPgfLeading; }
        public void setPgfNumTabs(int p) { PgfNumTabs = p; setProperties |= (uint)Flags.fPgfNumTabs; }
        public void setPgfPlacement(string p) { PgfPlacement = p; setProperties |= (uint)Flags.fPgfPlacement; }
        public void setPgfPlacementStyle(string p) { PgfPlacementStyle = p; setProperties |= (uint)Flags.fPgfPlacementStyle; }
        public void setPgfWithPrev(bool p) { PgfWithPrev = p; setProperties |= (uint)Flags.fPgfWithPrev; }
        public void setPgfWithNext(bool p) { PgfWithNext = p; setProperties |= (uint)Flags.fPgfWithNext; }
        public void setPgfBlockSize(int p) { PgfBlockSize = p; setProperties |= (uint)Flags.fPgfBlockSize; }
        public void setPgfAutoNum(bool p) { PgfAutoNum = p; setProperties |= (uint)Flags.fPgfAutoNum; }
        public void setPgfNumFormat(string p) { PgfNumFormat = p; setProperties |= (uint)Flags.fPgfNumFormat; }
        public void setPgfNumberFont(string p) { PgfNumberFont = p; setProperties |= (uint)Flags.fPgfNumberFont; }
        public void setPgfHyphenate(bool p) { PgfHyphenate = p; setProperties |= (uint)Flags.fPgfHyphenate; }
        public void setHyphenMaxLines(int p) { HyphenMaxLines = p; setProperties |= (uint)Flags.fHyphenMaxLines; }
        public void setHyphenMinPrefix(int p) { HyphenMinPrefix = p; setProperties |= (uint)Flags.fHyphenMinPrefix; }
        public void setHyphenMinSuffix(int p) { HyphenMinSuffix = p; setProperties |= (uint)Flags.fHyphenMinSuffix; }
        public void setHyphenMinWord(int p) { HyphenMinWord = p; setProperties |= (uint)Flags.fHyphenMinWord; }
        public void setPgfLetterSpace(bool p) { PgfLetterSpace = p; setProperties |= (uint)Flags.fPgfLetterSpace; }
        public void setPgfLanguage(string p) { PgfLanguage = p; setProperties |= (uint)Flags.fPgfLanguage; }
        public void setPgfCellAlignment(string p) { PgfCellAlignment = p; setProperties |= (uint)Flags.fPgfCellAlignment; }
        public void setPgfCellMargins(T_LTRB p) { PgfCellMargins = p; setProperties |= (uint)Flags.fPgfCellMargins; }
        public void setPgfCellLMarginFixed(bool p) { PgfCellLMarginFixed = p; setProperties |= (uint)Flags.fPgfCellLMarginFixed; }
        public void setPgfCellTMarginFixed(bool p) { PgfCellTMarginFixed = p; setProperties |= (uint)Flags.fPgfCellTMarginFixed; }
        public void setPgfCellRMarginFixed(bool p) { PgfCellRMarginFixed = p; setProperties |= (uint)Flags.fPgfCellRMarginFixed; }
        public void setPgfCellBMarginFixed(bool p) { PgfCellBMarginFixed = p; setProperties |= (uint)Flags.fPgfCellBMarginFixed; }

        public new void setDSSSLDefaults()
        {
            base.setDSSSLDefaults();
            setPgfFIndent(0);
            setPgfLIndent(0);
            setPgfRIndent(0);
            setPgfAlignment(sLeft);
            setPgfSpBefore(0);
            setPgfSpAfter(0);
            setPgfLineSpacing(sFixed);
            setPgfLeading(0);
            setPgfNumTabs(0);
            setPgfWithPrev(false);
            setPgfWithNext(false);
            setPgfBlockSize(2);
            setPgfAutoNum(false);
            setPgfHyphenate(false);
            setHyphenMaxLines(999);
            setHyphenMinPrefix(2);
            setHyphenMinSuffix(2);
            setHyphenMinWord(2);
            setPgfLetterSpace(false);
            setPgfLanguage(sNoLanguage);
            setPgfCellAlignment(sTop);
            setPgfCellMargins(new T_LTRB(new T_dimension(0), new T_dimension(0), new T_dimension(0), new T_dimension(0)));
            setPgfCellLMarginFixed(true);
            setPgfCellTMarginFixed(true);
            setPgfCellRMarginFixed(true);
            setPgfCellBMarginFixed(true);
            setPgfPlacement(sAnywhere);
            setPgfPlacementStyle(sNormal);
        }

        public void forceSetProperties(uint properties, uint fontProperties)
        {
            setProperties = properties;
            base.setProperties = fontProperties;
        }

        public new ref uint ffSetProperties() => ref base.setProperties;

        public new void updateFrom(ParagraphFormat f)
        {
            base.updateFrom(f);
            uint differingProperties = compare(f);
            setFrom(f, differingProperties, 0);
            setProperties = differingProperties;
        }

        public new void clearSetProperties()
        {
            setProperties = 0;
            base.clearSetProperties();
        }

        public void copyFrom(ParagraphFormat f)
        {
            setFrom(f, (uint)Flags.fAll, (uint)FontFormat.Flags.fAll);
            setProperties = f.setProperties;
            base.setProperties = ((FontFormat)f).setProperties;
            TabStops = new System.Collections.Generic.List<TabStop>(f.TabStops);
        }

        public uint compare(ParagraphFormat f)
        {
            uint differingProperties = 0;
            if (PgfTag != f.PgfTag) differingProperties |= (uint)Flags.fPgfTag;
            if (PgfLanguage != f.PgfLanguage) differingProperties |= (uint)Flags.fPgfLanguage;
            if (PgfFIndent != f.PgfFIndent) differingProperties |= (uint)Flags.fPgfFIndent;
            if (PgfLIndent != f.PgfLIndent) differingProperties |= (uint)Flags.fPgfLIndent;
            if (PgfRIndent != f.PgfRIndent) differingProperties |= (uint)Flags.fPgfRIndent;
            if (PgfAlignment != f.PgfAlignment) differingProperties |= (uint)Flags.fPgfAlignment;
            if (PgfSpBefore != f.PgfSpBefore) differingProperties |= (uint)Flags.fPgfSpBefore;
            if (PgfSpAfter != f.PgfSpAfter) differingProperties |= (uint)Flags.fPgfSpAfter;
            if (PgfLineSpacing != f.PgfLineSpacing) differingProperties |= (uint)Flags.fPgfLineSpacing;
            if (PgfLeading != f.PgfLeading) differingProperties |= (uint)Flags.fPgfLeading;
            if (PgfNumTabs != f.PgfNumTabs) differingProperties |= (uint)Flags.fPgfNumTabs;
            if (PgfPlacement != f.PgfPlacement) differingProperties |= (uint)Flags.fPgfPlacement;
            if (PgfPlacementStyle != f.PgfPlacementStyle) differingProperties |= (uint)Flags.fPgfPlacementStyle;
            if (PgfWithPrev != f.PgfWithPrev) differingProperties |= (uint)Flags.fPgfWithPrev;
            if (PgfWithNext != f.PgfWithNext) differingProperties |= (uint)Flags.fPgfWithNext;
            if (PgfBlockSize != f.PgfBlockSize) differingProperties |= (uint)Flags.fPgfBlockSize;
            if (PgfAutoNum != f.PgfAutoNum) differingProperties |= (uint)Flags.fPgfAutoNum;
            if (PgfNumFormat != f.PgfNumFormat) differingProperties |= (uint)Flags.fPgfNumFormat;
            if (PgfNumberFont != f.PgfNumberFont) differingProperties |= (uint)Flags.fPgfNumberFont;
            if (PgfHyphenate != f.PgfHyphenate) differingProperties |= (uint)Flags.fPgfHyphenate;
            if (HyphenMaxLines != f.HyphenMaxLines) differingProperties |= (uint)Flags.fHyphenMaxLines;
            if (HyphenMinPrefix != f.HyphenMinPrefix) differingProperties |= (uint)Flags.fHyphenMinPrefix;
            if (HyphenMinSuffix != f.HyphenMinSuffix) differingProperties |= (uint)Flags.fHyphenMinSuffix;
            if (HyphenMinWord != f.HyphenMinWord) differingProperties |= (uint)Flags.fHyphenMinWord;
            if (PgfLetterSpace != f.PgfLetterSpace) differingProperties |= (uint)Flags.fPgfLetterSpace;
            if (PgfCellAlignment != f.PgfCellAlignment) differingProperties |= (uint)Flags.fPgfCellAlignment;
            if (PgfCellMargins != f.PgfCellMargins) differingProperties |= (uint)Flags.fPgfCellMargins;
            if (PgfCellLMarginFixed != f.PgfCellLMarginFixed) differingProperties |= (uint)Flags.fPgfCellLMarginFixed;
            if (PgfCellTMarginFixed != f.PgfCellTMarginFixed) differingProperties |= (uint)Flags.fPgfCellTMarginFixed;
            if (PgfCellRMarginFixed != f.PgfCellRMarginFixed) differingProperties |= (uint)Flags.fPgfCellRMarginFixed;
            if (PgfCellBMarginFixed != f.PgfCellBMarginFixed) differingProperties |= (uint)Flags.fPgfCellBMarginFixed;
            return differingProperties;
        }

        public void setFrom(ParagraphFormat f, uint properties, uint fontProperties)
        {
            if ((properties & (uint)Flags.fPgfTag) != 0) PgfTag = f.PgfTag;
            if ((properties & (uint)Flags.fPgfLanguage) != 0) PgfLanguage = f.PgfLanguage;
            if ((properties & (uint)Flags.fPgfFIndent) != 0) PgfFIndent = f.PgfFIndent;
            if ((properties & (uint)Flags.fPgfLIndent) != 0) PgfLIndent = f.PgfLIndent;
            if ((properties & (uint)Flags.fPgfRIndent) != 0) PgfRIndent = f.PgfRIndent;
            if ((properties & (uint)Flags.fPgfAlignment) != 0) PgfAlignment = f.PgfAlignment;
            if ((properties & (uint)Flags.fPgfSpBefore) != 0) PgfSpBefore = f.PgfSpBefore;
            if ((properties & (uint)Flags.fPgfSpAfter) != 0) PgfSpAfter = f.PgfSpAfter;
            if ((properties & (uint)Flags.fPgfLineSpacing) != 0) PgfLineSpacing = f.PgfLineSpacing;
            if ((properties & (uint)Flags.fPgfLeading) != 0) PgfLeading = f.PgfLeading;
            if ((properties & (uint)Flags.fPgfNumTabs) != 0) PgfNumTabs = f.PgfNumTabs;
            if ((properties & (uint)Flags.fPgfPlacement) != 0) PgfPlacement = f.PgfPlacement;
            if ((properties & (uint)Flags.fPgfPlacementStyle) != 0) PgfPlacementStyle = f.PgfPlacementStyle;
            if ((properties & (uint)Flags.fPgfWithPrev) != 0) PgfWithPrev = f.PgfWithPrev;
            if ((properties & (uint)Flags.fPgfWithNext) != 0) PgfWithNext = f.PgfWithNext;
            if ((properties & (uint)Flags.fPgfBlockSize) != 0) PgfBlockSize = f.PgfBlockSize;
            if ((properties & (uint)Flags.fPgfAutoNum) != 0) PgfAutoNum = f.PgfAutoNum;
            if ((properties & (uint)Flags.fPgfNumFormat) != 0) PgfNumFormat = f.PgfNumFormat;
            if ((properties & (uint)Flags.fPgfNumberFont) != 0) PgfNumberFont = f.PgfNumberFont;
            if ((properties & (uint)Flags.fPgfHyphenate) != 0) PgfHyphenate = f.PgfHyphenate;
            if ((properties & (uint)Flags.fHyphenMaxLines) != 0) HyphenMaxLines = f.HyphenMaxLines;
            if ((properties & (uint)Flags.fHyphenMinPrefix) != 0) HyphenMinPrefix = f.HyphenMinPrefix;
            if ((properties & (uint)Flags.fHyphenMinSuffix) != 0) HyphenMinSuffix = f.HyphenMinSuffix;
            if ((properties & (uint)Flags.fHyphenMinWord) != 0) HyphenMinWord = f.HyphenMinWord;
            if ((properties & (uint)Flags.fPgfLetterSpace) != 0) PgfLetterSpace = f.PgfLetterSpace;
            if ((properties & (uint)Flags.fPgfCellAlignment) != 0) PgfCellAlignment = f.PgfCellAlignment;
            if ((properties & (uint)Flags.fPgfCellMargins) != 0) PgfCellMargins = f.PgfCellMargins;
            if ((properties & (uint)Flags.fPgfCellLMarginFixed) != 0) PgfCellLMarginFixed = f.PgfCellLMarginFixed;
            if ((properties & (uint)Flags.fPgfCellTMarginFixed) != 0) PgfCellTMarginFixed = f.PgfCellTMarginFixed;
            if ((properties & (uint)Flags.fPgfCellRMarginFixed) != 0) PgfCellRMarginFixed = f.PgfCellRMarginFixed;
            if ((properties & (uint)Flags.fPgfCellBMarginFixed) != 0) PgfCellBMarginFixed = f.PgfCellBMarginFixed;
            base.setFrom(f, fontProperties);
        }

        public void @out(MifOutputByteStream os, bool excludeCellProperties = true)
        {
            @out(os, setProperties, base.setProperties, excludeCellProperties);
        }

        public void @out(MifOutputByteStream os, uint properties, uint fontProperties, bool excludeCellProperties = true)
        {
            bool outPgfTag = false;
            string pgfTag = "";

            if ((properties & (uint)Flags.fPgfTag) == 0)
            {
                TagStream? ts = MifDoc.CurInstance?.curTagStream();
                if (ts != null && !ts.PgfTagUsed)
                {
                    pgfTag = ts.InitialPgfTag;
                    outPgfTag = true;
                    ts.PgfTagUsed = true;
                }
            }

            if (properties != 0 || fontProperties != 0 || outPgfTag)
            {
                _ = os << '\n' << MifOutputByteStream.INDENT << "<Pgf ";
                os.indent();

                if (outPgfTag)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfTag `" << pgfTag << "'>";
                else if ((properties & (uint)Flags.fPgfTag) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfTag `" << PgfTag << "'>";

                base.@out(os, fontProperties, FontStatement.stPgfFont);

                if ((properties & (uint)Flags.fPgfLanguage) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfLanguage " << PgfLanguage << ">";
                if ((properties & (uint)Flags.fPgfFIndent) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfFIndent " << new T_dimension(PgfFIndent) << ">";
                if ((properties & (uint)Flags.fPgfLIndent) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfLIndent " << new T_dimension(PgfLIndent) << ">";
                if ((properties & (uint)Flags.fPgfRIndent) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfRIndent " << new T_dimension(PgfRIndent) << ">";
                if ((properties & (uint)Flags.fPgfAlignment) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfAlignment " << PgfAlignment << ">";
                if ((properties & (uint)Flags.fPgfSpBefore) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfSpBefore " << new T_dimension(PgfSpBefore) << ">";
                if ((properties & (uint)Flags.fPgfSpAfter) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfSpAfter " << new T_dimension(PgfSpAfter) << ">";
                if ((properties & (uint)Flags.fPgfLineSpacing) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfLineSpacing " << PgfLineSpacing << ">";
                if ((properties & (uint)Flags.fPgfLeading) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfLeading " << new T_dimension(PgfLeading) << ">";

                if ((properties & (uint)Flags.fPgfNumTabs) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfNumTabs " << PgfNumTabs << ">";
                if ((properties & (uint)Flags.fTabStops) != 0)
                    for (int i = 0; i < TabStops.Count; i++)
                        TabStops[i].@out(os);

                if ((properties & (uint)Flags.fPgfPlacement) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfPlacement " << PgfPlacement << ">";
                if ((properties & (uint)Flags.fPgfPlacementStyle) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfPlacementStyle " << PgfPlacementStyle << ">";
                if ((properties & (uint)Flags.fPgfWithPrev) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfWithPrev " << (PgfWithPrev ? "Yes" : "No") << ">";
                if ((properties & (uint)Flags.fPgfWithNext) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfWithNext " << (PgfWithNext ? "Yes" : "No") << ">";
                if ((properties & (uint)Flags.fPgfBlockSize) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfBlockSize " << PgfBlockSize << ">";
                if ((properties & (uint)Flags.fPgfAutoNum) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfAutoNum " << (PgfAutoNum ? "Yes" : "No") << ">";
                if ((properties & (uint)Flags.fPgfNumFormat) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfNumFormat `" << PgfNumFormat << "'>";
                if ((properties & (uint)Flags.fPgfNumberFont) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfNumberFont `" << PgfNumberFont << "'>";
                if ((properties & (uint)Flags.fPgfHyphenate) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfHyphenate " << (PgfHyphenate ? "Yes" : "No") << ">";
                if ((properties & (uint)Flags.fHyphenMaxLines) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<HyphenMaxLines " << HyphenMaxLines << ">";
                if ((properties & (uint)Flags.fHyphenMinPrefix) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<HyphenMinPrefix " << HyphenMinPrefix << ">";
                if ((properties & (uint)Flags.fHyphenMinSuffix) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<HyphenMinSuffix " << HyphenMinSuffix << ">";
                if ((properties & (uint)Flags.fHyphenMinWord) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<HyphenMinWord " << HyphenMinWord << ">";
                if ((properties & (uint)Flags.fPgfLetterSpace) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfLetterSpace " << (PgfLetterSpace ? "Yes" : "No") << ">";

                if (MifDoc.CurInstance?.curCell() != null || !excludeCellProperties)
                {
                    if ((properties & (uint)Flags.fPgfCellAlignment) != 0)
                        _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfCellAlignment " << PgfCellAlignment << ">";
                    if ((properties & (uint)Flags.fPgfCellMargins) != 0)
                        _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfCellMargins " << PgfCellMargins << ">";
                    if ((properties & (uint)Flags.fPgfCellLMarginFixed) != 0)
                        _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfCellLMarginFixed " << (PgfCellLMarginFixed ? "Yes" : "No") << ">";
                    if ((properties & (uint)Flags.fPgfCellTMarginFixed) != 0)
                        _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfCellTMarginFixed " << (PgfCellTMarginFixed ? "Yes" : "No") << ">";
                    if ((properties & (uint)Flags.fPgfCellRMarginFixed) != 0)
                        _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfCellRMarginFixed " << (PgfCellRMarginFixed ? "Yes" : "No") << ">";
                    if ((properties & (uint)Flags.fPgfCellBMarginFixed) != 0)
                        _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfCellBMarginFixed " << (PgfCellBMarginFixed ? "Yes" : "No") << ">";
                }

                os.undent();
                _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            }
        }
    }

    // Document class
    public class Document
    {
        [Flags]
        public enum Flags
        {
            fNone = 0,
            fDPageSize = 0x1,
            fDStartPage = 0x2,
            fDPageNumStyle = 0x4,
            fDTwoSides = 0x8,
            fDParity = 0x10,
            fDMargins = 0x20,
            fDColumns = 0x40,
            fDColumnGap = 0x80,
            fAll = 0xFF
        }

        public uint setProperties;

        public Document() { setProperties = 0; }

        public T_LTRB DMargins;
        public int DColumns;
        public T_dimension DColumnGap;
        public T_WH DPageSize;
        public int DStartPage;
        public string DPageNumStyle = "";
        public bool DTwoSides;
        public string DParity = "";

        public void setDMargins(T_LTRB p) { DMargins = p; setProperties |= (uint)Flags.fDMargins; }
        public void setDColumns(int p) { DColumns = p; setProperties |= (uint)Flags.fDColumns; }
        public void setDColumnGap(T_dimension p) { DColumnGap = p; setProperties |= (uint)Flags.fDColumnGap; }
        public void setDPageSize(T_WH p) { DPageSize = p; setProperties |= (uint)Flags.fDPageSize; }
        public void setDStartPage(int p) { DStartPage = p; setProperties |= (uint)Flags.fDStartPage; }
        public void setDPageNumStyle(string p) { DPageNumStyle = p; setProperties |= (uint)Flags.fDPageNumStyle; }
        public void setDTwoSides(bool p) { DTwoSides = p; setProperties |= (uint)Flags.fDTwoSides; }
        public void setDParity(string p) { DParity = p; setProperties |= (uint)Flags.fDParity; }

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Document ";
            os.indent();
            if ((setProperties & (uint)Flags.fDMargins) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<DMargins " << DMargins << ">";
            if ((setProperties & (uint)Flags.fDColumns) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<DColumns " << DColumns << ">";
            if ((setProperties & (uint)Flags.fDColumnGap) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<DColumnGap " << DColumnGap << ">";
            if ((setProperties & (uint)Flags.fDPageSize) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<DPageSize " << DPageSize << ">";
            if ((setProperties & (uint)Flags.fDStartPage) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<DStartPage " << DStartPage << ">";
            if ((setProperties & (uint)Flags.fDPageNumStyle) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<DPageNumStyle " << DPageNumStyle << ">";
            if ((setProperties & (uint)Flags.fDTwoSides) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<DTwoSides " << (DTwoSides ? "Yes" : "No") << ">";
            if ((setProperties & (uint)Flags.fDParity) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<DParity " << DParity << ">";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // Object class (base for graphics objects)
    public abstract class Object
    {
        [Flags]
        public enum Flags
        {
            fNone = 0,
            fID = 0x1,
            fPen = 0x2,
            fFill = 0x4,
            fPenWidth = 0x8,
            fObjectNext = 0x10,
            fObColor = 0x20
        }

        public uint setProperties;
        public static uint IDCnt = 0;

        public uint ID;
        public int Pen;
        public int Fill;
        public T_dimension PenWidth;
        public T_tagstring ObColor = new T_tagstring();

        public Object(int pen = 15, int fill = 15, long penWidth = 0, string obColor = "Black")
        {
            setProperties = 0;
            setID(++IDCnt);
            setPen(pen);
            setFill(fill);
            setPenWidth(new T_dimension(penWidth));
            setObColor(obColor);
        }

        public void setID(uint p) { ID = p; setProperties |= (uint)Flags.fID; }
        public void setPen(int p) { Pen = p; setProperties |= (uint)Flags.fPen; }
        public void setFill(int p) { Fill = p; setProperties |= (uint)Flags.fFill; }
        public void setPenWidth(T_dimension p) { PenWidth = p; setProperties |= (uint)Flags.fPenWidth; }
        public void setObColor(string p) { ObColor = new T_tagstring(p); setProperties |= (uint)Flags.fObColor; }
        public void setObColor(T_tagstring p) { ObColor = p; setProperties |= (uint)Flags.fObColor; }

        public void outObjectProperties(MifOutputByteStream os)
        {
            if ((setProperties & (uint)Flags.fID) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<ID " << ID << ">";
            if ((setProperties & (uint)Flags.fPen) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<Pen " << Pen << ">";
            if ((setProperties & (uint)Flags.fFill) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<Fill " << Fill << ">";
            if ((setProperties & (uint)Flags.fPenWidth) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<PenWidth " << PenWidth << ">";
            if ((setProperties & (uint)Flags.fObColor) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<ObColor `" << ObColor.ToString() << "'>";
        }

        public abstract void @out(MifOutputByteStream os);
    }

    // Forward declarations for other nested classes
    public class PolyLine : Object
    {
        public string HeadCap = "";
        public string TailCap = "";
        public System.Collections.Generic.List<T_XY> Points = new();

        public PolyLine(string cap, int pen = 15, int fill = 15, long penWidth = 0, string obColor = "Black")
            : base(pen, fill, penWidth, obColor)
        {
            setHeadCap(cap);
            setTailCap(cap);
        }

        public void setHeadCap(string p) { HeadCap = p; setProperties |= (uint)Flags.fObjectNext << 1; }
        public void setTailCap(string p) { TailCap = p; setProperties |= (uint)Flags.fObjectNext << 2; }

        public override void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<PolyLine ";
            os.indent();
            outObjectProperties(os);
            if ((setProperties & ((uint)Flags.fObjectNext << 1)) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<HeadCap " << HeadCap << ">";
            if ((setProperties & ((uint)Flags.fObjectNext << 2)) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TailCap " << TailCap << ">";
            for (int i = 0; i < Points.Count; i++)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<Point " << Points[i] << ">";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    public class ImportObject : Object
    {
        public string ImportObFileDI = "";
        public int BitMapDpi;
        public T_LTWH ShapeRect;
        public bool ImportObFixedSize;
        public T_XY NativeOrigin;

        public ImportObject(string importObFileDI, T_LTWH shapeRect, bool importObFixedSize = true, int bitMapDpi = 72)
            : base()
        {
            setImportObFileDI(importObFileDI);
            setShapeRect(shapeRect);
            setImportObFixedSize(importObFixedSize);
            setBitMapDpi(bitMapDpi);
        }

        public void setImportObFileDI(string p) { ImportObFileDI = p; }
        public void setBitMapDpi(int p) { BitMapDpi = p; }
        public void setShapeRect(T_LTWH p) { ShapeRect = p; }
        public void setImportObFixedSize(bool p) { ImportObFixedSize = p; }
        public void setNativeOrigin(T_XY p) { NativeOrigin = p; }

        public override void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<ImportObject ";
            os.indent();
            outObjectProperties(os);
            _ = os << '\n' << MifOutputByteStream.INDENT << "<ImportObFileDI `" << ImportObFileDI << "'>";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<BitMapDpi " << BitMapDpi << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<ShapeRect " << ShapeRect << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<ImportObFixedSize " << (ImportObFixedSize ? "Yes" : "No") << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<NativeOrigin " << NativeOrigin << ">";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    public class Frame : Object
    {
        public T_LTWH ShapeRect;
        public string FrameType = "";
        public T_dimension NSOffset;
        public T_dimension BLOffset;
        public string AnchorAlign = "";
        public System.Collections.Generic.List<Object> Objects = new();

        public Frame() : base() { }

        public void setShapeRect(T_LTWH p) { ShapeRect = p; }
        public void setFrameType(string p) { FrameType = p; }
        public void setNSOffset(T_dimension p) { NSOffset = p; }
        public void setBLOffset(T_dimension p) { BLOffset = p; }
        public void setAnchorAlign(string p) { AnchorAlign = p; }

        public override void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Frame ";
            os.indent();
            outObjectProperties(os);
            _ = os << '\n' << MifOutputByteStream.INDENT << "<ShapeRect " << ShapeRect << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<FrameType " << FrameType << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<NSOffset " << NSOffset << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<BLOffset " << BLOffset << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<AnchorAlign " << AnchorAlign << ">";
            for (int i = 0; i < Objects.Count; i++) Objects[i].@out(os);
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    public class TextRect : Object
    {
        public T_LTWH ShapeRect;
        public int TRNumColumns;
        public T_dimension TRColumnGap;
        public bool TRColumnBalance;

        public TextRect() : base() { }
        public TextRect(T_LTWH shapeRect, int tRNumColumns = 1, long tRColumnGap = 0, bool tRColumnBalance = false)
            : base()
        {
            setShapeRect(shapeRect);
            setTRNumColumns(tRNumColumns);
            setTRColumnGap(new T_dimension(tRColumnGap));
            setTRColumnBalance(tRColumnBalance);
        }

        public void setShapeRect(T_LTWH p) { ShapeRect = p; }
        public void setTRNumColumns(int p) { TRNumColumns = p; }
        public void setTRColumnGap(T_dimension p) { TRColumnGap = p; }
        public void setTRColumnBalance(bool p) { TRColumnBalance = p; }

        public override void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<TextRect ";
            os.indent();
            outObjectProperties(os);
            _ = os << '\n' << MifOutputByteStream.INDENT << "<ShapeRect " << ShapeRect << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<TRNumColumns " << TRNumColumns << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<TRColumnGap " << TRColumnGap << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<TRColumnBalance " << (TRColumnBalance ? "Yes" : "No") << ">";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    public class Page
    {
        public uint setProperties;
        public string PageType = "";
        public string PageTag = "";
        public string PageBackground = "";
        public System.Collections.Generic.List<TextRect> TextRects = new();

        public Page(string pageType, string pageTag = "", string pageBackground = "")
        {
            PageType = pageType;
            PageTag = pageTag;
            PageBackground = pageBackground;
            setProperties = 0x1;
            if (pageTag != "") setProperties |= 0x2;
            if (pageBackground != "") setProperties |= 0x4;
        }

        public Page() { PageType = sRightMasterPage; setProperties = 0x1; }

        public void setPageType(string p) { PageType = p; setProperties |= 0x1; }
        public void setPageTag(string p) { PageTag = p; setProperties |= 0x2; }
        public void setPageBackground(string p) { PageBackground = p; setProperties |= 0x4; }

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Page ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<PageType " << PageType << ">";
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<PageTag `" << PageTag << "'>";
            if ((setProperties & 0x4) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<PageBackground `" << PageBackground << "'>";
            for (int i = 0; i < TextRects.Count; i++)
                TextRects[i].@out(os);
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // TagStream class
    public class TagStream
    {
        public enum TagStreamClass2 { tsTagStream, tsTextFlow, tsCell, tsPara }
        public TagStreamClass2 TagStreamClass;

        public string InitialPgfTag = "";
        public bool PgfTagUsed;
        protected MifTmpOutputByteStream? Content;
        protected ParagraphFormat Format = new();

        public TagStream(int osIndent = 2)
        {
            Content = new MifTmpOutputByteStream(osIndent);
            PgfTagUsed = false;
            InitialPgfTag = sDefaultPgfFormat;
            TagStreamClass = TagStreamClass2.tsTagStream;
        }

        ~TagStream()
        {
            Content = null;
        }

        public ParagraphFormat format() => Format;
        public MifTmpOutputByteStream content() { System.Diagnostics.Debug.Assert(Content != null); return Content!; }
        public void setParagraphFormat(ParagraphFormat pf) { Format = pf; }
    }

    // TextFlow class
    public class TextFlow : TagStream
    {
        public uint setProperties;
        public uint TextRectID;
        public bool TextRectIDUsed;
        public string TFTag = "";
        public bool TFAutoConnect;
        protected TextRect? TextRect_;

        public TextFlow() : base()
        {
            setProperties = 0;
            TextRectIDUsed = false;
            TagStreamClass = TagStreamClass2.tsTextFlow;
        }

        public TextFlow(TextRect textRect, bool body, ParagraphFormat? format = null, string pgfTag = "")
            : base()
        {
            TextRectID = textRect.ID;
            setProperties = 0;
            TextRectIDUsed = false;
            TextRect_ = textRect;
            TagStreamClass = TagStreamClass2.tsTextFlow;
            InitialPgfTag = pgfTag != "" ? pgfTag : sDefaultPgfFormat;

            if (format != null)
                Format = format;

            if (body)
            {
                setTFTag(sA);
                setTFAutoConnect(true);
            }
        }

        public void setTFTag(string p) { TFTag = p; setProperties |= 0x1; }
        public void setTFAutoConnect(bool p) { TFAutoConnect = p; setProperties |= 0x2; }

        public TextRect? textRect() { System.Diagnostics.Debug.Assert(TextRect_ != null); return TextRect_; }

        public void @out(MifOutputByteStream os, bool resolveCrossReferences = false)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<TextFlow ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TFTag `" << TFTag << "'>";
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TFAutoConnect " << (TFAutoConnect ? "Yes" : "No") << ">";
            content().commit(os.stream(), resolveCrossReferences);
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // Para class
    public class Para : TagStream
    {
        public uint setProperties;
        public static bool currentlyOpened = false;
        public bool leaderTabsSet;
        public string PgfTag = "";
        protected ParagraphFormat CurFormat = new();

        public Para(int osIndent = 2) : base(osIndent)
        {
            setProperties = 0x2;
            leaderTabsSet = false;
            TagStreamClass = TagStreamClass2.tsPara;
        }

        public void setPgfTag(string p) { PgfTag = p; setProperties |= 0x1; }
        public ParagraphFormat curFormat() => CurFormat;

        public void @out(MifOutputByteStream os)
        {
            outProlog(os);
            ParaLine.outProlog(os);
            content().commit(os.stream());
            ParaLine.outEpilog(os);
            outEpilog(os);
        }

        public void outProlog(MifOutputByteStream os)
        {
            currentlyOpened = true;
            outSimpleProlog(os);
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfTag `" << PgfTag << "'>";
            if ((setProperties & 0x2) != 0)  // fParagraphFormat
                curFormat().@out(os);
        }

        public static void outSimpleProlog(MifOutputByteStream os)
        {
            currentlyOpened = true;
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Para ";
            os.indent();
        }

        public static void outEpilog(MifOutputByteStream os)
        {
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            currentlyOpened = false;
        }
    }

    // ParaLine class
    public class ParaLine
    {
        public static uint setProperties = 0;
        public static uint TextRectID;
        public static uint ATbl;

        public static void setTextRectID(uint p) { TextRectID = p; setProperties |= 0x1; }
        public static void setATbl(uint p) { ATbl = p; setProperties |= 0x2; }

        public static void outProlog(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<ParaLine ";
            os.indent();

            TextFlow? tf = MifDoc.CurInstance?.curTextFlow();
            if (tf != null && !tf.TextRectIDUsed)
            {
                setTextRectID(tf.TextRectID);
                if ((setProperties & 0x1) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<TextRectID " << TextRectID << ">";
                setProperties &= ~0x1u;
                tf.TextRectIDUsed = true;
            }

            Tbl? tbl = MifDoc.CurInstance?.curTbl(false);
            if (tbl != null && !tbl.TblIDUsed)
            {
                setATbl(tbl.TblID);
                if ((setProperties & 0x2) != 0)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<ATbl " << ATbl << ">";
                setProperties &= ~0x2u;
                tbl.TblIDUsed = true;
            }
        }

        public static void outEpilog(MifOutputByteStream os)
        {
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // PgfCatalog class
    public class PgfCatalog
    {
        public System.Collections.Generic.List<ParagraphFormat> ParaFormats = new();

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<PgfCatalog ";
            os.indent();
            for (int i = 0; i < ParaFormats.Count; i++)
                ParaFormats[i].@out(os, false);
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // Color class
    public class Color
    {
        public uint setProperties;
        public string ColorTag = "";
        public double ColorCyan;
        public double ColorMagenta;
        public double ColorYellow;
        public double ColorBlack;

        public Color() { setProperties = 0; }

        public Color(byte red, byte green, byte blue)
        {
            setProperties = 0;
            setColorTag($"RGB-{red}-{green}-{blue}");

            if (red >= green && red >= blue)
            {
                setColorBlack((255 - red) / 2.55);
                setColorYellow((red - blue) / 2.55);
                setColorMagenta((red - green) / 2.55);
                setColorCyan(0.0);
            }
            else if (green >= red && green >= blue)
            {
                setColorBlack((255 - green) / 2.55);
                setColorCyan((green - red) / 2.55);
                setColorYellow((green - blue) / 2.55);
                setColorMagenta(0.0);
            }
            else
            {
                setColorBlack((255 - blue) / 2.55);
                setColorCyan((blue - red) / 2.55);
                setColorMagenta((blue - green) / 2.55);
                setColorYellow(0.0);
            }
        }

        public void setColorTag(string p) { ColorTag = p; setProperties |= 0x1; }
        public void setColorCyan(double p) { ColorCyan = p; setProperties |= 0x2; }
        public void setColorMagenta(double p) { ColorMagenta = p; setProperties |= 0x4; }
        public void setColorYellow(double p) { ColorYellow = p; setProperties |= 0x8; }
        public void setColorBlack(double p) { ColorBlack = p; setProperties |= 0x10; }

        public static string key(Color color) => color.ColorTag;

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Color ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<ColorTag `" << ColorTag << "'>";
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<ColorCyan " << ColorCyan << ">";
            if ((setProperties & 0x4) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<ColorMagenta " << ColorMagenta << ">";
            if ((setProperties & 0x8) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<ColorYellow " << ColorYellow << ">";
            if ((setProperties & 0x10) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<ColorBlack " << ColorBlack << ">";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // ColorCatalog class
    public class ColorCatalog
    {
        public Dictionary<string, Color> Colors = new();

        public void @out(MifOutputByteStream os)
        {
            if (Colors.Count > 0)
            {
                _ = os << '\n' << MifOutputByteStream.INDENT << "<ColorCatalog ";
                os.indent();
                foreach (var color in Colors.Values)
                    color.@out(os);
                os.undent();
                _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            }
        }
    }

    // Ruling class
    public class Ruling
    {
        public uint setProperties;
        public string RulingTag = "";
        public long RulingPenWidth = 1000;
        public long RulingGap = 1000;
        public string RulingColor = "";
        public int RulingPen = 0;
        public int RulingLines = 1;
        public string Key = "";

        public Ruling() { setProperties = 0; }
        public Ruling(string rulingTag)
        {
            setProperties = 0;
            setRulingTag(rulingTag);
        }

        public void setRulingTag(string p) { RulingTag = p; setProperties |= 0x1; }
        public void setRulingPenWidth(long p) { RulingPenWidth = p; setProperties |= 0x2; }
        public void setRulingGap(long p) { RulingGap = p; setProperties |= 0x4; }
        public void setRulingColor(string p) { RulingColor = p; setProperties |= 0x20; }
        public void setRulingPen(int p) { RulingPen = p; setProperties |= 0x8; }
        public void setRulingLines(int p) { RulingLines = p; setProperties |= 0x10; }

        public static string key(Ruling r)
        {
            if (r.Key.Length < 1)
            {
                r.Key = $"Ruling-{r.RulingPenWidth}-{r.RulingGap}-{r.RulingPen}-{r.RulingLines}-{r.RulingColor}";
            }
            return r.Key;
        }

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Ruling ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<RulingTag `" << RulingTag << "'>";
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<RulingPenWidth " << new T_dimension(RulingPenWidth) << ">";
            if ((setProperties & 0x4) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<RulingGap " << new T_dimension(RulingGap) << ">";
            if ((setProperties & 0x20) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<RulingColor `" << RulingColor << "'>";
            if ((setProperties & 0x8) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<RulingPen " << RulingPen << ">";
            if ((setProperties & 0x10) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<RulingLines " << RulingLines << ">";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // RulingCatalog class
    public class RulingCatalog
    {
        public Dictionary<string, Ruling> Rulings = new();

        public void @out(MifOutputByteStream os)
        {
            if (Rulings.Count > 0)
            {
                _ = os << '\n' << MifOutputByteStream.INDENT << "<RulingCatalog ";
                os.indent();
                foreach (var ruling in Rulings.Values)
                    ruling.@out(os);
                os.undent();
                _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            }
        }
    }

    // Cell class
    public class Cell : TagStream
    {
        public uint setProperties;
        public int CellFill;
        public string CellColor = "";
        public string CellLRuling = "";
        public string CellBRuling = "";
        public string CellRRuling = "";
        public string CellTRuling = "";
        public int CellColumns;
        public int CellRows;

        public Cell(int osIndent = 12) : base(osIndent)
        {
            setProperties = 0;
            TagStreamClass = TagStreamClass2.tsCell;
        }

        public void setCellFill(int p) { CellFill = p; setProperties |= 0x40; }
        public void setCellColor(string p) { CellColor = p; setProperties |= 0x80; }
        public void setCellLRuling(string p) { CellLRuling = p; setProperties |= 0x1; }
        public void setCellBRuling(string p) { CellBRuling = p; setProperties |= 0x2; }
        public void setCellRRuling(string p) { CellRRuling = p; setProperties |= 0x4; }
        public void setCellTRuling(string p) { CellTRuling = p; setProperties |= 0x8; }
        public void setCellColumns(int p) { CellColumns = p; setProperties |= 0x10; }
        public void setCellRows(int p) { CellRows = p; setProperties |= 0x20; }

        public void @out(MifOutputByteStream os, bool resolveCrossReferences = false)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Cell ";
            os.indent();
            if ((setProperties & 0x40) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<CellFill " << CellFill << ">";
            if ((setProperties & 0x80) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<CellColor `" << CellColor << "'>";
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<CellLRuling `" << CellLRuling << "'>";
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<CellBRuling `" << CellBRuling << "'>";
            if ((setProperties & 0x4) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<CellRRuling `" << CellRRuling << "'>";
            if ((setProperties & 0x8) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<CellTRuling `" << CellTRuling << "'>";
            if ((setProperties & 0x10) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<CellColumns " << CellColumns << ">";
            if ((setProperties & 0x20) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<CellRows " << CellRows << ">";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<CellContent ";
            os.indent();
            content().commit(os.stream(), resolveCrossReferences);
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // Row class
    public class Row
    {
        public System.Collections.Generic.List<Cell> Cells = new();

        public void @out(MifOutputByteStream os, bool resolveCrossReferences = false)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Row ";
            os.indent();
            for (int i = 0; i < Cells.Count; i++)
                Cells[i].@out(os, resolveCrossReferences);
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // TblColumn class
    public class TblColumn
    {
        public uint setProperties;
        public int TblColumnNum = 0;
        public long TblColumnWidth = 0;

        public TblColumn() { setProperties = 0; }
        public TblColumn(int tblColumnNum, long tblColumnWidth)
        {
            setProperties = 0;
            setTblColumnNum(tblColumnNum);
            setTblColumnWidth(tblColumnWidth);
        }

        public void setTblColumnNum(int p) { TblColumnNum = p; setProperties |= 0x1; }
        public void setTblColumnWidth(long p) { TblColumnWidth = p; setProperties |= 0x2; }

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<TblFormat ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblColumnNum " << TblColumnNum << ">";
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblColumnWidth " << new T_dimension(TblColumnWidth) << ">";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // TblFormat class
    public class TblFormat
    {
        public uint setProperties;
        public string TblTag = "";
        public long TblLIndent;
        public long TblRIndent;
        public long TblSpBefore;
        public long TblSpAfter;
        public string TblAlignment = "";
        public T_LTRB TblCellMargins;
        public string TblTitlePlacement = "";
        public long TblWidth;
        public System.Collections.Generic.List<TblColumn> TblColumns = new();

        public TblFormat() { setProperties = 0; }
        public TblFormat(string tblTag)
        {
            setProperties = 0;
            setTblTag(tblTag);
        }

        public void setDSSSLDefaults()
        {
            setTblLIndent(0);
            setTblRIndent(0);
            setTblSpBefore(0);
            setTblSpAfter(0);
            setTblAlignment(sLeft);
            setTblCellMargins(new T_LTRB(new T_dimension(0), new T_dimension(0), new T_dimension(0), new T_dimension(0)));
            setTblTitlePlacement(sNone);
        }

        public void setTblTag(string p) { TblTag = p; setProperties |= 0x1; }
        public void setTblLIndent(long p) { TblLIndent = p; setProperties |= 0x2; }
        public void setTblRIndent(long p) { TblRIndent = p; setProperties |= 0x4; }
        public void setTblSpBefore(long p) { TblSpBefore = p; setProperties |= 0x8; }
        public void setTblSpAfter(long p) { TblSpAfter = p; setProperties |= 0x10; }
        public void setTblAlignment(string p) { TblAlignment = p; setProperties |= 0x20; }
        public void setTblCellMargins(T_LTRB p) { TblCellMargins = p; setProperties |= 0x40; }
        public void setTblTitlePlacement(string p) { TblTitlePlacement = p; setProperties |= 0x100; }
        public void setTblWidth(long p) { TblWidth = p; setProperties |= 0x80; }

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<TblFormat ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblTag `" << TblTag << "'>";
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblLIndent " << new T_dimension(TblLIndent) << ">";
            if ((setProperties & 0x4) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblRIndent " << new T_dimension(TblRIndent) << ">";
            if ((setProperties & 0x8) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblSpBefore " << new T_dimension(TblSpBefore) << ">";
            if ((setProperties & 0x10) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblSpAfter " << new T_dimension(TblSpAfter) << ">";
            if ((setProperties & 0x20) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblAlignment " << TblAlignment << ">";
            if ((setProperties & 0x40) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblCellMargins " << TblCellMargins << ">";
            if ((setProperties & 0x100) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblTitlePlacement " << TblTitlePlacement << ">";
            if ((setProperties & 0x80) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblWidth " << new T_dimension(TblWidth) << ">";
            for (int i = 0; i < TblColumns.Count; i++)
                TblColumns[i].@out(os);
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // TblCatalog class
    public class TblCatalog
    {
        public System.Collections.Generic.List<TblFormat> TblFormats = new();

        public void @out(MifOutputByteStream os)
        {
            if (TblFormats.Count > 0)
            {
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblCatalog ";
                os.indent();
                foreach (var tblFormat in TblFormats)
                    tblFormat.@out(os);
                os.undent();
                _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            }
        }
    }

    // Tbl class
    public class Tbl
    {
        [Flags]
        public enum TblFlags : uint
        {
            fNone = 0,
            fTblID = 0x1,
            fTblTag = 0x2,
            fTblNumColumns = 0x4,
            fTblColumnWidths = 0x8,
            fTblFormat = 0x200
        }

        public uint setProperties;
        public uint TblID;
        public string TblTag = "";
        public bool TblIDUsed;
        public TblFormat tblFormat = new();
        public int TblNumColumns;
        public System.Collections.Generic.List<long> TblColumnWidths = new();
        public System.Collections.Generic.List<Row> TblH = new();
        public System.Collections.Generic.List<Row> TblBody = new();
        public System.Collections.Generic.List<Row> TblF = new();

        public Tbl()
        {
            setProperties = 0;
            TblIDUsed = false;
            setTblID(CurInstance!.nextID());
            setTblTag(sDefaultTblFormat);
        }

        public void setTblID(uint p) { TblID = p; setProperties |= 0x1; }
        public void setTblTag(string p) { TblTag = p; setProperties |= 0x2; }
        public void setTblNumColumns(int p) { TblNumColumns = p; setProperties |= 0x4; }
        public void TblColumnWidthsAreSet() { setProperties |= 0x8; }

        public void @out(MifOutputByteStream os, bool resolveCrossReferences = false)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Tbl ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblID " << TblID << ">";
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblTag `" << TblTag << "'>";

            if ((setProperties & 0x200) != 0)  // fTblFormat
                tblFormat.@out(os);

            if ((setProperties & 0x4) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblNumColumns " << TblNumColumns << ">";
            if ((setProperties & 0x8) != 0)
                for (int i = 0; i < TblColumnWidths.Count; i++)
                    _ = os << '\n' << MifOutputByteStream.INDENT << "<TblColumnWidth " << new T_dimension(TblColumnWidths[i]) << ">";

            if (TblH.Count > 0)
            {
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblH ";
                os.indent();
                for (int i = 0; i < TblH.Count; i++)
                    TblH[i].@out(os, resolveCrossReferences);
                os.undent();
                _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            }

            if (TblBody.Count > 0)
            {
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblBody ";
                os.indent();
                for (int i = 0; i < TblBody.Count; i++)
                    TblBody[i].@out(os, resolveCrossReferences);
                os.undent();
                _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            }

            if (TblF.Count > 0)
            {
                _ = os << '\n' << MifOutputByteStream.INDENT << "<TblF ";
                os.indent();
                for (int i = 0; i < TblF.Count; i++)
                    TblF[i].@out(os, resolveCrossReferences);
                os.undent();
                _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            }

            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // XRefFormat class
    public class XRefFormat
    {
        public uint setProperties;
        public string XRefName = "";
        public string XRefDef = "";

        public XRefFormat() { setProperties = 0; }
        public XRefFormat(string xRefName, string xRefDef)
        {
            setXRefName(xRefName);
            setXRefDef(xRefDef);
        }

        public void setXRefName(string p) { XRefName = p; setProperties |= 0x1; }
        public void setXRefDef(string p) { XRefDef = p; setProperties |= 0x2; }

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<XRefFormat ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<XRefName `" << XRefName << "'>";
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<XRefDef `" << XRefDef << "'>";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // CrossRefInfo class
    public class CrossRefInfo
    {
        public enum InfoType { PotentialMarker, XRef, HypertextLink, HypertextDestination }
        public InfoType Type;
        public ulong groveIndex;
        public ulong elementIndex;
        public int tagIndent;
        public StringC sgmlId = new();

        public CrossRefInfo() { }

        public CrossRefInfo(ulong groveIndex_, ulong elementIndex_, int tagIndent_,
                           InfoType type_, Char[]? id, nuint idLen)
        {
            groveIndex = groveIndex_;
            elementIndex = elementIndex_;
            tagIndent = tagIndent_;
            Type = type_;
            if (id != null && idLen > 0)
                sgmlId.assign(id, idLen);
        }

        public InfoType type() => Type;

        public string crossRefText()
        {
            if (sgmlId.size() > 0)
            {
                var sb = new StringBuilder($"NODE{groveIndex}.");
                for (nuint i = 0; i < sgmlId.size(); i++)
                    sb.Append((char)sgmlId[i]);
                return sb.ToString();
            }
            else
            {
                return $"NODE{groveIndex}.{elementIndex}";
            }
        }

        public void @out(MifOutputByteStream os)
        {
            // CrossRefInfo doesn't output itself directly - it's used to construct other objects
        }
    }

    // XRef class
    public class XRef
    {
        public uint setProperties;
        public string XRefName = "";
        public string XRefSrcText = "";
        public string XRefSrcFile = "";
        public string XRefText = "";

        public XRef() { setProperties = 0; }

        public XRef(CrossRefInfo crossRefInfo)
        {
            setProperties = 0;
            switch (crossRefInfo.type())
            {
                case CrossRefInfo.InfoType.XRef:
                    {
                        int bookComponentIdx = crossRefInfo.sgmlId.size() > 0
                            ? MifDoc.CurInstance!.elements().bookComponentIndex(crossRefInfo.groveIndex, crossRefInfo.sgmlId)
                            : MifDoc.CurInstance!.elements().bookComponentIndex(crossRefInfo.groveIndex, crossRefInfo.elementIndex);
                        string targetFileName = "<c\\>" + MifDoc.CurInstance.bookComponents()[bookComponentIdx].FileName;
                        setXRefSrcFile(targetFileName);
                        setXRefName(MifDoc.sPageNumXRefFormatName);
                        setXRefSrcText(crossRefInfo.crossRefText());
                        setXRefText("000");
                    }
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
        }

        public XRef(string xRefName, string xRefSrcText, string xRefText, string xRefSrcFile)
        {
            setXRefName(xRefName);
            setXRefSrcText(xRefSrcText);
            setXRefText(xRefText);
            setXRefSrcFile(xRefSrcFile);
        }

        public void setXRefName(string p) { XRefName = p; setProperties |= 0x1; }
        public void setXRefSrcText(string p) { XRefSrcText = p; setProperties |= 0x4; }
        public void setXRefSrcFile(string p) { XRefSrcFile = p; setProperties |= 0x2; }
        public void setXRefText(string p) { XRefText = p; setProperties |= 0x8; }

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<XRef ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<XRefName `" << XRefName << "'>";
            if ((setProperties & 0x4) != 0)
            {
                // escapeSpecialChars in XRefSrcText
                var escaped = new T_string(XRefSrcText);
                escaped.escapeSpecialChars();
                _ = os << '\n' << MifOutputByteStream.INDENT << "<XRefSrcText `" << escaped.ToString() << "'>";
            }
            if ((setProperties & 0x2) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<XRefSrcFile `" << XRefSrcFile << "'>";
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
            if ((setProperties & 0x8) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<String `" << XRefText << "'>";
            _ = os << '\n' << MifOutputByteStream.INDENT << "<XRefEnd>";
        }
    }

    // Marker class
    public class Marker
    {
        public enum MarkerType { XRef = 9, Index = 2, Hypertext = 8, HypertextLink = 100, HypertextDestination = 101 }
        public uint setProperties;
        public int MType;
        public string MText = "";

        public Marker() { setProperties = 0; }

        public Marker(CrossRefInfo crossRefInfo, bool linkDestinationMode = false)
        {
            setProperties = 0;
            switch (crossRefInfo.type())
            {
                case CrossRefInfo.InfoType.PotentialMarker:
                    if (linkDestinationMode)
                    {
                        setMType((int)MarkerType.Hypertext);
                        setMText("newlink " + crossRefInfo.crossRefText());
                    }
                    else
                    {
                        setMType((int)MarkerType.XRef);
                        setMText(crossRefInfo.crossRefText());
                    }
                    break;
                case CrossRefInfo.InfoType.HypertextLink:
                    {
                        setMType((int)MarkerType.Hypertext);
                        int bookComponentIdx = crossRefInfo.sgmlId.size() > 0
                            ? MifDoc.CurInstance!.elements().bookComponentIndex(crossRefInfo.groveIndex, crossRefInfo.sgmlId)
                            : MifDoc.CurInstance!.elements().bookComponentIndex(crossRefInfo.groveIndex, crossRefInfo.elementIndex);
                        string targetFileName = "<c>" + MifDoc.CurInstance.bookComponents()[bookComponentIdx].FileName;
                        string mtext = "gotolink " + targetFileName + ":" + crossRefInfo.crossRefText();
                        setMText(mtext);
                    }
                    break;
                case CrossRefInfo.InfoType.HypertextDestination:
                    setMText("newlink " + crossRefInfo.crossRefText());
                    break;
                default:
                    break;
            }
        }

        public Marker(string mText, MarkerType mType = MarkerType.XRef)
        {
            setMText(mText);
            setMType((int)mType);
        }

        public void setMType(int p) { MType = p; setProperties |= 0x1; }
        public void setMText(string p) { MText = p; setProperties |= 0x2; }

        public void @out(MifOutputByteStream os)
        {
            _ = os << '\n' << MifOutputByteStream.INDENT << "<Marker ";
            os.indent();
            if ((setProperties & 0x1) != 0)
                _ = os << '\n' << MifOutputByteStream.INDENT << "<MType " << MType << ">";
            if ((setProperties & 0x2) != 0)
            {
                var escaped = new T_string(MText);
                escaped.escapeSpecialChars();
                _ = os << '\n' << MifOutputByteStream.INDENT << "<MText `" << escaped.ToString() << "'>";
            }
            os.undent();
            _ = os << '\n' << MifOutputByteStream.INDENT << ">";
        }
    }

    // BookComponent class
    public class BookComponent
    {
        public string FileName = "";
        public System.Collections.Generic.List<XRefFormat> XRefFormats = new();
        public Document document = new();
        public ColorCatalog colorCatalog = new();
        public PgfCatalog pgfCatalog = new();
        public TblCatalog tblCatalog = new();
        public System.Collections.Generic.List<Frame> AFrames = new();
        public System.Collections.Generic.List<Tbl> Tbls = new();
        public System.Collections.Generic.List<Page> Pages = new();
        public System.Collections.Generic.List<TextFlow> TextFlows = new();
        public RulingCatalog rulingCatalog = new();
        public bool pageNumXRefFormatGenerated = false;
        public MifTmpOutputByteStream? epilogOs;

        public BookComponent() { }

        public BookComponent(MifTmpOutputByteStream? epilogOs_)
        {
            epilogOs = epilogOs_;
        }

        public void commit(string dirName, bool resolveCrossReferences = true)
        {
            System.Diagnostics.Debug.Assert(FileName.Length > 0);
            string fileLoc = dirName + FileName;

            using (var outFileStream = new System.IO.FileStream(fileLoc, System.IO.FileMode.Create))
            {
                var outFile = new FileOutputByteStream();
                outFile.attach(outFileStream);

                MifOutputByteStream os = new MifOutputByteStream(0);
                os.setStream(outFile);

                _ = os << "<MIFFile 5.0>";

                colorCatalog.@out(os);
                pgfCatalog.@out(os);
                rulingCatalog.@out(os);

                if (AFrames.Count > 0)
                {
                    _ = os << "\n<AFrames ";
                    os.indent();
                    for (int i = 0; i < AFrames.Count; i++)
                        AFrames[i].@out(os);
                    os.undent();
                    _ = os << '\n' << ">";
                }

                if (XRefFormats.Count > 0)
                {
                    _ = os << "\n<XRefFormats ";
                    os.indent();
                    for (int i = 0; i < XRefFormats.Count; i++)
                        XRefFormats[i].@out(os);
                    os.undent();
                    _ = os << '\n' << ">";
                }

                tblCatalog.@out(os);
                document.@out(os);

                if (Tbls.Count > 0)
                {
                    _ = os << "\n<Tbls ";
                    os.indent();
                    for (int i = 0; i < Tbls.Count; i++)
                        Tbls[i].@out(os, resolveCrossReferences);
                    os.undent();
                    _ = os << '\n' << ">";
                }

                for (int i = 0; i < Pages.Count; i++)
                    Pages[i].@out(os);
                for (int i = 0; i < TextFlows.Count; i++)
                    TextFlows[i].@out(os, resolveCrossReferences);

                if (epilogOs != null)
                    epilogOs.commit(os.stream(), resolveCrossReferences);
            }
        }
    }

    // ElementSet class
    public class ElementSet
    {
        public enum ReferenceType { AnyReference = 0xC000, LinkReference = 0x8000, PageReference = 0x4000 }

        public static ushort LINK_TYPE_MASK(ReferenceType refType) => (ushort)refType;
        public static ushort BOOK_COMPONENT_INDEX_M() => 0x3FFF;

        public class SgmlIdInfo
        {
            public StringC sgmlId;
            public ushort flags;
            public ulong groveIndex;

            public SgmlIdInfo(StringC sgmlId_, ulong groveIndex_)
            {
                sgmlId = sgmlId_;
                groveIndex = groveIndex_;
                flags = 0;
            }

            public static StringC key(SgmlIdInfo sgmlIdInfo) => sgmlIdInfo.sgmlId;
        }

        private Dictionary<StringC, SgmlIdInfo> SgmlIdInfos = new();
        private System.Collections.Generic.List<System.Collections.Generic.List<ushort>> Flags_ = new();

        public ElementSet() { }

        public SgmlIdInfo enforceSgmlId(StringC sgmlId, ulong groveIndex)
        {
            if (!SgmlIdInfos.TryGetValue(sgmlId, out var result))
            {
                result = new SgmlIdInfo(sgmlId, groveIndex);
                SgmlIdInfos[sgmlId] = result;
            }
            return result;
        }

        public void setReferencedFlag(ReferenceType refType, ulong groveIndex, ulong n)
        {
            System.Diagnostics.Debug.Assert(refType != ReferenceType.AnyReference);
            ushort flags = 0;
            getFlags(groveIndex, n, out flags);
            add(groveIndex, n, (ushort)(flags | LINK_TYPE_MASK(refType)));
        }

        public void setReferencedFlag(ReferenceType refType, ulong groveIndex, StringC sgmlId)
        {
            var sgmlIdInfo = enforceSgmlId(sgmlId, groveIndex);
            System.Diagnostics.Debug.Assert(refType != ReferenceType.AnyReference);
            sgmlIdInfo.flags |= LINK_TYPE_MASK(refType);
        }

        public void setBookComponentIndex(ulong groveIndex, ulong n, int i)
        {
            ushort flags = 0;
            getFlags(groveIndex, n, out flags);
            add(groveIndex, n, (ushort)((flags & LINK_TYPE_MASK(ReferenceType.AnyReference)) | (i & BOOK_COMPONENT_INDEX_M())));
        }

        public void setBookComponentIndex(ulong groveIndex, StringC sgmlId, int i)
        {
            var sgmlIdInfo = enforceSgmlId(sgmlId, groveIndex);
            ushort flags = sgmlIdInfo.flags;
            sgmlIdInfo.flags = (ushort)((flags & LINK_TYPE_MASK(ReferenceType.AnyReference)) | (i & BOOK_COMPONENT_INDEX_M()));
        }

        public bool hasBeenReferenced(ReferenceType refType, ulong groveIndex, ulong n)
        {
            ushort flags = 0;
            getFlags(groveIndex, n, out flags);
            return (flags & LINK_TYPE_MASK(refType)) != 0;
        }

        public bool hasBeenReferenced(ReferenceType refType, ulong groveIndex, StringC sgmlId)
        {
            var sgmlIdInfo = enforceSgmlId(sgmlId, groveIndex);
            return (sgmlIdInfo.flags & LINK_TYPE_MASK(refType)) != 0;
        }

        public int bookComponentIndex(ulong groveIndex, ulong n)
        {
            ushort flags;
            if (getFlags(groveIndex, n, out flags))
                return flags & BOOK_COMPONENT_INDEX_M();
            System.Diagnostics.Debug.Assert(false);
            return 0;
        }

        public int bookComponentIndex(ulong groveIndex, StringC sgmlId)
        {
            var sgmlIdInfo = enforceSgmlId(sgmlId, groveIndex);
            return sgmlIdInfo.flags & BOOK_COMPONENT_INDEX_M();
        }

        private void add(ulong groveIndex, ulong n, ushort flags)
        {
            while ((ulong)Flags_.Count <= groveIndex)
                Flags_.Add(new System.Collections.Generic.List<ushort>());
            var elems = Flags_[(int)groveIndex];
            while ((ulong)elems.Count <= n)
                elems.Add(0);
            elems[(int)n] = flags;
        }

        private bool getFlags(ulong groveIndex, ulong n, out ushort result)
        {
            result = 0;
            if ((ulong)Flags_.Count > groveIndex && (ulong)Flags_[(int)groveIndex].Count > n)
            {
                result = Flags_[(int)groveIndex][(int)n];
                return true;
            }
            return false;
        }
    }

    // Instance methods
    public void commit()
    {
        string outDir = rootOutputFileLoc();
        string outFileName = "";

        // Extract filename from path
        int i;
        for (i = outDir.Length - 1; i >= 0; i--)
            if (outDir[i] == '/' || outDir[i] == '\\')
                break;
        if (outDir.Length - (i + 1) > 0)
            outFileName = outDir.Substring(i + 1);
        outDir = outDir.Substring(0, i + 1);

        if (BookComponents_.Count > 1)
        {
            string bookFileLoc = rootOutputFileLoc();
            string fileNameExt = "";

            // Extract file extension
            int idx;
            for (idx = bookFileLoc.Length; idx > 0; idx--)
                if (bookFileLoc[idx - 1] == '.')
                    break;
            if (idx > 0 && bookFileLoc.Length - idx > 0)
                fileNameExt = bookFileLoc.Substring(idx);
            else
                fileNameExt = "mif";

            // Assign filenames to book components
            for (int j = 0; j < BookComponents_.Count; j++)
            {
                string fileName = (j + 1).ToString() + "." + fileNameExt;
                BookComponents_[j].FileName = fileName;
            }

            // Write book file
            try
            {
                using (var bookFileStream = new System.IO.FileStream(bookFileLoc, System.IO.FileMode.Create))
                {
                    var bookFile = new FileOutputByteStream();
                    bookFile.attach(bookFileStream);

                    MifOutputByteStream os = new MifOutputByteStream(0);
                    os.setStream(bookFile);

                    _ = os << "<Book 5.0>";
                    for (int j = 0; j < BookComponents_.Count; j++)
                    {
                        _ = os << "\n<BookComponent"
                              << "\n  <FileName `<c\\>" << BookComponents_[j].FileName << "'>"
                              << "\n>";
                    }
                }
            }
            catch (System.Exception)
            {
                App.message(MifMessages.cannotOpenOutputError,
                    new StringMessageArg(new StringC(bookFileLoc)),
                    new ErrnoMessageArg(0));
            }
        }
        else if (BookComponents_.Count == 1)
        {
            BookComponents_[0].FileName = outFileName;
        }

        // Commit all book components
        for (int j = 0; j < BookComponents_.Count; j++)
            BookComponents_[j].commit(outDir);
    }

    public System.Collections.Generic.List<BookComponent> bookComponents() => BookComponents_;
    public BookComponent bookComponent()
    {
        System.Diagnostics.Debug.Assert(BookComponents_.Count > 0);
        return BookComponents_[BookComponents_.Count - 1];
    }

    public void setCurOs(MifOutputByteStream os) { curOs_ = os; }
    public MifOutputByteStream os() { System.Diagnostics.Debug.Assert(curOs_ != null); return curOs_!; }

    public void setCurTextFlow(TextFlow? tf) { CurTextFlow_ = tf; }
    public TextFlow? curTextFlow() => CurTextFlow_;

    public void setCurCell(Cell? c) { CurCell_ = c; }
    public Cell? curCell() => CurCell_;

    public void setCurTblNum(nuint n) { CurTblNum_ = n; }
    public Tbl? curTbl(bool assertNotNull = true)
    {
        if (assertNotNull) System.Diagnostics.Debug.Assert(CurTblNum_ > 0);
        return CurTblNum_ > 0 ? bookComponent().Tbls[(int)CurTblNum_ - 1] : null;
    }

    public void setCurPara(Para? p) { CurPara_ = p; }
    public Para? curPara(bool assertNotNull = true)
    {
        if (assertNotNull) System.Diagnostics.Debug.Assert(CurPara_ != null);
        return CurPara_;
    }

    public void setCurParagraphFormat(ParagraphFormat pf) { CurFormat_ = pf; }
    public ParagraphFormat curFormat() => CurFormat_;

    public TagStream curTagStream()
    {
        System.Diagnostics.Debug.Assert(TagStreamStack_.Count > 0);
        return TagStreamStack_[TagStreamStack_.Count - 1];
    }

    public void enterTextFlow(TextFlow textFlow) { enterTagStream(textFlow); }
    public void exitTextFlow() { setCurTextFlow(null); exitTagStream(); }

    public void enterTableCell(Cell cell) { enterTagStream(cell); }
    public void exitTableCell() { setCurCell(null); exitTagStream(); }

    public void enterPara(Para p) { enterTagStream(p); }
    public void exitPara() { setCurPara(null); exitTagStream(); }

    public void enterBookComponent()
    {
        var defaultTagStream = new TagStream(0);
        enterTagStream(defaultTagStream);
        var newBookComponent = new BookComponent(defaultTagStream.content());
        BookComponents_.Add(newBookComponent);
    }

    public void exitBookComponent()
    {
        exitTagStream();
    }

    public void enterTagStream(TagStream tagStream)
    {
        TagStreamStack_.Add(tagStream);
        switchToTagStream(TagStreamStack_[TagStreamStack_.Count - 1]);
    }

    public void exitTagStream()
    {
        bool startWithDefaultPgfFormat = true;
        System.Diagnostics.Debug.Assert(TagStreamStack_.Count > 0);
        if (TagStreamStack_[TagStreamStack_.Count - 1].TagStreamClass == TagStream.TagStreamClass2.tsPara)
        {
            Para.currentlyOpened = false;
            startWithDefaultPgfFormat = false;
        }
        TagStreamStack_.RemoveAt(TagStreamStack_.Count - 1);
        if (TagStreamStack_.Count >= 1)
            switchToTagStream(TagStreamStack_[TagStreamStack_.Count - 1], startWithDefaultPgfFormat);
    }

    public uint nextID() => ++NextID_;

    public Document document() => bookComponent().document;
    public PgfCatalog pgfCatalog() => bookComponent().pgfCatalog;
    public TblCatalog tblCatalog() => bookComponent().tblCatalog;
    public RulingCatalog rulingCatalog() => bookComponent().rulingCatalog;
    public ColorCatalog colorCatalog() => bookComponent().colorCatalog;
    public System.Collections.Generic.List<Tbl> tbls() => bookComponent().Tbls;
    public System.Collections.Generic.List<Page> pages() => bookComponent().Pages;
    public System.Collections.Generic.List<TextFlow> textFlows() => bookComponent().TextFlows;
    public System.Collections.Generic.List<Frame> aFrames() => bookComponent().AFrames;
    public ElementSet elements() => Elements_;
    public System.Collections.Generic.List<CrossRefInfo> crossRefInfos() => CrossRefInfos_;

    public string rootOutputFileLoc() => RootOutputFileLoc_;

    public void outTagEnd() { os().stream().operatorOutput(">"); }

    public void outHexChar(uint code, MifOutputByteStream? o = null)
    {
        var outS = o ?? os();
        outS.stream().operatorOutput($"\\x{code:x2} ");
    }

    public void outHexChar(uint code, T_string targetString)
    {
        targetString.append($"\\x{code:x2} ", 6);
    }

    public void outSpecialChar(string charName, MifOutputByteStream? o = null)
    {
        var outS = o ?? os();
        outS.stream().operatorOutput($"\n <Char {charName}>");
    }

    public void beginParaLine()
    {
        ParaLine.outProlog(os());
    }

    public void endParaLine()
    {
        ParaLine.outEpilog(os());
    }

    public void outPageNumber()
    {
        os().stream().operatorOutput("\n <Variable <VariableName `Current Page #'>>");
    }

    public void outBreakingPara(string pgfPlacement)
    {
        curFormat().setPgfSpBefore(0);
        curFormat().setPgfSpAfter(0);
        curFormat().setPgfPlacement(pgfPlacement);
        Para.outSimpleProlog(os());
        curFormat().@out(os());
        ParaLine.outProlog(os());
        ParaLine.outEpilog(os());
        Para.outEpilog(os());
    }

    public void outAFrame(uint ID, MifOutputByteStream os)
    {
        os.stream().operatorOutput($"\n <AFrame {ID}>");
    }

    protected void switchToTagStream(TagStream tagStream, bool startWithDefaultPgfFormat = true)
    {
        if (tagStream.TagStreamClass == TagStream.TagStreamClass2.tsTextFlow)
            setCurTextFlow((TextFlow)tagStream);
        else if (tagStream.TagStreamClass == TagStream.TagStreamClass2.tsCell)
            setCurCell((Cell)tagStream);

        if (tagStream.TagStreamClass == TagStream.TagStreamClass2.tsPara)
        {
            Para.currentlyOpened = true;
        }
        else if (startWithDefaultPgfFormat)
        {
            tagStream.PgfTagUsed = false;
            var defaultPgfFormat = new ParagraphFormat();
            defaultPgfFormat.setDSSSLDefaults();
            setCurParagraphFormat(defaultPgfFormat);
        }
        setCurOs(tagStream.content().stream());
    }

    // Protected fields
    protected System.Collections.Generic.List<CrossRefInfo> CrossRefInfos_;
    protected ElementSet Elements_;

    protected string RootOutputFileLoc_;
    protected uint NextID_;

    protected MifOutputByteStream? curOs_;

    protected System.Collections.Generic.List<BookComponent> BookComponents_;

    protected nuint CurTblNum_;
    protected ParagraphFormat CurFormat_ = new();
    protected TextFlow? CurTextFlow_;
    protected Cell? CurCell_;
    protected Para? CurPara_;

    protected System.Collections.Generic.List<TagStream> TagStreamStack_;
}

// MifOutputByteStream class
public class MifOutputByteStream
{
    private OutputByteStream? os_;
    public int CurTagIndent;
    public static T_indent INDENT = new T_indent(0xFFF);

    public void indent() { CurTagIndent += 2; }
    public void undent() { CurTagIndent -= 2; System.Diagnostics.Debug.Assert(CurTagIndent >= 0); }

    public MifOutputByteStream(int i = 0)
    {
        os_ = null;
        CurTagIndent = i;
    }

    public MifOutputByteStream(OutputByteStream os, int i = 0)
    {
        os_ = os;
        CurTagIndent = i;
    }

    public void setStream(OutputByteStream s) { os_ = s; }
    public OutputByteStream stream() { System.Diagnostics.Debug.Assert(os_ != null); return os_!; }

    public struct T_indent
    {
        public int data;
        public T_indent(int i) { data = i; }
    }

    // Operator overloads for output chaining (ported from C++)
    public static MifOutputByteStream operator <<(MifOutputByteStream os, char c)
    {
        os.stream().operatorOutput(c);
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, string s)
    {
        os.stream().operatorOutput(s);
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, int n)
    {
        os.stream().operatorOutput(n.ToString());
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, uint n)
    {
        os.stream().operatorOutput(n.ToString());
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, long n)
    {
        os.stream().operatorOutput(n.ToString());
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, T_indent i)
    {
        int cnt = (i.data == INDENT.data) ? os.CurTagIndent : i.data;
        for (; cnt > 0; cnt--) os.stream().operatorOutput(' ');
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, MifDoc.T_dimension d)
    {
        long val = d.data;
        string buf = string.Format("{0}.{1:D3}", val / 1000, Math.Abs(val % 1000)).TrimEnd('0').TrimEnd('.');
        os.stream().operatorOutput(buf + "pt");
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, MifDoc.T_string s)
    {
        os.stream().operatorOutput('`');
        os.stream().operatorOutput(s.ToString());
        os.stream().operatorOutput('\'');
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, MifDoc.T_tagstring s)
    {
        os.stream().operatorOutput('`');
        os.stream().operatorOutput(s.ToString());
        os.stream().operatorOutput('\'');
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, MifDoc.T_boolean b)
    {
        os.stream().operatorOutput(b.data ? "Yes" : "No");
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, double p)
    {
        os.stream().operatorOutput(string.Format("{0:F6}", p));
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, MifDoc.T_WH s)
    {
        os = os << s.w << " " << s.h;
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, MifDoc.T_XY s)
    {
        os = os << s.x << " " << s.y;
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, MifDoc.T_LTRB b)
    {
        os = os << b.l << " " << b.t << " " << b.r << " " << b.b;
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, MifDoc.T_LTWH b)
    {
        os = os << b.l << " " << b.t << " " << b.w << " " << b.h;
        return os;
    }

    public static MifOutputByteStream operator <<(MifOutputByteStream os, MifDoc.CrossRefInfo cri)
    {
        var crossRefInfos = MifDoc.CurInstance!.crossRefInfos();
        uint idx = (uint)crossRefInfos.Count;
        crossRefInfos.Add(cri);
        os.stream().operatorOutput(MifDoc.escapeChar());
        byte[] idxBytes = BitConverter.GetBytes(idx);
        foreach (byte b in idxBytes) os.stream().sputc((sbyte)b);
        return os;
    }
}

// TmpOutputByteStream - temporary output byte stream (memory-based)
public class TmpOutputByteStream : OutputByteStream
{
    private MemoryStream buffer_ = new MemoryStream();

    public TmpOutputByteStream() : base() { }

    // Required abstract method implementations from OutputByteStream
    public override void flush()
    {
        // Memory stream doesn't need flushing
    }

    public override void flushBuf(sbyte c)
    {
        buffer_.WriteByte((byte)c);
    }

    // Additional convenience methods
    public void sputc(byte c)
    {
        buffer_.WriteByte(c);
    }

    public void sputn(byte[] s, nuint n)
    {
        buffer_.Write(s, 0, (int)n);
    }

    public void sputn(string s, int n)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        buffer_.Write(bytes, 0, Math.Min(n, bytes.Length));
    }

    public bool isEmpty() => buffer_.Length == 0;
    public long length() => buffer_.Length;

    public void commit(OutputByteStream target)
    {
        var data = buffer_.ToArray();
        var sbyteData = new sbyte[data.Length];
        for (int i = 0; i < data.Length; i++)
            sbyteData[i] = (sbyte)data[i];
        target.sputn(sbyteData, (nuint)sbyteData.Length);
    }

    public byte[] getBuffer() => buffer_.ToArray();

    public class Iter
    {
        private byte[] data_;
        private int offset_ = 0;

        public Iter(TmpOutputByteStream stream)
        {
            data_ = stream.getBuffer();
        }

        public bool next(out string s, out int n)
        {
            if (offset_ >= data_.Length)
            {
                s = "";
                n = 0;
                return false;
            }
            s = System.Text.Encoding.UTF8.GetString(data_, offset_, data_.Length - offset_);
            n = data_.Length - offset_;
            offset_ = data_.Length;
            return true;
        }
    }
}

// MifTmpOutputByteStream class
public class MifTmpOutputByteStream : TmpOutputByteStream
{
    private MifOutputByteStream os_;

    public MifTmpOutputByteStream(int osIndent = 0) : base()
    {
        os_ = new MifOutputByteStream(osIndent);
        os_.setStream(this);
    }

    public void commit(OutputByteStream os, bool resolveCrossReferences = false)
    {
        MifOutputByteStream outS = new MifOutputByteStream(0);
        outS.setStream(os);

        var data = getBuffer();
        int i = 0;

        while (i < data.Length)
        {
            if (resolveCrossReferences && data[i] == (byte)MifDoc.escapeChar())
            {
                // Read the cross-reference index (4 bytes, little endian)
                i++;
                if (i + 4 <= data.Length)
                {
                    uint crossRefInfoIdx = BitConverter.ToUInt32(data, i);
                    i += 4;

                    var crossRefInfos = MifDoc.CurInstance?.crossRefInfos();
                    if (crossRefInfos != null && crossRefInfoIdx < crossRefInfos.Count)
                    {
                        MifDoc.CrossRefInfo crossRefInfo = crossRefInfos[(int)crossRefInfoIdx];
                        crossRefInfo.@out(outS);
                    }
                }
            }
            else
            {
                os.sputc((sbyte)data[i]);
                i++;
            }
        }
    }

    public void commit(StringBuilder str)
    {
        var data = getBuffer();
        str.Append(System.Text.Encoding.UTF8.GetString(data));
    }

    public MifOutputByteStream stream() => os_;
}

// MifFOTBuilder - produces FrameMaker MIF output
public class MifFOTBuilder : FOTBuilder
{
    public static MifFOTBuilder? CurInstance;

    // Constructor
    public MifFOTBuilder(
        StringC fileLoc,
        Ptr<ExtendEntityManager> entityManager,
        CharsetInfo systemCharset,
        CmdLineApp app)
    {
        App = app;
        EntityManager = entityManager;
        SystemCharset = systemCharset;
        mifDoc = new MifDoc(fileLoc, app);
        paragraphBreakInEffect = false;
        inLeader = false;
        CurLeaderStream = null;
        CharTable = new Dictionary<Char, uint>();
        lastFlowObjectWasWhitespace = false;
        pendingBreak = Symbol.symbolFalse;
        firstHeaderFooter = true;
        inSimplePageSequence = false;
        bookComponentOpened = false;
        bookComponentAvailable = false;

        CurInstance = this;

        nextFormat = new Format();
        nextFormat.setDSSSLDefaults();
        nextFormat.FotCurDisplaySize = Format.INITIAL_PAGE_SIZE() - 2;
        // 2 is for margins (MIF doesn't accept zero-sized ones)
        formatStack.Insert(0, new Format(nextFormat));

        initMifBookComponent();
        bookComponentOpened = true;
        bookComponentAvailable = true;

        // Initialize character table mappings
        for (int i = 0; i < 128; i++)
        {
            Char c = FrameCharsetMap[i];
            if (c != 0)
            {
                if (!CharTable.ContainsKey((Char)(i + 0x80)))
                    CharTable[(Char)(i + 0x80)] = c;
                else if ((CharTable[(Char)(i + 0x80)] & ((1U << CHAR_TABLE_CHAR_BITS) - 1)) == c)
                    CharTable[(Char)(i + 0x80)] = CharTable[(Char)(i + 0x80)] | (1U << (i + CHAR_TABLE_CHAR_BITS));
            }
        }

        // Symbol font character code from James Clark
        for (int i = 0; i < nSymbolFonts; i++)
        {
            for (int j = 0; j < 256; j++)
            {
                Char c = SymbolFonts[i].mapping[j];
                if (c != 0 && !CharTable.ContainsKey(c))
                    CharTable[c] = (uint)(j | (i << CHAR_TABLE_CHAR_BITS) | CHAR_TABLE_SYMBOL_FLAG);
            }
        }
    }

    public static MifFOTBuilder curInstance()
    {
        System.Diagnostics.Debug.Assert(CurInstance != null);
        return CurInstance!;
    }

    // IndexEntryNIC struct
    public struct IndexEntryNIC
    {
        public StringC sortString;
        public System.Collections.Generic.List<StringC> components;
        public bool pageNumber;
        public bool startsPageRange;
        public bool endsPageRange;

        public IndexEntryNIC()
        {
            sortString = new StringC();
            components = new System.Collections.Generic.List<StringC>();
            pageNumber = true;
            startsPageRange = false;
            endsPageRange = false;
        }
    }

    // Extension flow object interface
    public abstract class MifExtensionFlowObj : ExtensionFlowObj
    {
        public abstract void atomic(MifFOTBuilder fb, NodePtr node);
    }

    // Index entry flow object
    public class IndexEntryFlowObj : MifExtensionFlowObj
    {
        public IndexEntryNIC nic = new IndexEntryNIC();

        public override void atomic(MifFOTBuilder fb, NodePtr node)
        {
            fb.indexEntry(nic);
        }

        public override bool hasNIC(StringC name)
        {
            // IndexEntryFlowObj NICs are handled through the nic field
            return false;
        }

        public override void setNIC(StringC name, IExtensionFlowObjValue value)
        {
            // IndexEntryFlowObj NICs are handled through the nic field
        }

        public override ExtensionFlowObj copy()
        {
            return new IndexEntryFlowObj { nic = this.nic };
        }
    }

    // DisplayInfo struct
    public class DisplayInfo
    {
        public DisplaySpace spaceBefore;
        public DisplaySpace spaceAfter;
        public Symbol keep;
        public Symbol breakBefore;
        public Symbol breakAfter;
        public bool keepWithPrevious;
        public bool keepWithNext;
        public bool mayViolateKeepBefore;
        public bool mayViolateKeepAfter;
        public bool firstParaOutputed;
        public bool isParagraph;
        public bool paragraphClosedInMif;
        public bool keepWithinPageInEffect;

        public DisplayInfo(DisplayNIC nic, DisplayInfo? parentDs)
        {
            spaceBefore = nic.spaceBefore;
            spaceAfter = nic.spaceAfter;
            keep = nic.keep;
            breakAfter = nic.breakAfter;
            breakBefore = nic.breakBefore;
            keepWithPrevious = nic.keepWithPrevious;
            keepWithNext = nic.keepWithNext;
            mayViolateKeepBefore = nic.mayViolateKeepBefore;
            isParagraph = false;
            mayViolateKeepAfter = nic.mayViolateKeepAfter;
            firstParaOutputed = false;
            paragraphClosedInMif = false;
            keepWithinPageInEffect = (nic.keep == Symbol.symbolPage)
                ? true
                : (parentDs != null ? parentDs.keepWithinPageInEffect : false);
        }
    }

    // DisplaySpaceInfo struct
    public class DisplaySpaceInfo
    {
        public DisplaySpace space;
        public Symbol breakType;
        public bool breakIsAfter;

        public DisplaySpaceInfo(DisplaySpace space_, Symbol breakType_, bool breakIsAfter_)
        {
            space = space_;
            breakType = breakType_;
            breakIsAfter = breakIsAfter_;
        }
    }

    // EffectiveDisplaySpace struct
    public struct EffectiveDisplaySpace
    {
        public long nominal;
        public long min;
        public long max;
        public long priority;
        public bool conditional;
        public bool force;

        public EffectiveDisplaySpace()
        {
            nominal = 0;
            min = 0;
            max = 0;
            priority = 0;
            conditional = true;
            force = false;
        }

        public void set(long nominal_ = 0, long min_ = 0, long max_ = 0, long priority_ = 0,
                       bool conditional_ = true, bool force_ = false)
        {
            nominal = nominal_;
            min = min_;
            max = max_;
            priority = priority_;
            conditional = conditional_;
            force = force_;
        }

        public void combine(EffectiveDisplaySpace eds)
        {
            if (eds.force)
            {
                if (force)
                {
                    nominal += eds.nominal;
                    min += eds.min;
                    max += eds.max;
                }
                else
                    this = eds;
            }
            else
            {
                if (eds.priority > priority)
                    this = eds;
                else if (eds.priority == priority)
                {
                    if (eds.nominal > nominal)
                        this = eds;
                    else if (eds.nominal == nominal)
                    {
                        if (eds.min < min) min = eds.min;
                        if (eds.max > max) max = eds.max;
                    }
                }
            }
        }

        public void clear() => set();
    }

    // TFotSimplePageSequence struct
    public struct TFotSimplePageSequence
    {
        public MifDoc.TextFlow? BodyTextFlow;
        public MifDoc.TextFlow? FirstHeaderTextFlow;
        public MifDoc.TextFlow? FirstFooterTextFlow;
        public MifDoc.TextFlow? LeftHeaderTextFlow;
        public MifDoc.TextFlow? LeftFooterTextFlow;
        public MifDoc.TextFlow? RightHeaderTextFlow;
        public MifDoc.TextFlow? RightFooterTextFlow;
        public MifDoc.ParagraphFormat paragraphFormat;
    }

    // Border struct
    public class Border
    {
        public long borderPriority;
        public long lineThickness;
        public bool borderPresent;
        public long lineRepeat;
        public long lineSep;
        public bool cellBorder;
        public string color = "";

        public Border(bool cellBorder_ = true)
        {
            cellBorder = cellBorder_;
            borderPresent = false;
        }

        public string makeMifRuling(MifDoc mifDoc)
        {
            string result = "";

            if (borderPresent)
            {
                MifDoc.Ruling mifRuling = new MifDoc.Ruling();

                mifRuling.setRulingPenWidth(new MifDoc.T_dimension(lineThickness));
                mifRuling.setRulingLines((int)(lineRepeat >= 2 ? 2 : lineRepeat));
                mifRuling.setRulingGap(new MifDoc.T_dimension(lineRepeat >= 2 ? lineSep - lineThickness : 0));
                mifRuling.setRulingColor(new MifDoc.T_tagstring(color));

                result = MifDoc.Ruling.key(mifRuling);
                mifRuling.setRulingTag(new MifDoc.T_tagstring(result));

                if (!mifDoc.rulingCatalog().Rulings.ContainsKey(result))
                {
                    mifDoc.rulingCatalog().Rulings[result] = mifRuling;
                }
            }

            return result;
        }

        public void resolve(Border adjacentBorder)
        {
            if (adjacentBorder.borderPriority > borderPriority
                || (adjacentBorder.borderPriority == borderPriority
                    && !adjacentBorder.cellBorder
                    && adjacentBorder.borderPresent))
            {
                lineThickness = adjacentBorder.lineThickness;
                borderPresent = adjacentBorder.borderPresent;
                lineRepeat = adjacentBorder.lineRepeat;
                lineSep = adjacentBorder.lineSep;
                color = adjacentBorder.color;

                if (adjacentBorder.cellBorder)
                    adjacentBorder.borderPresent = false;
            }
        }

        public void setFromFot()
        {
            Format f = MifFOTBuilder.curInstance().format();
            borderPriority = f.FotBorderPriority;
            borderPresent = f.FotBorderPresent;
            lineThickness = f.FotLineThickness;
            lineRepeat = f.FotLineRepeat;
            lineSep = f.FotLineSep;
            color = f.FColor;
        }
    }

    // Column class (changed from struct to allow list element modification)
    public class Column
    {
        public bool hasWidth;
        public TableLengthSpec width;
    }

    // Cell struct
    public class Cell
    {
        public bool missing;
        public uint nColumnsSpanned;
        public uint nRowsSpanned;
        public Border beforeRowBorder;
        public Border afterRowBorder;
        public Border beforeColumnBorder;
        public Border afterColumnBorder;
        public long displaySize;
        public Cell? OverlappingCell;
        protected MifDoc.Cell? MifCell_;

        public Cell()
        {
            missing = false;
            MifCell_ = new MifDoc.Cell();
            OverlappingCell = null;
            nRowsSpanned = 1;
            nColumnsSpanned = 1;
            displaySize = 0;
            beforeRowBorder = new Border();
            afterRowBorder = new Border();
            beforeColumnBorder = new Border();
            afterColumnBorder = new Border();
        }

        public MifDoc.Cell mifCell()
        {
            System.Diagnostics.Debug.Assert(MifCell_ != null);
            return MifCell_!;
        }

        public void translate(MifDoc.Cell mifCell, MifDoc mifDoc)
        {
            MifDoc.T_tagstring rulingTag;

            rulingTag = new MifDoc.T_tagstring(beforeRowBorder.makeMifRuling(mifDoc));
            if (rulingTag.size() > 0)
                mifCell.setCellTRuling(rulingTag);

            rulingTag = new MifDoc.T_tagstring(afterRowBorder.makeMifRuling(mifDoc));
            if (rulingTag.size() > 0)
                mifCell.setCellBRuling(rulingTag);

            rulingTag = new MifDoc.T_tagstring(beforeColumnBorder.makeMifRuling(mifDoc));
            if (rulingTag.size() > 0)
                mifCell.setCellLRuling(rulingTag);

            rulingTag = new MifDoc.T_tagstring(afterColumnBorder.makeMifRuling(mifDoc));
            if (rulingTag.size() > 0)
                mifCell.setCellRRuling(rulingTag);
        }
    }

    // Row struct
    public class Row
    {
        public System.Collections.Generic.List<Cell> Cells = new();

        public void translate(System.Collections.Generic.List<MifDoc.Row> mifRows, MifDoc mifDoc)
        {
            mifRows.Add(new MifDoc.Row());
            MifDoc.Row mifRow = mifRows[mifRows.Count - 1];
            mifRow.Cells = new System.Collections.Generic.List<MifDoc.Cell>();
            for (int i = 0; i < Cells.Count; i++)
                mifRow.Cells.Add(new MifDoc.Cell());

            for (int i = 0; i + 1 < Cells.Count; i++)
            {
                Cells[i].translate(Cells[i].mifCell(), mifDoc);
                mifRow.Cells[i] = Cells[i].mifCell();
            }
        }
    }

    // TablePart struct
    public class TablePart
    {
        public System.Collections.Generic.List<Column> Columns = new();
        public System.Collections.Generic.List<Row> Header = new();
        public System.Collections.Generic.List<Row> Body = new();
        public System.Collections.Generic.List<Row> Footer = new();
        public nuint MifTableNum;
        public Table? ParentTable;
        public bool columnsProcessed;
        public bool needsColumnReprocessing;

        public TablePart()
        {
            MifTableNum = 0;
            ParentTable = null;
            columnsProcessed = false;
            needsColumnReprocessing = false;
        }

        public void translate(MifDoc mifDoc)
        {
            if (needsColumnReprocessing)
                processColumns();

            MifDoc.Tbl mifTbl = mifTable(mifDoc);
            if (parentTable().startIndent != 0)  // DSSSL default
            {
                mifTbl.tblFormat.setTblLIndent(new MifDoc.T_dimension(parentTable().startIndent));
                mifTbl.setProperties |= (uint)MifDoc.Tbl.TblFlags.fTblFormat;
            }

            if (parentTable().displayAlignment != Symbol.symbolStart)
            {
                mifTbl.setProperties |= (uint)MifDoc.Tbl.TblFlags.fTblFormat;
                MifDoc.T_keyword mifAlignment = new MifDoc.T_keyword(MifDoc.sLeft);
                switch (parentTable().displayAlignment)
                {
                    case Symbol.symbolStart: mifAlignment = new MifDoc.T_keyword(MifDoc.sLeft); break;
                    case Symbol.symbolEnd: mifAlignment = new MifDoc.T_keyword(MifDoc.sRight); break;
                    case Symbol.symbolCenter: mifAlignment = new MifDoc.T_keyword(MifDoc.sCenter); break;
                    case Symbol.symbolInside: mifAlignment = new MifDoc.T_keyword(MifDoc.sInside); break;
                    case Symbol.symbolOutside: mifAlignment = new MifDoc.T_keyword(MifDoc.sOutside); break;
                    default: System.Diagnostics.Debug.Assert(false); break;
                }
                mifTbl.tblFormat.setTblAlignment(mifAlignment);
            }

            bool putHeaderInBody = Body.Count == 0 && Header.Count > 0;
            bool putFooterInBody = !putHeaderInBody && Body.Count == 0 && Footer.Count > 0;
            for (int i = 0; i < Header.Count; i++)
                Header[i].translate(putHeaderInBody ? mifTbl.TblBody : mifTbl.TblH, mifDoc);
            for (int i = 0; i < Body.Count; i++)
                Body[i].translate(mifTbl.TblBody, mifDoc);
            for (int i = 0; i < Footer.Count; i++)
                Footer[i].translate(putFooterInBody ? mifTbl.TblBody : mifTbl.TblF, mifDoc);
        }

        public void processColumns()
        {
            MifDoc.Tbl mifTbl = mifTable(MifDoc.CurInstance!);
            mifTbl.setTblNumColumns(0);
            mifTbl.setTblNumColumns(Columns.Count);
            mifTbl.TblColumnWidths.Clear();
            for (int i = 0; i < Columns.Count; i++)
                mifTbl.TblColumnWidths.Add(0);
            mifTbl.TblColumnWidthsAreSet();

            long totalNonproportionalWidth = 0L;
            double totalProportionalUnits = 0.0;
            for (int i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].hasWidth)
                {
                    if (Columns[i].width.tableUnitFactor != 0.0)
                    {
                        totalProportionalUnits += Columns[i].width.tableUnitFactor;
                    }
                    else
                    {
                        mifTbl.TblColumnWidths[i]
                            = MifFOTBuilder.curInstance().computeLengthSpec(Columns[i].width);
                        totalNonproportionalWidth += mifTbl.TblColumnWidths[i];
                    }
                }
            }

            double proportionalUnit = 0.0;
            if (totalProportionalUnits != 0.0)
                proportionalUnit
                    = (parentTable().tableWidth - totalNonproportionalWidth) / totalProportionalUnits;

            for (int i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].hasWidth)
                {
                    if (Columns[i].width.tableUnitFactor != 0.0)
                        mifTbl.TblColumnWidths[i]
                            = (long)(proportionalUnit * Columns[i].width.tableUnitFactor);
                }
                else
                    mifTbl.TblColumnWidths[i] = (long)proportionalUnit;
            }

            columnsProcessed = true;
        }

        public void normalizeRows()
        {
            int maxCellsInRow = Columns.Count + 1;
            System.Collections.Generic.List<Row>? rows;
            for (int step = 0; step < 2; step++)
            {
                for (int rowType = 0; rowType < 3; rowType++)
                {
                    switch (rowType)
                    {
                        case 0: rows = Header; break;
                        case 1: rows = Body; break;
                        default: rows = Footer; break;
                    }
                    for (int r = 0; r < rows.Count; r++)
                    {
                        if (step == 0)
                        {
                            if (rows[r].Cells.Count > 1)
                            {
                                int lastCellIdx = rows[r].Cells.Count - 2;
                                Cell lastCell = rows[r].Cells[lastCellIdx];
                                if (!lastCell.missing
                                    && lastCellIdx + (int)lastCell.nColumnsSpanned + 1 > maxCellsInRow)
                                    maxCellsInRow = lastCellIdx + (int)lastCell.nColumnsSpanned + 1;
                            }
                        }
                        else if (rows[r].Cells.Count < maxCellsInRow)
                        {
                            while (rows[r].Cells.Count < maxCellsInRow)
                                rows[r].Cells.Add(new Cell());
                        }
                    }
                }
            }
        }

        public void begin(Table parent, MifDoc mifDoc)
        {
            Columns.Clear();
            Header.Clear();
            Body.Clear();
            Footer.Clear();

            columnsProcessed = false;
            needsColumnReprocessing = false;

            ParentTable = parent;
            parentTable().CurRows = Body;
            parentTable().CurTablePart = this;

            if (MifTableNum == 0)
            {
                mifDoc.tbls().Add(new MifDoc.Tbl());
                MifTableNum = (nuint)mifDoc.tbls().Count;
                MifDoc.CurInstance?.setCurTblNum(MifTableNum);
            }
        }

        public MifDoc.Tbl mifTable(MifDoc mifDoc)
        {
            System.Diagnostics.Debug.Assert(MifTableNum > 0);
            return mifDoc.tbls()[(int)MifTableNum - 1];
        }

        public Table parentTable()
        {
            System.Diagnostics.Debug.Assert(ParentTable != null);
            return ParentTable!;
        }
    }

    // Table struct
    public class Table
    {
        public System.Collections.Generic.List<TablePart> TableParts = new();
        public Border beforeRowBorder;
        public Border afterRowBorder;
        public Border beforeColumnBorder;
        public Border afterColumnBorder;
        public long tableWidth;
        public Symbol displayAlignment;
        public long startIndent;
        public Cell? CurCell;
        public TablePart? CurTablePart;
        public System.Collections.Generic.List<Row>? CurRows;
        public bool DefaultTblFormatGenerated;
        public bool NoTablePartsSeen;

        public Table()
        {
            beforeRowBorder = new Border(false);
            afterRowBorder = new Border(false);
            beforeColumnBorder = new Border(false);
            afterColumnBorder = new Border(false);
            CurCell = null;
            CurTablePart = null;
            DefaultTblFormatGenerated = false;
            NoTablePartsSeen = true;
        }

        public void resolveBorders(System.Collections.Generic.List<Row> rows, bool hasFirstTableRow, bool hasLastTableRow)
        {
            bool isFirstRow;
            bool isLastRow;
            bool isFirstColumn;
            bool isLastColumn;
            Cell? cell = null;
            int r, c, rr, cc;
            bool leftEdge, topEdge;

            for (r = 0; r < rows.Count; r++)
            {
                for (c = 0; c < rows[r].Cells.Count - 1; c++)
                {
                    cell = rows[r].Cells[c];
                    if (cell.OverlappingCell == null)
                    {
                        for (rr = r; rr < r + (int)cell.nRowsSpanned; rr++)
                        {
                            for (cc = c, leftEdge = true; cc < c + (int)cell.nColumnsSpanned; cc++)
                            {
                                rows[rr].Cells[cc].OverlappingCell = cell;
                            }
                        }
                    }
                }
            }

            for (r = 0; r < rows.Count; r++)
            {
                for (c = 0; c < rows[r].Cells.Count - 1; c++)
                {
                    cell = rows[r].Cells[c];
                    if (cell.OverlappingCell == cell)
                    {
                        for (rr = r, topEdge = true; rr < r + (int)cell.nRowsSpanned; rr++)
                        {
                            isFirstRow = (rr == 0);
                            isLastRow = (rr == rows.Count - 1);
                            for (cc = c, leftEdge = true; cc < c + (int)cell.nColumnsSpanned; cc++)
                            {
                                isFirstColumn = (cc == 0);
                                isLastColumn = (cc == rows[rr].Cells.Count - 2);

                                if (leftEdge)
                                    if (isFirstColumn)
                                    {
                                        cell.beforeColumnBorder.resolve(beforeColumnBorder);
                                    }
                                    else
                                        cell.beforeColumnBorder.resolve(
                                            rows[rr].Cells[cc - 1].OverlappingCell!.afterColumnBorder);

                                if (topEdge)
                                    if (isFirstRow && hasFirstTableRow)
                                        cell.beforeRowBorder.resolve(beforeRowBorder);
                                    else if (!isFirstRow)
                                        cell.beforeRowBorder.resolve(
                                            rows[rr - 1].Cells[cc].OverlappingCell!.afterRowBorder);

                                if (isLastColumn)
                                    cell.afterColumnBorder.resolve(afterColumnBorder);

                                if (isLastRow && hasLastTableRow)
                                    cell.afterRowBorder.resolve(afterRowBorder);

                                leftEdge = false;
                            }
                            topEdge = false;
                        }
                    }
                }
            }
        }

        public void begin(MifDoc mifDoc)
        {
            CurCell = null;
            NoTablePartsSeen = true;

            TableParts.Clear();
            TableParts.Add(new TablePart());
            TableParts[TableParts.Count - 1].begin(this, mifDoc);
        }
        public System.Collections.Generic.List<Row> curRows() { System.Diagnostics.Debug.Assert(CurRows != null); return CurRows!; }
        public TablePart curTablePart() { System.Diagnostics.Debug.Assert(CurTablePart != null); return CurTablePart!; }
        public Cell curCell() { System.Diagnostics.Debug.Assert(CurCell != null); return CurCell!; }
    }

    // Format class - extends MifDoc.ParagraphFormat with FOT-specific fields
    public class Format : MifDoc.ParagraphFormat
    {
        public LengthSpec FotFirstLineStartIndentSpec;
        public LengthSpec FotStartIndentSpec;
        public LengthSpec FotEndIndentSpec;
        public LengthSpec FotLineSpacingSpec;
        public LengthSpec FotPositionPointShiftSpec;
        public LengthSpec FotFieldWidth;
        public OptLengthSpec FotMinLeading;
        public long FotCurDisplaySize;
        public long FotLineThickness;
        public Symbol FotLineCap;
        public long FotBorderPriority;
        public bool FotBorderPresent;
        public long FotLineRepeat;
        public long FotLineSep;
        public Symbol FotDisplayAlignment;
        public long FotLeftMargin;
        public long FotRightMargin;
        public long FotTopMargin;
        public long FotBottomMargin;
        public long FotHeaderMargin;
        public long FotFooterMargin;
        public long FotPageWidth;
        public long FotPageHeight;
        public Symbol FotFieldAlign;
        public Symbol FotInputWhitespaceTreatment;
        public Symbol FotLines;
        public long FotPageNColumns;
        public long FotPageColumnSep;
        public long FotSpan;
        public bool FotPageBalanceColumns;
        public bool FotCellBackground;
        public string MifBackgroundColor = "";
        public Letter2 FotLanguage;
        public Letter2 FotCountry;

        public Format() : base()
        {
            FotLineSpacingSpec = new LengthSpec { length = 12000 };
            FotFirstLineStartIndentSpec = new LengthSpec();
            FotStartIndentSpec = new LengthSpec();
            FotEndIndentSpec = new LengthSpec();
            FotLanguage = new Letter2(0);
            FotCurDisplaySize = 0;
            FotLineThickness = 1000;
            FotBorderPresent = true;
            FotLineRepeat = 1;
            FotLineSep = 1000;
            FotBorderPriority = 0;
            FotDisplayAlignment = Symbol.symbolStart;
            FotLeftMargin = 1;
            FotRightMargin = 1;
            FotTopMargin = 0;
            FotBottomMargin = 0;
            FotHeaderMargin = 0;
            FotFooterMargin = 0;
            FotPageWidth = 72000 * 8;
            FotPageHeight = (72000 * 23) / 2;
            FotLineCap = Symbol.symbolButt;
            FotPositionPointShiftSpec = new LengthSpec();
            FotMinLeading = new OptLengthSpec();
            FotFieldAlign = Symbol.symbolStart;
            FotFieldWidth = new LengthSpec();
            FotLines = Symbol.symbolWrap;
            FotInputWhitespaceTreatment = Symbol.symbolPreserve;
            FotPageNColumns = 1;
            FotPageColumnSep = 72000 / 2;
            FotSpan = 1;
            FotPageBalanceColumns = false;
            FotCellBackground = false;
        }

        public Format(Format other) : base()
        {
            // Copy all fields
            FotLineSpacingSpec = other.FotLineSpacingSpec;
            FotFirstLineStartIndentSpec = other.FotFirstLineStartIndentSpec;
            FotStartIndentSpec = other.FotStartIndentSpec;
            FotEndIndentSpec = other.FotEndIndentSpec;
            FotLanguage = other.FotLanguage;
            FotCurDisplaySize = other.FotCurDisplaySize;
            FotLineThickness = other.FotLineThickness;
            FotBorderPresent = other.FotBorderPresent;
            FotLineRepeat = other.FotLineRepeat;
            FotLineSep = other.FotLineSep;
            FotBorderPriority = other.FotBorderPriority;
            FotDisplayAlignment = other.FotDisplayAlignment;
            FotLeftMargin = other.FotLeftMargin;
            FotRightMargin = other.FotRightMargin;
            FotTopMargin = other.FotTopMargin;
            FotBottomMargin = other.FotBottomMargin;
            FotHeaderMargin = other.FotHeaderMargin;
            FotFooterMargin = other.FotFooterMargin;
            FotPageWidth = other.FotPageWidth;
            FotPageHeight = other.FotPageHeight;
            FotLineCap = other.FotLineCap;
            FotPositionPointShiftSpec = other.FotPositionPointShiftSpec;
            FotMinLeading = other.FotMinLeading;
            FotFieldAlign = other.FotFieldAlign;
            FotFieldWidth = other.FotFieldWidth;
            FotLines = other.FotLines;
            FotInputWhitespaceTreatment = other.FotInputWhitespaceTreatment;
            FotPageNColumns = other.FotPageNColumns;
            FotPageColumnSep = other.FotPageColumnSep;
            FotSpan = other.FotSpan;
            FotPageBalanceColumns = other.FotPageBalanceColumns;
            FotCellBackground = other.FotCellBackground;
            MifBackgroundColor = other.MifBackgroundColor;
            FotCountry = other.FotCountry;
        }

        public static long INITIAL_PAGE_SIZE() => 72000 * 8;

        public void computePgfLanguage()
        {
            switch (FotLanguage.value)
            {
                case 0x454E: // 'EN'
                    PgfLanguage = (FotCountry.value == 0x4742) ? MifDoc.sUKEnglish : MifDoc.sUSEnglish;
                    break;
                case 0x4445: // 'DE'
                    PgfLanguage = (FotCountry.value == 0x4348) ? MifDoc.sSwissGerman : MifDoc.sGerman;
                    break;
                case 0x4652: // 'FR'
                    PgfLanguage = (FotCountry.value == 0x4341) ? MifDoc.sCanadianFrench : MifDoc.sFrench;
                    break;
                case 0x4553: // 'ES'
                    PgfLanguage = MifDoc.sSpanish;
                    break;
                case 0x4341: // 'CA'
                    PgfLanguage = MifDoc.sCatalan;
                    break;
                case 0x4954: // 'IT'
                    PgfLanguage = MifDoc.sItalian;
                    break;
                case 0x5054: // 'PT'
                    PgfLanguage = MifDoc.sPortuguese;
                    break;
                case 0x4E4C: // 'NL'
                    PgfLanguage = MifDoc.sDutch;
                    break;
                case 0x4E4F: // 'NO'
                    PgfLanguage = MifDoc.sNorwegian;
                    break;
                case 0x4649: // 'FI'
                    PgfLanguage = MifDoc.sFinnish;
                    break;
                case 0x5356: // 'SV'
                    PgfLanguage = MifDoc.sSwedish;
                    break;
                default:
                    PgfLanguage = MifDoc.sNoLanguage;
                    break;
            }
        }
    }

    // NodeInfo struct
    public struct NodeInfo
    {
        public NodePtr node;
        public uint nodeLevel;
        public static uint nonEmptyElementsOpened;
        public static uint curNodeLevel;

        public NodeInfo(NodePtr node_, uint nodeLevel_)
        {
            node = node_;
            nodeLevel = nodeLevel_;
        }
    }

    // LinkInfo struct
    public class LinkInfo
    {
        public MifDoc.CrossRefInfo? crossRefInfo;
        public bool openedInMif;
        public static uint pendingMifClosings;

        public LinkInfo(MifDoc.CrossRefInfo? crossRefInfo_ = null)
        {
            crossRefInfo = crossRefInfo_;
            openedInMif = false;
        }

        ~LinkInfo()
        {
            crossRefInfo = null;
        }

        public bool forcesNoLink() => crossRefInfo == null;
    }

    // SymbolFont struct
    public struct SymbolFont
    {
        public string name;
        public Char[] mapping;
    }

    public const int nSymbolFonts = 3;
    public const int CHAR_TABLE_CHAR_BITS = 16;
    public const uint CHAR_TABLE_SYMBOL_FLAG = 1U << 31;

    // Main FOTBuilder methods
    public override void characters(Char[] data, nuint n)
    {
        checkForParagraphReopening();
        if (MifDoc.Para.currentlyOpened)
        {
            if (inLeader)
            {
                outString(data, n, CurLeaderStream, false);
            }
            else
            {
                synchronizeFontFormat();
                outString(data, n, null, true);
            }
        }
    }

    public override void extension(ExtensionFlowObj fo, NodePtr node)
    {
        if (fo is MifExtensionFlowObj mifFo)
            mifFo.atomic(this, node);
    }

    public override void start()
    {
        NodeInfo.nonEmptyElementsOpened = (uint)nodeStack.Count;

        var effectiveFormat = new Format(nextFormat);
        if (nextFormat.FSize > 0)
            effectiveFormat.setFDY(
                (double)(computeLengthSpec(nextFormat.FotPositionPointShiftSpec) * -100)
                / nextFormat.FSize);

        formatStack.Insert(0, effectiveFormat);
    }

    public override void end()
    {
        System.Diagnostics.Debug.Assert(formatStack.Count > 0 && formatStack[0] != null);
        formatStack.RemoveAt(0);

        System.Diagnostics.Debug.Assert(formatStack.Count > 0 && formatStack[0] != null);
        nextFormat = new Format(formatStack[0]);
    }

    public void indexEntry(IndexEntryNIC nic)
    {
        StringBuilder mText = new StringBuilder();
        if (nic.components.Count > 0)
        {
            if (!nic.pageNumber)
                mText.Append("<$nopage>");
            if (nic.startsPageRange)
                mText.Append("<$startrange>");
            if (nic.endsPageRange)
                mText.Append("<$endrange>");
            bool first = true;
            for (int i = 0; i < nic.components.Count; first = false, i++)
            {
                if (!first) mText.Append(':');
                for (nuint ii = 0; ii < nic.components[i].size(); ii++)
                    mText.Append((char)nic.components[i][ii]);
            }
            if (nic.sortString.size() > 0)
            {
                mText.Append('[');
                for (nuint i = 0; i < nic.sortString.size(); i++)
                    mText.Append((char)nic.sortString[i]);
                mText.Append(']');
            }
            indexEntryStack.Add(new MifDoc.Marker(new MifDoc.T_string(mText.ToString()), MifDoc.Marker.MarkerType.Index));
        }
    }

    public override void startSimplePageSequenceSerial()
    {
        inSimplePageSequence = true;
        firstHeaderFooter = true;

        bool openBookComponent = true;
        if (bookComponentOpened)
        {
            if (bookComponentAvailable)
            {
                openBookComponent = false;
                bookComponentAvailable = false;
            }
            else
                mifDoc.exitBookComponent();
        }

        if (openBookComponent)
        {
            mifDoc.enterBookComponent();
            initMifBookComponent();
            bookComponentOpened = true;
            bookComponentAvailable = false;
        }

        // Desc: Pagesize was being initialized but not set after
        // the attribute was read.

        mifDoc.document().setDPageSize(
            new MifDoc.T_WH(new MifDoc.T_dimension(nextFormat.FotPageWidth),
                           new MifDoc.T_dimension(nextFormat.FotPageHeight)));

        nextFormat.FotCurDisplaySize
            = (nextFormat.FotPageWidth - nextFormat.FotLeftMargin - nextFormat.FotRightMargin
               - nextFormat.FotPageColumnSep * (nextFormat.FotPageNColumns - 1))
               / nextFormat.FotPageNColumns;

        mifDoc.document().setDColumns((int)nextFormat.FotPageNColumns);
        if (nextFormat.FotPageNColumns > 1)
            mifDoc.document().setDColumnGap(new MifDoc.T_dimension(nextFormat.FotPageColumnSep));

        // fDMargins not implemented yet
        // mifDoc.document().setProperties &= ~MifDoc.Document.fDMargins;

        start();

        FotSimplePageSequence.paragraphFormat = format();
    }

    public override void endSimplePageSequenceSerial()
    {
        end();
        mifDoc.exitTextFlow();
        mifDoc.exitBookComponent();
        inSimplePageSequence = false;
        bookComponentOpened = false;
        bookComponentAvailable = false;
    }

    public override void startSimplePageSequenceHeaderFooter(uint hfPart)
    {
        if (firstHeaderFooter) { setupSimplePageSequence(); firstHeaderFooter = false; }

        MifDoc.TextFlow? curTextFlow;

        const uint firstHF = (uint)HF.firstHF;
        const uint frontHF = (uint)HF.frontHF;
        const uint headerHF = (uint)HF.headerHF;
        const uint centerHF = (uint)HF.centerHF;
        const uint rightHF = (uint)HF.rightHF;

        if ((hfPart & firstHF) != 0)
            if ((hfPart & frontHF) != 0)
                if ((hfPart & headerHF) != 0)
                    curTextFlow = FotSimplePageSequence.FirstHeaderTextFlow;
                else
                    curTextFlow = FotSimplePageSequence.FirstFooterTextFlow;
            else
                return;
        else
            if ((hfPart & frontHF) != 0)
                if ((hfPart & headerHF) != 0)
                    curTextFlow = FotSimplePageSequence.RightHeaderTextFlow;
                else
                    curTextFlow = FotSimplePageSequence.RightFooterTextFlow;
            else
                if ((hfPart & headerHF) != 0)
                    curTextFlow = FotSimplePageSequence.LeftHeaderTextFlow;
                else
                    curTextFlow = FotSimplePageSequence.LeftFooterTextFlow;

        mifDoc.enterTextFlow(curTextFlow!);

        if ((hfPart & (centerHF | rightHF)) != 0)
            mifDoc.outSpecialChar(MifDoc.sTab);
        else // leftHF
            if ((hfPart & headerHF) != 0) beginHeader(); else beginFooter();
    }

    public override void endSimplePageSequenceHeaderFooter(uint hfPart)
    {
        const uint firstHF = (uint)HF.firstHF;
        const uint frontHF = (uint)HF.frontHF;
        const uint rightHF = (uint)HF.rightHF;

        if ((hfPart & rightHF) != 0 && ((hfPart & frontHF) != 0 || (hfPart & firstHF) == 0))
        {
            endHeaderFooter(hfPart);
        }

        if ((hfPart & firstHF) == 0 || (hfPart & frontHF) != 0)
            mifDoc.exitTextFlow();
    }

    public override void endAllSimplePageSequenceHeaderFooter()
    {
        // mifDoc.enterTextFlow( *FotSimplePageSequence.BodyTextFlow );
    }

    public void setPageNColumns(long n)
    {
        nextFormat.FotPageNColumns = n;
    }

    public void setPageColumnSep(long l)
    {
        nextFormat.FotPageColumnSep = l;
    }

    public void setPageBalanceColumns(bool b)
    {
        nextFormat.FotPageBalanceColumns = b;
    }

    public override void startNode(NodePtr node, StringC modeName)
    {
        NodeInfo.curNodeLevel++;
        if (modeName.size() == 0)
            nodeStack.Add(new NodeInfo(node, NodeInfo.curNodeLevel));
    }

    public override void endNode()
    {
        if (nodeStack.Count > 0 && nodeStack[nodeStack.Count - 1].nodeLevel == NodeInfo.curNodeLevel
            && NodeInfo.nonEmptyElementsOpened < nodeStack.Count)
            nodeStack.RemoveAt(nodeStack.Count - 1);
        NodeInfo.curNodeLevel--;
    }

    public override void currentNodePageNumber(NodePtr node)
    {
        ulong n = 0;
        if (node.elementIndex(ref n) == AccessResult.accessOK)
        {
            GroveString id = new GroveString();
            node.getId(ref id);
            if (!mifDoc.bookComponent().pageNumXRefFormatGenerated)
            {
                mifDoc.bookComponent().XRefFormats.Add(
                    new MifDoc.XRefFormat(MifDoc.sPageNumXRefFormatName, @"<$pagenum\>"));
                mifDoc.bookComponent().pageNumXRefFormatGenerated = true;
            }
            ulong groveIndex = node.groveIndex();
            _ = mifDoc.os() << new MifDoc.CrossRefInfo(
                    groveIndex, n, mifDoc.os().CurTagIndent,
                    MifDoc.CrossRefInfo.InfoType.XRef, id.data(), id.size());
            if (id.size() > 0)
                mifDoc.elements().setReferencedFlag(
                    MifDoc.ElementSet.ReferenceType.PageReference, groveIndex, new StringC(id.data(), id.size()));
            else
                mifDoc.elements().setReferencedFlag(
                    MifDoc.ElementSet.ReferenceType.PageReference, groveIndex, n);
        }
    }

    public override void startLink(Address address)
    {
        switch (address.type)
        {
            case Address.Type.resolvedNode:
                {
                    ulong n = 0;
                    if (address.node.elementIndex(ref n) == AccessResult.accessOK)
                    {
                        GroveString id = new GroveString();
                        address.node.getId(ref id);
                        ulong groveIndex = address.node.groveIndex();
                        linkStack.Add(new LinkInfo(
                            new MifDoc.CrossRefInfo(groveIndex, n, 0,
                                MifDoc.CrossRefInfo.InfoType.HypertextLink,
                                id.data(), id.size())));
                        if (id.size() > 0)
                            mifDoc.elements().setReferencedFlag(
                                MifDoc.ElementSet.ReferenceType.LinkReference, groveIndex,
                                new StringC(id.data(), id.size()));
                        else
                            mifDoc.elements().setReferencedFlag(
                                MifDoc.ElementSet.ReferenceType.LinkReference, groveIndex, n);
                    }
                    break;
                }

            case Address.Type.idref:
                {
                    StringC id = address.@params[0];
                    nuint i;
                    for (i = 0; i < id.size(); i++)
                        if (id[i] == ' ')
                            break;
                    linkStack.Add(new LinkInfo(
                        new MifDoc.CrossRefInfo(address.node.groveIndex(), 0, 0,
                            MifDoc.CrossRefInfo.InfoType.HypertextLink,
                            id.data(), i)));
                    mifDoc.elements().setReferencedFlag(
                        MifDoc.ElementSet.ReferenceType.LinkReference, address.node.groveIndex(),
                        new StringC(id.data(), i));
                    break;
                }

            case Address.Type.none:
            default:
                linkStack.Add(new LinkInfo());
                break;
        }
    }

    public override void endLink()
    {
        System.Diagnostics.Debug.Assert(linkStack.Count > 0);
        if (linkStack[linkStack.Count - 1].openedInMif)
            LinkInfo.pendingMifClosings++;
        linkStack.RemoveAt(linkStack.Count - 1);

        // MifDoc::Marker marker( MifDoc::T_string( "" ), MifDoc::Marker::Hypertext );
        // marker.out( mifDoc.os() );
    }

    public override void startLineField(LineFieldNIC nic)
    {
        checkForParagraphReopening();
        lastFlowObjectWasWhitespace = false;

        long fieldWidth = computeLengthSpec(nextFormat.FotFieldWidth);
        long firstLineIndent = mifDoc.curPara()?.curFormat().PgfFIndent ?? 0;
        bool leadingTab = true;
        switch (nextFormat.FotFieldAlign)
        {
            case Symbol.symbolStart:
            default:
                mifDoc.curPara()?.curFormat().TabStops.Add(
                    new MifDoc.TabStop(MifDoc.sLeft, firstLineIndent + fieldWidth));
                leadingTab = false;
                break;
            case Symbol.symbolEnd:
                mifDoc.curPara()?.curFormat().TabStops.Add(
                    new MifDoc.TabStop(MifDoc.sRight, firstLineIndent + fieldWidth));
                mifDoc.curPara()?.curFormat().TabStops.Add(
                    new MifDoc.TabStop(MifDoc.sLeft, firstLineIndent + fieldWidth + 1));
                break;
            case Symbol.symbolCenter:
                mifDoc.curPara()?.curFormat().TabStops.Add(
                    new MifDoc.TabStop(MifDoc.sCenter, firstLineIndent + fieldWidth / 2));
                mifDoc.curPara()?.curFormat().TabStops.Add(
                    new MifDoc.TabStop(MifDoc.sLeft, firstLineIndent + fieldWidth + 1));
                break;
        }
        if (leadingTab)
            mifDoc.outSpecialChar(MifDoc.sTab);
    }

    public override void endLineField()
    {
        mifDoc.outSpecialChar(MifDoc.sTab);
    }

    public override void startParagraph(ParagraphNIC nic)
    {
        doStartParagraph(nic);
    }

    public override void endParagraph()
    {
        doEndParagraph();
    }

    public void doStartParagraph(DisplayNIC nic, bool servesAsWrapper = false,
                                  long height = 0, bool allowNegativeLeading = false)
    {
        startDisplay(nic);

        DisplayInfo? curDs = displayStack.First?.Value;
        System.Diagnostics.Debug.Assert(curDs != null);
        curDs!.isParagraph = true;

        nextFormat.setPgfWithPrev(curDs.mayViolateKeepBefore
                                    ? curDs.keepWithPrevious
                                    : (curDs.firstParaOutputed
                                         ? curDs.keepWithinPageInEffect
                                         : false));
        nextFormat.setPgfWithNext(curDs.keepWithNext);
        curDs.firstParaOutputed = true;

        processDisplaySpaceStack();
        switch (pendingBreak)
        {
            case Symbol.symbolPage:
                nextFormat.setPgfPlacement(MifDoc.sPageTop);
                break;
            case Symbol.symbolColumn:
                nextFormat.setPgfPlacement(MifDoc.sColumnTop);
                break;
            default:
                nextFormat.setPgfPlacement(MifDoc.sAnywhere);
                break;
        }
        pendingBreak = Symbol.symbolFalse;

        long lineSpacing;
        long extraSpaceBefore = 0;
        if (servesAsWrapper)
        {
            if (height < 2000)   // FrameMaker minimum is 2pt
            {
                if (allowNegativeLeading)
                    nextFormat.setPgfLeading(height - 2000);
                else
                    pendingEffectiveDisplaySpace.nominal
                      -= (pendingEffectiveDisplaySpace.nominal > 2000)
                            ? 2000 : pendingEffectiveDisplaySpace.nominal;
                     // try to steal as much as possible from space before
                height = 2000;
            }
            //if( effectiveDisplaySpace.nominal <= 0 )
             //    extraSpaceBefore = 1; // FrameMaker bug workaround
            //nextFormat.setFSize( height );
            lineSpacing = height;
            nextFormat.setFColor(MifDoc.sWhite);
            nextFormat.setPgfLineSpacing(MifDoc.sFixed);
        }
        else
        {
            //nextFormat.setFSize( computeLengthSpec( nextFormat.FotLineSpacingSpec.length ) );
            lineSpacing = computeLengthSpec(nextFormat.FotLineSpacingSpec);
            if (nextFormat.FotMinLeading.hasLength) // but ignore the actual min-leading value
                nextFormat.PgfLineSpacing = MifDoc.sProportional;
            else
                nextFormat.PgfLineSpacing = MifDoc.sFixed;
        }

        nextFormat.setPgfSpBefore(pendingEffectiveDisplaySpace.nominal + extraSpaceBefore);
        pendingEffectiveDisplaySpace.clear();

        nextFormat.setPgfLIndent(computeLengthSpec(nextFormat.FotStartIndentSpec));
        nextFormat.setPgfFIndent(computeLengthSpec(nextFormat.FotFirstLineStartIndentSpec)
                                   + nextFormat.PgfLIndent);
        nextFormat.setPgfRIndent(computeLengthSpec(nextFormat.FotEndIndentSpec));

        nextFormat.setPgfPlacementStyle(
            nextFormat.FotSpan > 1 ? MifDoc.sStraddle : MifDoc.sNormal);
        start();

        MifDoc.Para p = new MifDoc.Para(mifDoc.curTagStream().content().stream().CurTagIndent + 4);
        p.setParagraphFormat(mifDoc.curFormat());
        p.format().updateFrom(format());
        p.format().FSize = mifDoc.curFormat().FSize;
        p.format().ffSetProperties() &= ~(uint)MifDoc.FontFormat.Flags.fFSize;
        if (p.format().FSize != lineSpacing)
            p.format().setFSize(lineSpacing);
        p.curFormat().updateFrom(p.format());
        // assert( mifDoc.curPara( false ) == NULL );
        mifDoc.setCurPara(p);
        mifDoc.enterPara(mifDoc.curPara()!);
        lastFlowObjectWasWhitespace = false;
        outPendingInlineStatements();

        // mifDoc.curFormat().out( mifDoc.os() );
        // mifDoc.beginParaLine();
    }

    public void doEndParagraph(bool sustainFormatStack = false, bool sustainDisplayStack = false,
                               bool paragraphBreakTest = true, bool discardThisPara = false)
    {
        //    mifDoc.endParaLine();
        //    MifDoc::Para::outEpilog( mifDoc.os() );

        DisplayInfo? curDs = displayStack.First?.Value;
        System.Diagnostics.Debug.Assert(curDs != null);

        if (!sustainFormatStack)
            end();
        if (!sustainDisplayStack)
            endDisplay();

        if (paragraphBreakTest && paragraphBreakInEffect)
        {
            paragraphBreakInEffect = false;
            end();
        }

        // Desc: Content of document missing in the debug version
        // Author: Seshadri
        // Date: 24th feb 2000

        if (!curDs.paragraphClosedInMif)
        {
            MifDoc.Para? p = mifDoc.curPara();
            mifDoc.exitPara();
            if (!discardThisPara)
            {
                p.@out(mifDoc.os());
                mifDoc.curFormat().updateFrom(p.format());
            }
            // delete p; // C# GC will handle this
        }
    }

    public override void startDisplayGroup(DisplayGroupNIC nic)
    {
        startDisplay(nic);
        start();
    }

    public override void endDisplayGroup()
    {
        end();
        endDisplay();
    }

    public override void paragraphBreak(ParagraphNIC nic)
    {
        if (MifDoc.Para.currentlyOpened)
        {
            if (paragraphBreakInEffect)
                doEndParagraph(false, false, false);
            else
            {
                doEndParagraph(true, false, false);
                paragraphBreakInEffect = true;
            }
            doStartParagraph(nic);
        }
    }

    public override void externalGraphic(ExternalGraphicNIC nic)
    {
        bool isInline = MifDoc.Para.currentlyOpened ? true : false;

        if (!isInline)
            startDisplay(nic);
        start();

        MifDoc.T_pathname mifPathname = new MifDoc.T_pathname();
        if (systemIdToMifPathname(nic.entitySystemId, ref mifPathname))
        {
            MifDoc.T_keyword mifAlignment = new MifDoc.T_keyword(MifDoc.sLeft);
            switch (format().FotDisplayAlignment)
            {
                case Symbol.symbolStart:
                    mifAlignment = new MifDoc.T_keyword(MifDoc.sLeft);
                    break;
                case Symbol.symbolEnd:
                    mifAlignment = new MifDoc.T_keyword(MifDoc.sRight);
                    break;
                case Symbol.symbolCenter:
                    mifAlignment = new MifDoc.T_keyword(MifDoc.sCenter);
                    break;
                case Symbol.symbolInside:
                    mifAlignment = new MifDoc.T_keyword(MifDoc.sInside);
                    break;
                case Symbol.symbolOutside:
                    mifAlignment = new MifDoc.T_keyword(MifDoc.sOutside);
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }

            MifDoc.Frame frame
             = makeAnchoredFrame(new MifDoc.T_keyword(isInline ? MifDoc.sInline : MifDoc.sBelow),
                                  nic.hasMaxWidth ? computeLengthSpec(nic.maxWidth) : 72000,
                                  nic.hasMaxHeight ? computeLengthSpec(nic.maxHeight) : 72000,
                                  mifAlignment);

            MifDoc.ImportObject importObject
             = new MifDoc.ImportObject(mifPathname, frame.ShapeRect);
            frame.Objects.Add(importObject);

            if (!isInline)
                doStartParagraph(nic, true, 0, false);
            else
                checkForParagraphReopening();
            mifDoc.outAFrame(frame.ID, mifDoc.os());
            if (!isInline)
                endParagraph();
            else
                lastFlowObjectWasWhitespace = false;
        }

        end();
        if (!isInline)
            endDisplay();
    }

    public override void rule(RuleNIC nic)
    {
        bool isInline = (nic.orientation == Symbol.symbolHorizontal || nic.orientation == Symbol.symbolVertical)
                         ? false : true;

        if (isInline)
            checkForParagraphReopening();

        if (!isInline)
            startDisplay(nic);
        start();

        long indentlessDisplaySize
              = format().FotCurDisplaySize - computeLengthSpec(format().FotStartIndentSpec)
                - computeLengthSpec(format().FotEndIndentSpec);

        long ruleHeight
         = format().FotLineThickness + format().FotLineSep * (format().FotLineRepeat - 1);
        if (ruleHeight < 0)
            ruleHeight = 0;

        long ruleLength;
        long ruleOffset;

        if (nic.hasLength && (ruleLength = computeLengthSpec(nic.length)) > 0)
        {
            switch (format().FotDisplayAlignment)
            {
                case Symbol.symbolStart:
                default:
                    ruleOffset = 0;
                    break;
                case Symbol.symbolCenter:
                    ruleOffset = (indentlessDisplaySize - ruleLength) / 2;
                    break;
                case Symbol.symbolEnd:
                    ruleOffset = indentlessDisplaySize - ruleLength;
                    break;
            }
        }
        else
        {
            ruleOffset = 0;
            ruleLength = indentlessDisplaySize;
        }

        MifDoc.Frame frame
         = makeAnchoredFrame(new MifDoc.T_keyword(MifDoc.sInline), isInline ? ruleLength : indentlessDisplaySize,
                              ruleHeight);

        if (isInline)
            frame.setBLOffset(new MifDoc.T_dimension(computeLengthSpec(format().FotPositionPointShiftSpec)));
        else
            frame.setBLOffset(new MifDoc.T_dimension(-ruleHeight / 2 + 4000 / 3)); // min font baseline correction

        string capType;
        switch (format().FotLineCap)
        {
            case Symbol.symbolButt:
            default:
                capType = MifDoc.sButt;
                break;
            case Symbol.symbolRound:
                capType = MifDoc.sRound;
                break;
            case Symbol.symbolSquare:
                capType = MifDoc.sSquare;
                break;
        }

        long curLineVOffset = format().FotLineThickness / 2;
        for (long i = format().FotLineRepeat; i > 0; i--, curLineVOffset += format().FotLineSep)
        {
            MifDoc.PolyLine polyLine = new MifDoc.PolyLine(capType, 0, 0, format().FotLineThickness,
                                             format().FColor);
            frame.Objects.Add(polyLine);

            polyLine.setHeadCap(capType);
            polyLine.setTailCap(capType);
            polyLine.Points.Add(new MifDoc.T_XY(new MifDoc.T_dimension(ruleOffset), new MifDoc.T_dimension(curLineVOffset)));
            polyLine.Points.Add(new MifDoc.T_XY(new MifDoc.T_dimension(ruleOffset + ruleLength), new MifDoc.T_dimension(curLineVOffset)));
        }

        if (!isInline) doStartParagraph(nic, true, 0, true);
        mifDoc.outAFrame(frame.ID, mifDoc.os());
        if (!isInline)
            endParagraph();
        else
            lastFlowObjectWasWhitespace = false;

        end();
        if (!isInline)
            endDisplay();
    }

    public override void pageNumber()
    {
        mifDoc.outPageNumber();
    }

    public override void startScore(Symbol scoreType)
    {
        checkForParagraphReopening();

        switch (scoreType)
        {
            case Symbol.symbolBefore:
                nextFormat.setFOverline(true);
                break;
            case Symbol.symbolThrough:
                nextFormat.setFStrike(true);
                break;
            case Symbol.symbolAfter:
            default:
                nextFormat.setFUnderlining(
                    nextFormat.FotLineRepeat > 1 ? MifDoc.sFDouble : MifDoc.sFSingle);
                break;
        }

        start();
    }

    public override void endScore()
    {
        end();
    }

    public override void startLeader(LeaderNIC nic)
    {
        checkForParagraphReopening();
        lastFlowObjectWasWhitespace = false;

        mifDoc.outSpecialChar(MifDoc.sTab);
        inLeader = true;
        setCurLeaderStream(new MifTmpOutputByteStream());
    }

    public override void endLeader()
    {
        if (!mifDoc.curPara()!.leaderTabsSet)
        {
            StringBuilder leaderStr = new StringBuilder();
            curLeaderStream().commit(leaderStr);

            int numTabs = mifDoc.curFormat().PgfNumTabs + 1;
            mifDoc.curFormat().setPgfNumTabs(numTabs);
            mifDoc.curPara()!.format().TabStops.Add(
                new MifDoc.TabStop(MifDoc.sRight, format().FotCurDisplaySize
                                        - mifDoc.curFormat().PgfRIndent - 1,
                                   new MifDoc.T_string(leaderStr.ToString())));
            mifDoc.curPara()!.format().setPgfNumTabs(numTabs);
            mifDoc.curPara()!.curFormat().setPgfNumTabs(numTabs);
            mifDoc.curPara()!.format().setProperties |= (uint)MifDoc.ParagraphFormat.Flags.fTabStops;

            mifDoc.curPara()!.leaderTabsSet = true;
        }

        if (CurLeaderStream != null)
        {
            CurLeaderStream = null;
        }
        inLeader = false;
        lastFlowObjectWasWhitespace = false;
    }

    public override void startTable(TableNIC nic)
    {
        startDisplay(nic);
        start();

        if (!curTable().DefaultTblFormatGenerated)
        {
            MifDoc.TblFormat defaultTblFormat = new MifDoc.TblFormat(MifDoc.sDefaultTblFormat);
            defaultTblFormat.setDSSSLDefaults();
            mifDoc.tblCatalog().TblFormats.Add(defaultTblFormat);
            curTable().DefaultTblFormatGenerated = true;
        }

        long curStartIndent = computeLengthSpec(format().FotStartIndentSpec);
        curTable().startIndent = curStartIndent;

        curTable().begin(mifDoc);
        //    curTable().nic = nic;
        curTable().displayAlignment = format().FotDisplayAlignment;

        if (nic.widthType == TableNIC.WidthType.widthExplicit)
            curTable().tableWidth = computeLengthSpec(nic.width);
        else
            curTable().tableWidth
             = format().FotCurDisplaySize - curStartIndent
                - computeLengthSpec(format().FotEndIndentSpec);

        doStartParagraph(nic, true, 0);
        endParagraph();
    }

    public override void endTable()
    {
        bool firstPart, lastPart, hasHeader, hasFooter;
        for (int i = 0; i < curTable().TableParts.Count; i++)
        {
            firstPart = (i == 0);
            lastPart = (i == curTable().TableParts.Count - 1);
            TablePart tablePart = curTable().TableParts[i];
            tablePart.normalizeRows();
            hasHeader = tablePart.Header.Count > 0;
            hasFooter = tablePart.Footer.Count > 0;
            if (hasHeader)
                curTable().resolveBorders(tablePart.Header, firstPart, false);
            curTable().resolveBorders(tablePart.Body, !hasHeader, !hasFooter);
            if (hasFooter)
                curTable().resolveBorders(tablePart.Footer, false, lastPart);
            tablePart.translate(mifDoc);
        }

        MifDoc.CurInstance?.setCurTblNum(0);

        endDisplay();
        end();
    }

    public override void startTablePartSerial(TablePartNIC nic)
    {
        startDisplay(nic);
        start();

        if (curTable().NoTablePartsSeen)
            curTable().NoTablePartsSeen = false;
        else
            curTable().TableParts.Add(new TablePart());

        curTable().TableParts[curTable().TableParts.Count - 1].begin(curTable(), mifDoc);

        // Create a ParagraphNIC from the TablePartNIC base class
        ParagraphNIC pnic = new ParagraphNIC();
        pnic.spaceBefore = nic.spaceBefore;
        pnic.spaceAfter = nic.spaceAfter;
        pnic.keep = nic.keep;
        pnic.breakBefore = nic.breakBefore;
        pnic.breakAfter = nic.breakAfter;
        pnic.keepWithPrevious = nic.keepWithPrevious;
        pnic.keepWithNext = nic.keepWithNext;
        pnic.mayViolateKeepBefore = nic.mayViolateKeepBefore;
        pnic.mayViolateKeepAfter = nic.mayViolateKeepAfter;
        doStartParagraph(pnic, true, 0);
        endParagraph();
    }

    public override void endTablePartSerial()
    {
        curTable().CurTablePart = null;
        endDisplay();
        end();
    }

    public override void startTablePartHeader()
    {
        curTable().CurRows = curTable().curTablePart().Header;
    }

    public override void endTablePartHeader()
    {
        curTable().CurRows = curTable().curTablePart().Body;
    }

    public override void startTablePartFooter()
    {
        curTable().CurRows = curTable().curTablePart().Footer;
    }

    public override void endTablePartFooter()
    {
        curTable().CurRows = curTable().curTablePart().Body;
    }

    public override void tableColumn(TableColumnNIC nic)
    {
        if ((int)nic.columnIndex >= curTable().curTablePart().Columns.Count)
        {
            while (curTable().curTablePart().Columns.Count <= (int)nic.columnIndex)
                curTable().curTablePart().Columns.Add(new Column());
        }

        curTable().curTablePart().Columns[(int)nic.columnIndex].hasWidth = nic.hasWidth;
        if (nic.hasWidth)
        {
            curTable().curTablePart().Columns[(int)nic.columnIndex].width = nic.width;
        }
    }

    public override void startTableRow()
    {
        curTable().curRows().Add(new Row());
    }

    public override void endTableRow()
    {
    }

    public override void startTableCell(TableCellNIC nic)
    {
        start();

        TablePart tp = curTable().curTablePart();
        if (!tp.columnsProcessed)
            tp.processColumns();

        System.Collections.Generic.List<Cell> Cells = curTable().curRows()[curTable().curRows().Count - 1].Cells;
        while (nic.columnIndex >= Cells.Count)
            Cells.Add(new Cell());

        Cell cell = Cells[(int)nic.columnIndex];
        curTable().CurCell = cell;
        cell.missing = nic.missing;

        if (nic.nColumnsSpanned != 1)
        {
            cell.nColumnsSpanned = nic.nColumnsSpanned;
            cell.mifCell().setCellColumns((int)nic.nColumnsSpanned);
        }

        if (nic.nRowsSpanned != 1)
        {
            cell.nRowsSpanned = nic.nRowsSpanned;
            cell.mifCell().setCellRows((int)nic.nRowsSpanned);
        }

        if (format().FotCellBackground && format().MifBackgroundColor.Length > 0)
        {
            cell.mifCell().setCellFill(0);
            cell.mifCell().setCellColor(format().MifBackgroundColor);
        }

        long newDisplaySize = 0;
        for (uint i = nic.columnIndex; i < nic.columnIndex + nic.nColumnsSpanned; i++)
            if (i < tp.Columns.Count)
            {
                if (tp.Columns[(int)i].hasWidth)
                    newDisplaySize
                     += computeLengthSpec(tp.Columns[(int)i].width);
            }
            else if (!nic.missing)
            {
                App.message(MifMessages.missingTableColumnFlowObject);
                // NOTE: at this point there's already a danger of not realizing
                // right display space sizes inside cells
                tp.Columns.Add(new Column());
                tp.Columns[tp.Columns.Count - 1].hasWidth = true;
                TableLengthSpec tls = new TableLengthSpec();
                tls.tableUnitFactor = 1.0;
                tp.Columns[tp.Columns.Count - 1].width = tls;
                tp.needsColumnReprocessing = true;
                if ((int)i > tp.mifTable(mifDoc).TblNumColumns)
                    tp.mifTable(mifDoc).setTblNumColumns((int)i);
            }

        newDisplaySize -= format().PgfCellMargins.l + format().PgfCellMargins.r;

        if (newDisplaySize > 0)
            format().FotCurDisplaySize = newDisplaySize;

        cell.displaySize = format().FotCurDisplaySize;
        mifDoc.enterTableCell(cell.mifCell());
    }

    public override void endTableCell()
    {
        mifDoc.exitTableCell();
        end();

        curTable().CurCell = null;
    }

    // Setters
    public override void setFontSize(long n) { nextFormat.FSize = n; }
    public override void setLineSpacing(LengthSpec l) { nextFormat.FotLineSpacingSpec = l; }

    public override void setFontWeight(Symbol weight)
    {
        nextFormat.FWeight = (weight > Symbol.symbolMedium) ? MifDoc.sBold : MifDoc.sRegular;
    }

    public override void setFontPosture(Symbol posture)
    {
        switch (posture)
        {
            case Symbol.symbolOblique:
            case Symbol.symbolBackSlantedOblique:
            case Symbol.symbolItalic:
            case Symbol.symbolBackSlantedItalic:
                nextFormat.FAngle = MifDoc.sItalic;
                break;
            default:
                nextFormat.FAngle = MifDoc.sRegular;
                break;
        }
    }

    public override void setStartIndent(LengthSpec l) { nextFormat.FotStartIndentSpec = l; }
    public override void setEndIndent(LengthSpec l) { nextFormat.FotEndIndentSpec = l; }
    public override void setFirstLineStartIndent(LengthSpec l) { nextFormat.FotFirstLineStartIndentSpec = l; }

    public override void setQuadding(Symbol quadding)
    {
        switch (quadding)
        {
            case Symbol.symbolEnd:
                nextFormat.PgfAlignment = MifDoc.sRight;
                break;
            case Symbol.symbolCenter:
                nextFormat.PgfAlignment = MifDoc.sCenter;
                break;
            case Symbol.symbolJustify:
                nextFormat.PgfAlignment = MifDoc.sLeftRight;
                break;
            default:
                nextFormat.PgfAlignment = MifDoc.sLeft;
                break;
        }
    }

    public override void setDisplayAlignment(Symbol alignment) { nextFormat.FotDisplayAlignment = alignment; }
    public override void setFieldAlign(Symbol align) { nextFormat.FotFieldAlign = align; }

    public override void setColor(DeviceRGBColor rgbColor)
    {
        var color = new MifDoc.Color(rgbColor.red, rgbColor.green, rgbColor.blue);
        nextFormat.FColor = color.ColorTag;
        if (!mifDoc.colorCatalog().Colors.ContainsKey(color.ColorTag))
        {
            mifDoc.colorCatalog().Colors[color.ColorTag] = color;
        }
    }

    public override void setBackgroundColor(DeviceRGBColor rgbColor)
    {
        var color = new MifDoc.Color(rgbColor.red, rgbColor.green, rgbColor.blue);
        nextFormat.MifBackgroundColor = color.ColorTag;
        if (!mifDoc.colorCatalog().Colors.ContainsKey(color.ColorTag))
        {
            mifDoc.colorCatalog().Colors[color.ColorTag] = color;
        }
    }

    public override void setBackgroundColor()
    {
        nextFormat.MifBackgroundColor = "";
    }

    public override void setPageWidth(long pWidth) { nextFormat.FotPageWidth = pWidth; }
    public override void setPageHeight(long pHeight) { nextFormat.FotPageHeight = pHeight; }
    public override void setLeftMargin(long leftM) { nextFormat.FotLeftMargin = leftM; }
    public override void setRightMargin(long rightM) { nextFormat.FotRightMargin = rightM; }
    public override void setTopMargin(long topM) { nextFormat.FotTopMargin = topM; }
    public override void setBottomMargin(long bottomM) { nextFormat.FotBottomMargin = bottomM; }
    public override void setHeaderMargin(long headerM) { nextFormat.FotHeaderMargin = headerM; }
    public override void setFooterMargin(long footerM) { nextFormat.FotFooterMargin = footerM; }

    public override void setBorderPresent(bool present) { nextFormat.FotBorderPresent = present; }
    public override void setLineThickness(long thickness) { nextFormat.FotLineThickness = thickness; }
    public override void setLineSep(long sep) { nextFormat.FotLineSep = sep; }
    public override void setBorderPriority(long priority) { nextFormat.FotBorderPriority = priority; }
    public override void setLineRepeat(long repeat) { nextFormat.FotLineRepeat = repeat; }
    public override void setSpan(long span) { nextFormat.FotSpan = span; }
    public override void setLineCap(Symbol cap) { nextFormat.FotLineCap = cap; }

    public override void setFontFamilyName(StringC s)
    {
        var sb = new StringBuilder();
        for (nuint i = 0; i < s.size(); i++)
            sb.Append((char)s[i]);
        nextFormat.FFamily = sb.ToString();
    }

    public override void setWidowCount(long n) { nextFormat.PgfBlockSize = (int)n; }
    public override void setOrphanCount(long n) { nextFormat.PgfBlockSize = (int)n; }

    public override void setKern(bool kern) { nextFormat.FPairKern = kern; }

    public override void setLanguage(Letter2 code)
    {
        nextFormat.FotLanguage = code;
        nextFormat.computePgfLanguage();
    }

    public override void setCountry(Letter2 code)
    {
        nextFormat.FotCountry = code;
        nextFormat.computePgfLanguage();
    }

    public override void setHyphenate(bool hyphenate) { nextFormat.PgfHyphenate = hyphenate; }
    public override void setHyphenationRemainCharCount(long n) { nextFormat.HyphenMinPrefix = (int)n; }
    public override void setHyphenationPushCharCount(long n) { nextFormat.HyphenMinSuffix = (int)n; }
    public override void setHyphenationLadderCount(long n) { nextFormat.HyphenMaxLines = (int)n; }

    public override void setMinLeading(OptLengthSpec spec) { nextFormat.FotMinLeading = spec; }
    public override void setInputWhitespaceTreatment(Symbol treatment) { nextFormat.FotInputWhitespaceTreatment = treatment; }
    public override void setLines(Symbol lines) { nextFormat.FotLines = lines; }
    public override void setFieldWidth(LengthSpec spec) { nextFormat.FotFieldWidth = spec; }
    public override void setPositionPointShift(LengthSpec spec) { nextFormat.FotPositionPointShiftSpec = spec; }
    public override void setBorderOmitAtBreak(bool omit) { }
    public override void setCellBackground(bool bg) { nextFormat.FotCellBackground = bg; }
    public override void setCellRowAlignment(Symbol alignment) { }
    public override void setCellBeforeRowMargin(long m) { }
    public override void setCellAfterRowMargin(long m) { }
    public override void setCellBeforeColumnMargin(long m) { }
    public override void setCellAfterColumnMargin(long m) { }

    // Table border methods
    public override void tableBeforeRowBorder()
    {
        start();
        curTable().beforeRowBorder.setFromFot();
        end();
    }

    public override void tableAfterRowBorder()
    {
        start();
        curTable().afterRowBorder.setFromFot();
        end();
    }

    public override void tableBeforeColumnBorder()
    {
        start();
        curTable().beforeColumnBorder.setFromFot();
        end();
    }

    public override void tableAfterColumnBorder()
    {
        start();
        curTable().afterColumnBorder.setFromFot();
        end();
    }

    public override void tableCellBeforeRowBorder()
    {
        start();
        curTable().curCell().beforeRowBorder.setFromFot();
        end();
    }

    public override void tableCellAfterRowBorder()
    {
        start();
        curTable().curCell().afterRowBorder.setFromFot();
        end();
    }

    public override void tableCellBeforeColumnBorder()
    {
        start();
        curTable().curCell().beforeColumnBorder.setFromFot();
        end();
    }

    public override void tableCellAfterColumnBorder()
    {
        start();
        curTable().curCell().afterColumnBorder.setFromFot();
        end();
    }

    // Helper methods
    public void synchronizeFontFormat()
    {
        if (mifDoc.curPara(false) != null)
        {
            mifDoc.curPara()!.curFormat().ffUpdateFrom(nextFormat);
            mifDoc.curPara()!.curFormat().ffOut(mifDoc.os(), MifDoc.FontFormat.FontStatement.stFont);
        }
        else
        {
            mifDoc.curFormat().ffUpdateFrom(nextFormat);
            mifDoc.curFormat().ffOut(mifDoc.os(), MifDoc.FontFormat.FontStatement.stFont);
        }
        outPendingInlineStatements();
    }

    public long computeLengthSpec(LengthSpec spec)
    {
        if (spec.displaySizeFactor == 0.0)
        {
            return spec.length;
        }
        else
        {
            double tem = format().FotCurDisplaySize * spec.displaySizeFactor;
            return spec.length + (long)(tem >= 0.0 ? tem + 0.5 : tem - 0.5);
        }
    }

    public long computeLengthSpec(TableLengthSpec spec)
    {
        // TableLengthSpec has tableUnitFactor - handle it
        // For now, just use the base length
        return spec.length;
    }

    protected void makeEmptyTextFlow(MifDoc.TextRect textRect)
    {
        MifDoc.TextFlow textFlow = new MifDoc.TextFlow(textRect, true);
        mifDoc.textFlows().Add(textFlow);

        mifDoc.enterTextFlow(textFlow);
        MifDoc.Para.outSimpleProlog(mifDoc.os());
        MifDoc.ParaLine.outProlog(mifDoc.os());
        MifDoc.ParaLine.outEpilog(mifDoc.os());
        MifDoc.Para.outEpilog(mifDoc.os());
        mifDoc.exitTextFlow();
    }

    protected void setupHeaderFooterParagraphFormat(MifDoc.ParagraphFormat hpf, MifDoc.ParagraphFormat fpf,
                                                    MifDoc.T_dimension textRectWidth)
    {
        MifDoc.TabStop centerTS = new MifDoc.TabStop(MifDoc.sCenter, (long)(textRectWidth / 2));
        MifDoc.TabStop rightTS = new MifDoc.TabStop(MifDoc.sRight, (long)textRectWidth);

        hpf.setFrom(FotSimplePageSequence.paragraphFormat, 0, (uint)MifDoc.FontFormat.Flags.fAll);
        hpf.TabStops.Add(centerTS);
        hpf.TabStops.Add(rightTS);
        hpf.setProperties |= (uint)MifDoc.ParagraphFormat.Flags.fTabStops;
        fpf.copyFrom(hpf);
        hpf.setFSize(((format().FotBottomMargin - format().FotFooterMargin) * 3) / 2);
        fpf.setFSize((format().FotHeaderMargin * 3) / 2);
        hpf.setPgfTag(MifDoc.sHeader);
        fpf.setPgfTag(MifDoc.sFooter);

        mifDoc.pgfCatalog().ParaFormats.Add(hpf);
        mifDoc.pgfCatalog().ParaFormats.Add(fpf);

        hpf.clearSetProperties();
        fpf.clearSetProperties();
    }

    protected void setupSimplePageSequence()
    {
        MifDoc.Page firstMasterPage = new MifDoc.Page(MifDoc.sOtherMasterPage, MifDoc.sFirst);
        MifDoc.Page rightMasterPage = new MifDoc.Page(MifDoc.sRightMasterPage, MifDoc.sRight);
        MifDoc.Page leftMasterPage = new MifDoc.Page(MifDoc.sLeftMasterPage, MifDoc.sLeft);
        MifDoc.Page bodyPage = new MifDoc.Page(MifDoc.sBodyPage, MifDoc.sNONE, MifDoc.sFirst);

        MifDoc.T_LTWH bodyRect = new MifDoc.T_LTWH();
        MifDoc.T_LTWH headerRect = new MifDoc.T_LTWH();
        MifDoc.T_LTWH footerRect = new MifDoc.T_LTWH();

        bodyRect.l = format().FotLeftMargin;
        bodyRect.t = format().FotTopMargin;
        bodyRect.w = format().FotPageWidth - format().FotLeftMargin - format().FotRightMargin;
        bodyRect.h = format().FotPageHeight - format().FotTopMargin - format().FotBottomMargin;

        headerRect.l = format().FotLeftMargin;
        headerRect.t = 0;
        headerRect.w = bodyRect.w;
        headerRect.h = format().FotTopMargin;

        footerRect.l = format().FotLeftMargin;
        footerRect.t = format().FotPageHeight - format().FotBottomMargin;
        footerRect.w = bodyRect.w;
        footerRect.h = format().FotBottomMargin;

        MifDoc.TextRect firstBodyTextRect = new MifDoc.TextRect(bodyRect, (int)format().FotPageNColumns,
            format().FotPageColumnSep, format().FotPageBalanceColumns);
        MifDoc.TextRect rightBodyTextRect = new MifDoc.TextRect(bodyRect, (int)format().FotPageNColumns,
            format().FotPageColumnSep, format().FotPageBalanceColumns);
        MifDoc.TextRect leftBodyTextRect = new MifDoc.TextRect(bodyRect, (int)format().FotPageNColumns,
            format().FotPageColumnSep, format().FotPageBalanceColumns);
        MifDoc.TextRect bodyTextRect = new MifDoc.TextRect(bodyRect, (int)format().FotPageNColumns,
            format().FotPageColumnSep, format().FotPageBalanceColumns);
        MifDoc.TextRect firstHeaderTextRect = new MifDoc.TextRect(headerRect);
        MifDoc.TextRect rightHeaderTextRect = new MifDoc.TextRect(headerRect);
        MifDoc.TextRect leftHeaderTextRect = new MifDoc.TextRect(headerRect);
        MifDoc.TextRect firstFooterTextRect = new MifDoc.TextRect(footerRect);
        MifDoc.TextRect rightFooterTextRect = new MifDoc.TextRect(footerRect);
        MifDoc.TextRect leftFooterTextRect = new MifDoc.TextRect(footerRect);

        firstMasterPage.TextRects.Add(firstHeaderTextRect);
        firstMasterPage.TextRects.Add(firstBodyTextRect);
        firstMasterPage.TextRects.Add(firstFooterTextRect);

        rightMasterPage.TextRects.Add(rightHeaderTextRect);
        rightMasterPage.TextRects.Add(rightBodyTextRect);
        rightMasterPage.TextRects.Add(rightFooterTextRect);

        leftMasterPage.TextRects.Add(leftHeaderTextRect);
        leftMasterPage.TextRects.Add(leftBodyTextRect);
        leftMasterPage.TextRects.Add(leftFooterTextRect);

        bodyPage.TextRects.Add(bodyTextRect);
        mifDoc.pages().Add(bodyPage);
        mifDoc.pages().Add(firstMasterPage);
        mifDoc.pages().Add(rightMasterPage);
        mifDoc.pages().Add(leftMasterPage);

        MifDoc.ParagraphFormat headerPF = new MifDoc.ParagraphFormat();
        headerPF.setDSSSLDefaults();
        MifDoc.ParagraphFormat footerPF = new MifDoc.ParagraphFormat();
        footerPF.setDSSSLDefaults();
        setupHeaderFooterParagraphFormat(headerPF, footerPF, new MifDoc.T_dimension(bodyRect.w));

        FotSimplePageSequence.BodyTextFlow = new MifDoc.TextFlow(bodyTextRect, true,
            FotSimplePageSequence.paragraphFormat, MifDoc.sDefaultPgfFormat);
        FotSimplePageSequence.FirstHeaderTextFlow = new MifDoc.TextFlow(firstHeaderTextRect, false,
            headerPF, MifDoc.sHeader);
        FotSimplePageSequence.FirstFooterTextFlow = new MifDoc.TextFlow(firstFooterTextRect, false,
            footerPF, MifDoc.sFooter);
        FotSimplePageSequence.LeftHeaderTextFlow = new MifDoc.TextFlow(leftHeaderTextRect, false,
            headerPF, MifDoc.sHeader);
        FotSimplePageSequence.LeftFooterTextFlow = new MifDoc.TextFlow(leftFooterTextRect, false,
            footerPF, MifDoc.sFooter);
        FotSimplePageSequence.RightHeaderTextFlow = new MifDoc.TextFlow(rightHeaderTextRect, false,
            headerPF, MifDoc.sHeader);
        FotSimplePageSequence.RightFooterTextFlow = new MifDoc.TextFlow(rightFooterTextRect, false,
            footerPF, MifDoc.sFooter);

        makeEmptyTextFlow(firstBodyTextRect);
        makeEmptyTextFlow(leftBodyTextRect);
        makeEmptyTextFlow(rightBodyTextRect);

        mifDoc.textFlows().Add(FotSimplePageSequence.BodyTextFlow);
        mifDoc.textFlows().Add(FotSimplePageSequence.FirstHeaderTextFlow);
        mifDoc.textFlows().Add(FotSimplePageSequence.FirstFooterTextFlow);
        mifDoc.textFlows().Add(FotSimplePageSequence.LeftHeaderTextFlow);
        mifDoc.textFlows().Add(FotSimplePageSequence.LeftFooterTextFlow);
        mifDoc.textFlows().Add(FotSimplePageSequence.RightHeaderTextFlow);
        mifDoc.textFlows().Add(FotSimplePageSequence.RightFooterTextFlow);

        mifDoc.document().setDTwoSides(true);
        mifDoc.document().setDParity(MifDoc.sFirstRight);

        mifDoc.enterTextFlow(FotSimplePageSequence.BodyTextFlow);
    }

    protected void beginHeader()
    {
        beginHeaderFooter(true);
    }

    protected void beginFooter()
    {
        beginHeaderFooter(false);
    }

    protected void beginHeaderFooter(bool header)
    {
        start();

        MifDoc.Para p = new MifDoc.Para();
        p.setPgfTag(header ? MifDoc.sHeader : MifDoc.sFooter);
        p.setProperties &= ~0x2u;  // ~fParagraphFormat
        p.outProlog(mifDoc.os());

        MifDoc.FontFormat ff = new MifDoc.FontFormat();
        ff.setFSize(format().FSize);
        ff.@out(mifDoc.os(), (uint)MifDoc.FontFormat.Flags.fFSize, MifDoc.FontFormat.FontStatement.stFont);

        mifDoc.beginParaLine();
    }

    protected void endHeaderFooter(uint hfPart)
    {
        const uint rightHF = (uint)HF.rightHF;

        mifDoc.endParaLine();
        MifDoc.Para.outEpilog(mifDoc.os());

        // Right header missing because the prev stmt has set
        // currentlyOpened flag to false for a right footer. So reset it.
        if ((hfPart & rightHF) != 0)
        {
            MifDoc.Para.currentlyOpened = true;
        }

        end();
    }

    public Format format()
    {
        var result = formatStack.Count > 0 ? formatStack[0] : null;
        System.Diagnostics.Debug.Assert(result != null);
        return result!;
    }

    public Table curTable() => CurTable;

    public void setCurLeaderStream(MifTmpOutputByteStream? s, bool doDelete = true)
    {
        CurLeaderStream = s;
    }

    public MifTmpOutputByteStream curLeaderStream()
    {
        System.Diagnostics.Debug.Assert(CurLeaderStream != null);
        return CurLeaderStream!;
    }

    protected void initMifBookComponent()
    {
        var defaultParaFormat = new MifDoc.ParagraphFormat();
        defaultParaFormat.setDSSSLDefaults();
        defaultParaFormat.setPgfTag(MifDoc.sDefaultPgfFormat);
        mifDoc.pgfCatalog().ParaFormats.Add(defaultParaFormat);

        mifDoc.document().setDPageSize(
            new MifDoc.T_WH(new MifDoc.T_dimension(format().FotPageWidth),
                           new MifDoc.T_dimension(format().FotPageHeight)));
        mifDoc.document().setDMargins(new MifDoc.T_LTRB(
            new MifDoc.T_dimension(1), new MifDoc.T_dimension(1),
            new MifDoc.T_dimension(1), new MifDoc.T_dimension(1)));
        // MIF doesn't accept zeros
        mifDoc.document().setDColumns(1);
    }

    protected EffectiveDisplaySpace createEffectiveDisplaySpace(DisplaySpace ds)
    {
        var result = new EffectiveDisplaySpace();
        result.set(computeLengthSpec(ds.nominal), computeLengthSpec(ds.min),
                   computeLengthSpec(ds.max), ds.priority, ds.conditional, ds.force);
        return result;
    }

    protected void checkForParagraphReopening()
    {
        DisplayInfo? curDs = displayStack.First?.Value;
        if (curDs != null && curDs.paragraphClosedInMif)
        {
            Format f = format();

            f.setPgfWithPrev(curDs.firstParaOutputed ? curDs.keepWithinPageInEffect : false);
            f.setPgfWithNext(false);
            curDs.firstParaOutputed = true;

            processDisplaySpaceStack();
            switch (pendingBreak)
            {
                case Symbol.symbolPage:
                    f.setPgfPlacement(MifDoc.sPageTop);
                    break;
                case Symbol.symbolColumn:
                    f.setPgfPlacement(MifDoc.sColumnTop);
                    break;
                default:
                    f.setPgfPlacement(MifDoc.sAnywhere);
                    break;
            }
            pendingBreak = Symbol.symbolFalse;

            f.setPgfSpBefore(pendingEffectiveDisplaySpace.nominal);
            pendingEffectiveDisplaySpace.clear();
            f.setPgfFIndent(nextFormat.PgfLIndent);

            MifDoc.Para p = new MifDoc.Para();
            mifDoc.enterPara(p);
            f.@out(mifDoc.os());
            mifDoc.beginParaLine();

            synchronizeFontFormat();
            curDs.paragraphClosedInMif = false;
        }
    }

    protected void outPendingInlineStatements()
    {
        if (linkStack.Count > 1
            && linkStack[linkStack.Count - 2].openedInMif
            && !linkStack[linkStack.Count - 2].forcesNoLink())
        {
            LinkInfo.pendingMifClosings++;
            linkStack[linkStack.Count - 2].openedInMif = false;
        }

        for (; LinkInfo.pendingMifClosings > 0; LinkInfo.pendingMifClosings--)
        {
            MifDoc.Marker marker = new MifDoc.Marker(new MifDoc.T_string(""), MifDoc.Marker.MarkerType.Hypertext);
            marker.@out(mifDoc.os());
        }

        if (indexEntryStack.Count > 0)
        {
            indexEntryStack[indexEntryStack.Count - 1].@out(mifDoc.os());
            indexEntryStack.RemoveAt(indexEntryStack.Count - 1);
        }

        for (int i = 0; i < nodeStack.Count; i++)
        {
            ulong n = 0;
            if (nodeStack[i].node.elementIndex(ref n) == AccessResult.accessOK)
            {
                GroveString id = new GroveString();
                nodeStack[i].node.getId(ref id);
                ulong groveIndex = nodeStack[i].node.groveIndex();
                _ = mifDoc.os() << new MifDoc.CrossRefInfo(
                        groveIndex, n, mifDoc.os().CurTagIndent,
                        MifDoc.CrossRefInfo.InfoType.PotentialMarker, id.data(), id.size());
                if (id.size() > 0)
                    mifDoc.elements().setBookComponentIndex(
                        groveIndex, new StringC(id.data(), id.size()),
                        mifDoc.bookComponents().Count - 1);
                else
                    mifDoc.elements().setBookComponentIndex(
                        groveIndex, n, mifDoc.bookComponents().Count - 1);
            }
        }
        NodeInfo.nonEmptyElementsOpened = 0;
        nodeStack.Clear();

        if (linkStack.Count > 0
            && !linkStack[linkStack.Count - 1].openedInMif
            && !linkStack[linkStack.Count - 1].forcesNoLink())
        {
            linkStack[linkStack.Count - 1].crossRefInfo!.tagIndent = mifDoc.os().CurTagIndent;
            _ = mifDoc.os() << linkStack[linkStack.Count - 1].crossRefInfo!;
            linkStack[linkStack.Count - 1].openedInMif = true;
        }
    }

    protected void outString(Char[] s, nuint n, MifTmpOutputByteStream? o = null,
                            bool inParagraph = true, StringBuilder? targetString = null)
    {
        MifOutputByteStream? outS
         = (o != null) ? o.stream()
                       : ((targetString != null) ? null : mifDoc.os());

        MifDoc.ParagraphFormat? curPFormat
         = inParagraph
            ? (mifDoc.curPara(false) != null ? mifDoc.curPara()!.curFormat() : mifDoc.curFormat())
            : null;
        MifDoc.T_string paraFFamily = new MifDoc.T_string();
        if (curPFormat != null)
            paraFFamily = new MifDoc.T_string(curPFormat.FFamily);
        bool stringOpened = false;
        bool thisFlowObjectIsWhitespace;

        for (nuint i = 0; i < n; i++)
        {
            Char c = s[i];
            thisFlowObjectIsWhitespace = false;
            string? outStr = null;
            char outChr = '\0';
            string? outSpecialChar = null;
            bool hasOutput = false;

            switch (c)
            {
                case '\n': break;
                case '\r':
                    if (!inParagraph)
                    {
                        outChr = ' '; hasOutput = true;
                    }
                    else
                    {
                        switch (format().FotLines)
                        {
                            case Symbol.symbolNone:
                            case Symbol.symbolWrap:
                                switch (format().FotInputWhitespaceTreatment)
                                {
                                    case Symbol.symbolIgnore: break;
                                    case Symbol.symbolCollapse:
                                        if (lastFlowObjectWasWhitespace) break;
                                        goto case Symbol.symbolPreserve;
                                    case Symbol.symbolPreserve:
                                    default:
                                        outChr = ' '; hasOutput = true;
                                        break;
                                }
                                break;
                            default:
                                // Hard return - output special char
                                if (outS != null && stringOpened)
                                {
                                    _ = outS << "'>";
                                    stringOpened = false;
                                }
                                mifDoc.outSpecialChar(MifDoc.sHardReturn, outS);
                                MifDoc.ParaLine.outEpilog(outS!);
                                MifDoc.ParaLine.outProlog(outS!);
                                break;
                        }
                        thisFlowObjectIsWhitespace = true;
                    }
                    break;
                case '\t':
                    if (!inParagraph)
                    {
                        outStr = "\\t"; hasOutput = true;
                    }
                    else
                    {
                        switch (format().FotInputWhitespaceTreatment)
                        {
                            case Symbol.symbolIgnore: break;
                            case Symbol.symbolCollapse:
                                if (lastFlowObjectWasWhitespace) break;
                                goto case Symbol.symbolPreserve;
                            case Symbol.symbolPreserve:
                            default:
                                outStr = "\\t"; hasOutput = true;
                                break;
                        }
                        thisFlowObjectIsWhitespace = true;
                    }
                    break;
                case '>': outStr = "\\>"; hasOutput = true; break;
                case '\'': outStr = "\\q"; hasOutput = true; break;
                case '`': outStr = "\\Q"; hasOutput = true; break;
                case '\\': outStr = "\\\\"; hasOutput = true; break;
                case ' ':
                    if (!inParagraph)
                    {
                        outChr = ' '; hasOutput = true;
                    }
                    else
                    {
                        switch (format().FotInputWhitespaceTreatment)
                        {
                            case Symbol.symbolIgnore: break;
                            case Symbol.symbolCollapse:
                                if (lastFlowObjectWasWhitespace) break;
                                goto case Symbol.symbolPreserve;
                            case Symbol.symbolPreserve:
                            default:
                                outChr = ' '; hasOutput = true;
                                break;
                        }
                        thisFlowObjectIsWhitespace = true;
                    }
                    break;
                case 0x00A0: outSpecialChar = MifDoc.sHardSpace; hasOutput = true; break;
                case 0x00A2: outSpecialChar = MifDoc.sCent; hasOutput = true; break;
                case 0x00A3: case 0x20A4: outSpecialChar = MifDoc.sPound; hasOutput = true; break;
                case 0x00A5: outSpecialChar = MifDoc.sYen; hasOutput = true; break;
                case 0x2002: outSpecialChar = MifDoc.sEnSpace; hasOutput = true; break;
                case 0x2003: outSpecialChar = MifDoc.sEmSpace; hasOutput = true; break;
                case 0x2009: outSpecialChar = MifDoc.sThinSpace; hasOutput = true; break;
                case 0x2010: outSpecialChar = MifDoc.sSoftHyphen; hasOutput = true; break;
                case 0x2011: outSpecialChar = MifDoc.sHardHyphen; hasOutput = true; break;
                case 0x2013: outSpecialChar = MifDoc.sEnDash; hasOutput = true; break;
                case 0x2014: outSpecialChar = MifDoc.sEmDash; hasOutput = true; break;
                case 0x2020: outSpecialChar = MifDoc.sDagger; hasOutput = true; break;
                case 0x2021: outSpecialChar = MifDoc.sDoubleDagger; hasOutput = true; break;
                case 0x2022: outSpecialChar = MifDoc.sBullet; hasOutput = true; break;
                default:
                    if (c >= 0x80)
                    {
                        ulong code = CharTable[c];
                        if ((code & CHAR_TABLE_SYMBOL_FLAG) != 0)
                        {
                            // Symbol font char - use hex output
                            if (curPFormat != null)
                            {
                                uint charCode = (uint)(code & 0xff);
                                if (targetString != null)
                                    mifDoc.outHexChar(charCode, new MifDoc.T_string(targetString.ToString()));
                                else if (outS != null)
                                    mifDoc.outHexChar(charCode, outS);
                            }
                        }
                        else if (code != 0)
                        {
                            uint charCode = (uint)(code & 0xff);
                            if (targetString != null)
                                mifDoc.outHexChar(charCode, new MifDoc.T_string(targetString.ToString()));
                            else if (outS != null)
                                mifDoc.outHexChar(charCode, outS);
                        }
                    }
                    else
                    {
                        outChr = (char)c; hasOutput = true;
                    }
                    break;
            }

            if (hasOutput)
            {
                if (curPFormat != null && !stringOpened && outS != null)
                {
                    _ = outS << '\n' << MifOutputByteStream.INDENT << "<String `";
                    stringOpened = true;
                }
                if (outChr != '\0')
                {
                    if (targetString != null)
                        targetString.Append(outChr);
                    else if (outS != null)
                        _ = outS << outChr;
                }
                else if (outStr != null)
                {
                    if (targetString != null)
                        targetString.Append(outStr);
                    else if (outS != null)
                        _ = outS << outStr;
                }
                else if (outSpecialChar != null && inParagraph)
                {
                    if (outS != null && stringOpened)
                    {
                        _ = outS << "'>";
                        stringOpened = false;
                    }
                    mifDoc.outSpecialChar(outSpecialChar, outS);
                }
            }

            lastFlowObjectWasWhitespace = thisFlowObjectIsWhitespace;
        }

        if (outS != null && stringOpened)
            _ = outS << "'>";
    }

    protected void startDisplay(DisplayNIC nic)
    {
        if (!inSimplePageSequence)
        {
            if (!bookComponentOpened)
            {
                mifDoc.enterBookComponent();
                initMifBookComponent();
                bookComponentOpened = true;
            }
            bookComponentAvailable = false;
        }

        displaySpaceQueue.Enqueue(new DisplaySpaceInfo(nic.spaceBefore, nic.breakBefore, false));

        DisplayInfo? di = displayStack.First?.Value;
        if (di != null && di.isParagraph && !di.paragraphClosedInMif)
        {
            if (mifDoc.curPara()?.content().isEmpty() ?? false)
            {
                doEndParagraph(true, true, true, true);
            }
            else
                doEndParagraph(true, true);
            di.paragraphClosedInMif = true;
        }

        if (curTable().CurCell != null)
            nextFormat.FotCurDisplaySize = curTable().CurCell.displaySize;
        else if (nextFormat.FotSpan > 1)
            nextFormat.FotCurDisplaySize
                = nextFormat.FotPageWidth - nextFormat.FotLeftMargin - nextFormat.FotRightMargin;
        else
            nextFormat.FotCurDisplaySize
                = (nextFormat.FotPageWidth - nextFormat.FotLeftMargin - nextFormat.FotRightMargin
                    - nextFormat.FotPageColumnSep * (nextFormat.FotPageNColumns - 1))
                  / nextFormat.FotPageNColumns;

        displayStack.AddFirst(new DisplayInfo(nic, displayStack.First?.Value));
    }

    protected void endDisplay()
    {
        DisplayInfo? di = displayStack.First?.Value;
        if (di != null)
        {
            displayStack.RemoveFirst();
            displaySpaceQueue.Enqueue(new DisplaySpaceInfo(di.spaceAfter, di.breakAfter, true));
        }
    }

    protected void processDisplaySpaceStack()
    {
        pendingBreak = Symbol.symbolFalse;
        EffectiveDisplaySpace effectiveDisplaySpace = new EffectiveDisplaySpace();
        while (displaySpaceQueue.Count > 0)
        {
            DisplaySpaceInfo curDSI = displaySpaceQueue.Dequeue();
            if (curDSI.breakType == Symbol.symbolPage || curDSI.breakType == Symbol.symbolColumn)
            {
                effectiveDisplaySpace.clear();
                if (pendingBreak != Symbol.symbolFalse)
                    mifDoc.outBreakingPara(curDSI.breakType == Symbol.symbolPage
                                            ? MifDoc.sPageTop : MifDoc.sColumnTop);
                pendingBreak = curDSI.breakType;
                if (!curDSI.breakIsAfter)
                    effectiveDisplaySpace.combine(createEffectiveDisplaySpace(curDSI.space));
            }
            else
                effectiveDisplaySpace.combine(createEffectiveDisplaySpace(curDSI.space));
        }

        pendingEffectiveDisplaySpace = effectiveDisplaySpace;
    }

    protected MifDoc.Frame makeAnchoredFrame(MifDoc.T_keyword frameType, long width, long height,
                                             MifDoc.T_keyword anchorAlign = default)
    {
        mifDoc.aFrames().Add(new MifDoc.Frame());
        MifDoc.Frame frame = mifDoc.aFrames()[mifDoc.aFrames().Count - 1];

        frame.setFrameType(frameType);
        frame.setAnchorAlign(anchorAlign);
        frame.setShapeRect(new MifDoc.T_LTWH(0, 0, width, height));

        return frame;
    }

    protected enum TComponent { cName, cUp, cRoot, cRootDrive }

    protected void addComponent(ref string target, TComponent cType, StringC component)
    {
        target += '<';
        target += cType == TComponent.cName ? 'c' : (cType == TComponent.cUp ? 'u' : 'r');
        target += '\\';
        target += '>';
        if (cType == TComponent.cName || cType == TComponent.cRootDrive)
            for (nuint i = 0; i < component.size(); i++)
                target += (char)component[i];
    }

    protected bool systemIdToMifPathname(StringC systemId, ref MifDoc.T_pathname mifPathname)
    {
        StringC filename = new StringC();
        StringC component = new StringC();
        int result;

        if ((result = systemIdFilename(systemId, ref filename)) < 0)
        {
            App.message(MifMessages.systemIdNotFilename, new StringMessageArg(systemId));
            return false;
        }
        else
        {
            string pathStr = "";
            bool firstComponent = true;
            nuint i = 0;
            do
            {
                component.resize(0);
                while (i < filename.size() && filename[i] != '\\' && filename[i] != '/')
                    component.operatorPlusAssign(filename[i++]);
                switch ((int)component.size())
                {
                    case 2:
                        if (firstComponent && component[1] == ':')
                            addComponent(ref pathStr, TComponent.cRootDrive, component);
                        else if (component[0] == '.' && component[1] == '.')
                            addComponent(ref pathStr, TComponent.cUp, component);
                        else
                            goto add_component;
                        break;
                    case 1:
                        if (component[0] != '.')
                            goto add_component;
                        break;
                    case 0:
                        if (firstComponent && filename.size() > 0)
                            addComponent(ref pathStr, TComponent.cRoot, component);
                        break;
                    default:
                    add_component:
                        addComponent(ref pathStr, TComponent.cName, component);
                        break;
                }
                firstComponent = false;
                i++;
            } while (i < filename.size());
            mifPathname = new MifDoc.T_pathname(pathStr);
        }

        return result == 0 ? false : true;
    }

    protected int systemIdFilename(StringC systemId, ref StringC filename)
    {
        if (systemId.size() == 0)
            return -1;

        // Simplified implementation: treat systemId as a file path directly
        // The C++ version uses EntityManager to resolve and validate the path
        // For now, just copy the systemId to filename
        filename.resize(0);
        for (nuint i = 0; i < systemId.size(); i++)
            filename.operatorPlusAssign(systemId[i]);

        // Return 1 to indicate success (file path extracted)
        // Return 0 if file doesn't exist, -1 for error
        return 1;
    }

    // Fields
    public CmdLineApp App;
    public CharsetInfo SystemCharset;
    public Ptr<ExtendEntityManager> EntityManager;
    public bool paragraphBreakInEffect;
    public bool inLeader;
    public bool lastFlowObjectWasWhitespace;
    public bool firstHeaderFooter;
    public bool inSimplePageSequence;
    public bool bookComponentOpened;
    public bool bookComponentAvailable;

    protected TFotSimplePageSequence FotSimplePageSequence;
    protected Table CurTable = new();

    protected MifDoc mifDoc;
    protected OutputByteStream? outputStream;
    protected MifTmpOutputByteStream? CurLeaderStream;

    protected LinkedList<DisplayInfo> displayStack = new();
    protected System.Collections.Generic.List<Format> formatStack = new();
    protected Queue<DisplaySpaceInfo> displaySpaceQueue = new();
    protected System.Collections.Generic.List<NodeInfo> nodeStack = new();
    protected System.Collections.Generic.List<LinkInfo> linkStack = new();
    protected System.Collections.Generic.List<MifDoc.Marker> indexEntryStack = new();

    protected Format nextFormat = new();
    protected Symbol pendingBreak;
    protected EffectiveDisplaySpace pendingEffectiveDisplaySpace;

    protected Dictionary<Char, uint> CharTable;

    // Static data: Frame charset map
    public static readonly Char[] FrameCharsetMap = new Char[128]
    {
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
        0x0000, 0x00c1, 0x00a2, 0x00a3, 0x00db, 0x00b4, 0x007c, 0x00a4,
        0x00ac, 0x00a9, 0x00bb, 0x00c7, 0x00c2, 0x002d, 0x00a8, 0x00f8,
        0x00fb, 0x0000, 0x0000, 0x0000, 0x00ab, 0x0000, 0x00a6, 0x00a5,
        0x00fc, 0x0000, 0x00bc, 0x00c8, 0x0000, 0x0000, 0x0000, 0x00c0,
        0x00cb, 0x00e7, 0x00e5, 0x00cc, 0x0080, 0x0081, 0x00ae, 0x0082,
        0x00e9, 0x0083, 0x00e6, 0x00e9, 0x00ed, 0x00ea, 0x00eb, 0x00ec,
        0x0000, 0x0084, 0x00f1, 0x00ee, 0x00ef, 0x00cd, 0x0085, 0x0000,
        0x00af, 0x00f4, 0x00f2, 0x00f3, 0x0086, 0x0000, 0x0000, 0x00a7,
        0x0088, 0x0087, 0x0089, 0x008b, 0x008a, 0x008c, 0x00be, 0x008d,
        0x008f, 0x008e, 0x0090, 0x0091, 0x0093, 0x0092, 0x0094, 0x0095,
        0x0000, 0x0096, 0x0098, 0x0097, 0x0099, 0x009b, 0x009a, 0x0000,
        0x00bf, 0x009d, 0x009c, 0x009e, 0x009f, 0x0000, 0x0000, 0x00d8
    };

    // Static data: Symbol fonts
    public static readonly SymbolFont[] SymbolFonts = new SymbolFont[nSymbolFonts]
    {
        new SymbolFont
        {
            name = "Symbol",
            mapping = new Char[256]
            {
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0020, 0x0021, 0x2200, 0x0023, 0x2203, 0x0025, 0x0026, 0x220B,
                0x0028, 0x0029, 0x2217, 0x002B, 0x002C, 0x2212, 0x002E, 0x002F,
                0x0030, 0x0031, 0x0032, 0x0033, 0x0034, 0x0035, 0x0036, 0x0037,
                0x0038, 0x0039, 0x003A, 0x003B, 0x003C, 0x003D, 0x003E, 0x003F,
                0x2245, 0x0391, 0x0392, 0x03A7, 0x2206, 0x0395, 0x03A6, 0x0393,
                0x0397, 0x0399, 0x03D1, 0x039A, 0x039B, 0x039C, 0x039D, 0x039F,
                0x03A0, 0x0398, 0x03A1, 0x03A3, 0x03A4, 0x03A5, 0x03C2, 0x2126,
                0x039E, 0x03A8, 0x0396, 0x005B, 0x2234, 0x005D, 0x22A5, 0x005F,
                0x203E, 0x03B1, 0x03B2, 0x03C7, 0x03B4, 0x03B5, 0x03C6, 0x03B3,
                0x03B7, 0x03B9, 0x03D5, 0x03BA, 0x03BB, 0x03BC, 0x03BD, 0x03BF,
                0x03C0, 0x03B8, 0x03C1, 0x03C3, 0x03C4, 0x03C5, 0x03D6, 0x03C9,
                0x03BE, 0x03C8, 0x03B6, 0x007B, 0x007C, 0x007D, 0x223C, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x03D2, 0x2032, 0x2264, 0x2215, 0x221E, 0x0192, 0x2663,
                0x2666, 0x2665, 0x2660, 0x2194, 0x2190, 0x2191, 0x2192, 0x2193,
                0x00B0, 0x00B1, 0x2033, 0x2265, 0x00D7, 0x221D, 0x2202, 0x2022,
                0x00F7, 0x2260, 0x2261, 0x2248, 0x2026, 0x0000, 0x0000, 0x21B5,
                0x2135, 0x2111, 0x211C, 0x2118, 0x2297, 0x2295, 0x2205, 0x2229,
                0x222A, 0x2283, 0x2287, 0x2284, 0x2282, 0x2286, 0x2208, 0x2209,
                0x2220, 0x2207, 0x00AE, 0x00A9, 0x2122, 0x220F, 0x221A, 0x22C5,
                0x00AC, 0x2227, 0x2228, 0x21D4, 0x21D0, 0x21D1, 0x21D2, 0x21D3,
                0x25CA, 0x2329, 0x00AE, 0x00A9, 0x2122, 0x2211, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x232A, 0x222B, 0x2320, 0x0000, 0x2321, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000
            }
        },
        new SymbolFont
        {
            name = "Wingdings",
            mapping = new Char[256]
            {
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x2702, 0x2701, 0x0000, 0x0000, 0x0000, 0x0000,
                0x260e, 0x2706, 0x2709, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x2328,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x2707, 0x270d,
                0x0000, 0x270c, 0x0000, 0x0000, 0x0000, 0x261c, 0x261e, 0x261d,
                0x261f, 0x0000, 0x263a, 0x0000, 0x2639, 0x0000, 0x2620, 0x0000,
                0x0000, 0x2708, 0x263c, 0x0000, 0x2744, 0x0000, 0x271e, 0x0000,
                0x2720, 0x2721, 0x262a, 0x262f, 0x0950, 0x2638, 0x2648, 0x2649,
                0x264a, 0x264b, 0x264c, 0x264d, 0x264e, 0x264f, 0x2650, 0x2651,
                0x2652, 0x2653, 0x0000, 0x0000, 0x25cf, 0x274d, 0x25a0, 0x25a1,
                0x0000, 0x2751, 0x2752, 0x0000, 0x0000, 0x25c6, 0x2756, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x2780, 0x2781, 0x2782, 0x2783, 0x2784, 0x2785, 0x2786,
                0x2787, 0x2788, 0x2789, 0x0000, 0x278a, 0x278b, 0x278c, 0x278d,
                0x278e, 0x278f, 0x2790, 0x2791, 0x2792, 0x2793, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x25cb, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x25aa,
                0x0000, 0x0000, 0x2726, 0x2605, 0x2736, 0x0000, 0x2739, 0x0000,
                0x0000, 0x0000, 0x2727, 0x0000, 0x0000, 0x272a, 0x2730, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x232b, 0x2326, 0x0000,
                0x27a2, 0x0000, 0x0000, 0x0000, 0x27b2, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x25ab, 0x2718, 0x2714, 0x2612, 0x2611, 0x0000
            }
        },
        new SymbolFont
        {
            name = "ZapfDingbats",
            mapping = new Char[256]
            {
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x2701, 0x2702, 0x2703, 0x2704, 0x260E, 0x2706, 0x2707,
                0x2708, 0x2709, 0x261B, 0x261E, 0x270C, 0x270D, 0x270E, 0x270F,
                0x2710, 0x2711, 0x2712, 0x2713, 0x2714, 0x2715, 0x2716, 0x2717,
                0x2718, 0x2719, 0x271A, 0x271B, 0x271C, 0x271D, 0x271E, 0x271F,
                0x2720, 0x2721, 0x2722, 0x2723, 0x2724, 0x2725, 0x2726, 0x2727,
                0x2605, 0x2729, 0x272A, 0x272B, 0x272C, 0x272D, 0x272E, 0x272F,
                0x2730, 0x2731, 0x2732, 0x2733, 0x2734, 0x2735, 0x2736, 0x2737,
                0x2738, 0x2739, 0x273A, 0x273B, 0x273C, 0x273D, 0x273E, 0x273F,
                0x2740, 0x2741, 0x2742, 0x2743, 0x2744, 0x2745, 0x2746, 0x2747,
                0x2748, 0x2749, 0x274A, 0x274B, 0x0000, 0x274D, 0x25A0, 0x274F,
                0x2750, 0x2751, 0x2752, 0x25B2, 0x25BC, 0x25C6, 0x2756, 0x0000,
                0x2758, 0x2759, 0x275A, 0x275B, 0x275C, 0x275D, 0x275E, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
                0x0000, 0x2761, 0x2762, 0x2763, 0x2764, 0x2765, 0x2766, 0x2767,
                0x2663, 0x2666, 0x2665, 0x2660, 0x2460, 0x2461, 0x2462, 0x2463,
                0x2464, 0x2465, 0x2466, 0x2467, 0x2468, 0x2469, 0x2776, 0x2777,
                0x2778, 0x2779, 0x277A, 0x277B, 0x277C, 0x277D, 0x277E, 0x277F,
                0x2780, 0x2781, 0x2782, 0x2783, 0x2784, 0x2785, 0x2786, 0x2787,
                0x2788, 0x2789, 0x278A, 0x278B, 0x278C, 0x278D, 0x278E, 0x278F,
                0x2790, 0x2791, 0x2792, 0x2793, 0x2794, 0x2192, 0x2194, 0x2195,
                0x2798, 0x2799, 0x279A, 0x279B, 0x279C, 0x279D, 0x279E, 0x279F,
                0x27A0, 0x27A1, 0x27A2, 0x27A3, 0x27A4, 0x27A5, 0x27A6, 0x27A7,
                0x27A8, 0x27A9, 0x27AA, 0x27AB, 0x27AC, 0x27AD, 0x27AE, 0x27AF,
                0x0000, 0x27B1, 0x27B2, 0x27B3, 0x27B4, 0x27B5, 0x27B6, 0x27B7,
                0x27B8, 0x27B9, 0x27BA, 0x27BB, 0x27BC, 0x27BD, 0x27BE, 0x0000
            }
        }
    };
}
