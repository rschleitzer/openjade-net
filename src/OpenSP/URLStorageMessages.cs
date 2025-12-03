// Copyright (c) 1995 James Clark
// See the file COPYING for copying permission.

namespace OpenSP;

public static class URLStorageMessages
{
    // 2300
    public static readonly MessageType1 emptyHost = new MessageType1(
        MessageType.Severity.error, null, 2300, "empty host in HTTP URL %1");

    // 2301
    public static readonly MessageType1 badRelative = new MessageType1(
        MessageType.Severity.error, null, 2301, "uncompletable relative HTTP URL %1");

    // 2302
    public static readonly MessageType1 emptyPort = new MessageType1(
        MessageType.Severity.error, null, 2302, "empty port number in HTTP URL %1");

    // 2303
    public static readonly MessageType1 invalidPort = new MessageType1(
        MessageType.Severity.error, null, 2303, "invalid port number in HTTP URL %1");

    // 2304
    public static readonly MessageType1 hostNotFound = new MessageType1(
        MessageType.Severity.error, null, 2304, "host %1 not found");

    // 2305
    public static readonly MessageType1 hostTryAgain = new MessageType1(
        MessageType.Severity.error, null, 2305, "could not resolve host %1 (try again later)");

    // 2306
    public static readonly MessageType1 hostNoRecovery = new MessageType1(
        MessageType.Severity.error, null, 2306, "could not resolve host %1 (unrecoverable error)");

    // 2307
    public static readonly MessageType1 hostNoData = new MessageType1(
        MessageType.Severity.error, null, 2307, "no address record for host name %1");

    // 2308
    public static readonly MessageType2 hostOtherError = new MessageType2(
        MessageType.Severity.error, null, 2308, "could not resolve host %1 (%2)");

    // 2309
    public static readonly MessageType1 hostUnknownError = new MessageType1(
        MessageType.Severity.error, null, 2309, "could not resolve host %1 (unknown error)");

    // 2310
    public static readonly MessageType1 cannotCreateSocket = new MessageType1(
        MessageType.Severity.error, null, 2310, "cannot create socket (%1)");

    // 2311
    public static readonly MessageType2 cannotConnect = new MessageType2(
        MessageType.Severity.error, null, 2311, "error connecting to %1 (%2)");

    // 2312
    public static readonly MessageType2 writeError = new MessageType2(
        MessageType.Severity.error, null, 2312, "error sending request to %1 (%2)");

    // 2313
    public static readonly MessageType2 readError = new MessageType2(
        MessageType.Severity.error, null, 2313, "error receiving from host %1 (%2)");

    // 2314
    public static readonly MessageType2 closeError = new MessageType2(
        MessageType.Severity.error, null, 2314, "error closing connection to host %1 (%2)");

    // 2315
    public static readonly MessageType1 invalidHostNumber = new MessageType1(
        MessageType.Severity.error, null, 2315, "invalid host number %1");

    // 2316
    public static readonly MessageType3 getFailed = new MessageType3(
        MessageType.Severity.error, null, 2316, "could not get %2 from %1 (reason given was %3)");

    // 2317
    public static readonly MessageType0 notSupported = new MessageType0(
        MessageType.Severity.error, null, 2317, "URL not supported by this version");

    // 2318
    public static readonly MessageType0 onlyHTTP = new MessageType0(
        MessageType.Severity.error, null, 2318, "only HTTP scheme supported");

    // 2319
    public static readonly MessageType1 winsockInitialize = new MessageType1(
        MessageType.Severity.error, null, 2319, "could not initialize Windows Sockets (%1)");

    // 2320
    public static readonly MessageType0 winsockVersion = new MessageType0(
        MessageType.Severity.error, null, 2320, "incompatible Windows Sockets version");

    // 2321
    public static readonly MessageFragment winsockErrorNumber = new MessageFragment(
        null, 2321, "error number ");

    // 2322
    public static readonly MessageType1 Redirect = new MessageType1(
        MessageType.Severity.warning, null, 2322, "URL Redirected to %1");
}
