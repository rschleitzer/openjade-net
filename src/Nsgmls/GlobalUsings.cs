// Copyright (c) 1994 James Clark
// See the file COPYING for copying permission.

// Type aliases preserved from C++ typedefs (types.h, Boolean.h)

// Fundamental integer types
global using Unsigned32 = System.UInt32;
global using Signed32 = System.Int32;

// Numeric types (Number holds values between 0 and 99999999)
global using Number = System.UInt32;
global using Offset = System.UInt32;
global using Index = System.UInt32;

// Character types (32-bit for SP_MULTI_BYTE)
global using Char = System.UInt32;
global using Xchar = System.Int32;  // Holds Char values plus -1 for EOF
global using UnivChar = System.UInt32;
global using WideChar = System.UInt32;
global using SyntaxChar = System.UInt32;

// Other types
global using CharClassIndex = System.UInt16;
global using Token = System.UInt32;
global using EquivCode = System.UInt16;

// Boolean types
global using Boolean = System.Boolean;
global using PackedBoolean = System.Boolean;

// Use OpenSP.StringC from the library directly
