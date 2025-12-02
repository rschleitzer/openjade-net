// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public class SrInfo
{
    public String<EquivCode> chars = new String<EquivCode>();
    public int bSequenceLength;
    public String<EquivCode> chars2 = new String<EquivCode>();

    public SrInfo()
    {
        bSequenceLength = 0;
    }
}
