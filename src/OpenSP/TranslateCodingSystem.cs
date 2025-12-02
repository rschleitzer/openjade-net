// Copyright (c) 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

class TranslateDecoder : Decoder
{
    private Decoder decoder_;
    private ConstPtr<CharMapResource<Char>> map_;

    // TranslateDecoder(Decoder *, const ConstPtr<CharMapResource<Char> > &);
    public TranslateDecoder(Decoder decoder, ConstPtr<CharMapResource<Char>> map)
        : base(decoder.minBytesPerChar())
    {
        decoder_ = decoder;
        map_ = map;
    }

    // size_t decode(Char *, const char *, size_t, const char **);
    public override nuint decode(Char[] result, byte[] input, nuint inputLen, out nuint inputUsed)
    {
        nuint n = decoder_.decode(result, input, inputLen, out inputUsed);
        CharMapResource<Char>? map = map_.pointer();
        if (map != null)
        {
            for (nuint i = 0; i < n; i++)
                result[i] = map[result[i]];
        }
        return n;
    }

    // Boolean convertOffset(unsigned long &offset) const;
    public override Boolean convertOffset(ref ulong offset)
    {
        return decoder_.convertOffset(ref offset);
    }
}

class TranslateEncoder : RecoveringEncoder
{
    private Encoder encoder_;
    private ConstPtr<CharMapResource<Char>> map_;
    private Char illegalChar_;
    private const int bufSize = 256;
    private Char[] buf_ = new Char[bufSize];

    // TranslateEncoder(Encoder *, const ConstPtr<CharMapResource<Char> > &map, Char illegalChar);
    public TranslateEncoder(Encoder encoder, ConstPtr<CharMapResource<Char>> map, Char illegalChar)
    {
        encoder_ = encoder;
        map_ = map;
        illegalChar_ = illegalChar;
    }

    // void startFile(OutputByteStream *);
    public override void startFile(OutputByteStream sbuf)
    {
        encoder_.startFile(sbuf);
    }

    // void output(const Char *, size_t, OutputByteStream *);
    public override void output(Char[] s, nuint n, OutputByteStream sbuf)
    {
        CharMapResource<Char>? map = map_.pointer();
        if (map == null)
        {
            encoder_.output(s, n, sbuf);
            return;
        }

        nuint sIndex = 0;
        nuint j = 0;
        for (; n > 0; sIndex++, n--)
        {
            Char c = map[s[sIndex]];
            if (c == illegalChar_)
            {
                if (j > 0)
                {
                    encoder_.outputMutable(buf_, j, sbuf);
                    j = 0;
                }
                handleUnencodable(s[sIndex], sbuf);
            }
            else
            {
                if (j >= bufSize)
                {
                    encoder_.outputMutable(buf_, j, sbuf);
                    j = 0;
                }
                buf_[j++] = c;
            }
        }
        if (j > 0)
            encoder_.outputMutable(buf_, j, sbuf);
    }

    // void output(Char *, size_t, OutputByteStream *);
    public override void outputMutable(Char[] s, nuint n, OutputByteStream sbuf)
    {
        CharMapResource<Char>? map = map_.pointer();
        if (map == null)
        {
            encoder_.outputMutable(s, n, sbuf);
            return;
        }

        nuint sIndex = 0;
        nuint i = 0;
        for (;;)
        {
            if (i == n)
            {
                if (n > 0)
                    encoder_.outputMutable(s, n, sbuf);
                break;
            }
            Char c = map[s[sIndex + i]];
            if (c == illegalChar_)
            {
                if (i > 0)
                {
                    // Create a sub-array for output
                    Char[] sub = new Char[i];
                    for (nuint k = 0; k < i; k++)
                        sub[k] = s[sIndex + k];
                    encoder_.outputMutable(sub, i, sbuf);
                }
                handleUnencodable(s[sIndex + i], sbuf);
                i++;
                sIndex += i;
                n -= i;
                i = 0;
            }
            else
            {
                s[sIndex + i] = c;
                i++;
            }
        }
    }
}

public class TranslateCodingSystem : CodingSystem
{
    public struct Desc
    {
        public CharsetRegistry.ISORegistrationNumber number;
        // How much to add to the values in the base set.
        public Char add;
    }

    private ConstPtr<CharMapResource<Char>> decodeMap_ = new ConstPtr<CharMapResource<Char>>();
    private ConstPtr<CharMapResource<Char>> encodeMap_ = new ConstPtr<CharMapResource<Char>>();
    private CodingSystem sub_;
    private Desc[] desc_;
    private CharsetInfo charset_;
    private Char illegalChar_;
    private Char replacementChar_;

    // TranslateCodingSystem(const CodingSystem *codingSystem,
    //                       const Desc *desc,
    //                       const CharsetInfo *charset,
    //                       Char illegalChar,
    //                       Char replacementChar);
    public TranslateCodingSystem(CodingSystem codingSystem,
                                  Desc[] desc,
                                  CharsetInfo charset,
                                  Char illegalChar,
                                  Char replacementChar)
    {
        sub_ = codingSystem;
        desc_ = desc;
        charset_ = charset;
        illegalChar_ = illegalChar;
        replacementChar_ = replacementChar;
    }

    // Decoder *makeDecoder() const;
    public override Decoder makeDecoder()
    {
        if (decodeMap_.isNull())
        {
            CharMapResource<Char> map = new CharMapResource<Char>(replacementChar_);
            decodeMap_ = new ConstPtr<CharMapResource<Char>>(map);
            for (int dIdx = 0; dIdx < desc_.Length && desc_[dIdx].number != CharsetRegistry.ISORegistrationNumber.UNREGISTERED; dIdx++)
            {
                Desc d = desc_[dIdx];
                CharsetRegistry.Iter? iter = CharsetRegistry.makeIter(d.number);
                if (iter != null)
                {
                    WideChar min;
                    WideChar max;
                    UnivChar univ;
                    while (iter.next(out min, out max, out univ))
                    {
                        do
                        {
                            ISet<WideChar> set = new ISet<WideChar>();
                            WideChar sysChar;
                            WideChar count;
                            uint n = charset_.univToDesc(univ, out sysChar, set, out count);
                            if (count > (max - min) + 1)
                                count = (max - min) + 1;
                            if (n != 0)
                            {
                                for (WideChar i = 0; i < count; i++)
                                    map.setChar(min + d.add + i, sysChar + i);
                            }
                            min += count - 1;
                            univ += count;
                        } while (min++ != max);
                    }
                }
            }
        }
        return new TranslateDecoder(sub_.makeDecoder(), decodeMap_);
    }

    // Encoder *makeEncoder() const;
    public override Encoder makeEncoder()
    {
        if (encodeMap_.isNull())
        {
            CharMapResource<Char> map = new CharMapResource<Char>(illegalChar_);
            encodeMap_ = new ConstPtr<CharMapResource<Char>>(map);
            for (int dIdx = 0; dIdx < desc_.Length && desc_[dIdx].number != CharsetRegistry.ISORegistrationNumber.UNREGISTERED; dIdx++)
            {
                Desc d = desc_[dIdx];
                CharsetRegistry.Iter? iter = CharsetRegistry.makeIter(d.number);
                if (iter != null)
                {
                    WideChar min;
                    WideChar max;
                    UnivChar univ;
                    while (iter.next(out min, out max, out univ))
                    {
                        do
                        {
                            ISet<WideChar> set = new ISet<WideChar>();
                            WideChar sysChar;
                            WideChar count;
                            uint n = charset_.univToDesc(univ, out sysChar, set, out count);
                            if (count > (max - min) + 1)
                                count = (max - min) + 1;
                            if (n != 0)
                            {
                                for (WideChar i = 0; i < count; i++)
                                    map.setChar(sysChar + i, min + d.add + i);
                            }
                            min += count - 1;
                            univ += count;
                        } while (min++ != max);
                    }
                }
            }
        }
        return new TranslateEncoder(sub_.makeEncoder(), encodeMap_, illegalChar_);
    }

    // unsigned fixedBytesPerChar() const;
    public override uint fixedBytesPerChar()
    {
        return sub_.fixedBytesPerChar();
    }
}
