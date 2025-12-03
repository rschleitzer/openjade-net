// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

// SdFormalError - stores formal errors from SGML declaration parsing
// These are errors that should only be reported if FORMAL YES is specified
public class SdFormalError : Link
{
    private MessageType1? message_;
    private Location location_;
    private StringC id_;

    // SdFormalError(const Location &, const MessageType1 &, const StringC &);
    public SdFormalError(Location location, MessageType1 message, StringC id)
    {
        location_ = new Location(location);
        message_ = message;
        id_ = new StringC(id);
    }

    // void send(ParserState &);
    public void send(ParserState parser)
    {
        if (message_ != null)
        {
            parser.setNextLocation(location_);
            parser.message(message_, new StringMessageArg(id_));
        }
    }
}
