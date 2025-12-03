// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// SdBuilder - Information about the SGML declaration being built
public class SdBuilder
{
    public Ptr<Sd> sd = new Ptr<Sd>();
    public Ptr<Syntax> syntax = new Ptr<Syntax>();
    public CharsetDecl syntaxCharsetDecl = new CharsetDecl();
    public CharsetInfo syntaxCharset = new CharsetInfo();
    public CharSwitcher switcher = new CharSwitcher();
    public Boolean externalSyntax;
    public Boolean enr;
    public Boolean www;
    public Boolean valid;
    public Boolean external;
    public IList<SdFormalError> formalErrorList = new IList<SdFormalError>();

    // SdBuilder();
    public SdBuilder()
    {
        externalSyntax = false;
        enr = false;
        www = false;
        valid = true;
        external = false;
    }

    // void addFormalError(const Location &, const MessageType1 &, const StringC &);
    public void addFormalError(Location location, MessageType1 message, StringC id)
    {
        formalErrorList.insert(new SdFormalError(location, message, id));
    }
}
