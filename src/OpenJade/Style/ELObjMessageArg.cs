// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

using OpenSP;

// ELObjMessageArg converts an ELObj to a StringMessageArg for error messages.
// This allows ELObj values to be included in diagnostic messages.
public class ELObjMessageArg : StringMessageArg
{
    public ELObjMessageArg(ELObj obj, Interpreter interp)
        : base(convert(obj, interp))
    {
    }

    private static StringC convert(ELObj obj, Interpreter interp)
    {
        StrOutputCharStream os = new StrOutputCharStream();
        obj.print(interp, os);
        StringC result = new StringC();
        os.extractString(result);
        return result;
    }
}
