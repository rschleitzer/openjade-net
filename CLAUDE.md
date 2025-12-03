# OpenJade-NET Porter Development Instructions

## Mission
Create a Python tool that performs a 1:1 port of OpenSP and OpenJade from C++ to C#, preserving all names, algorithms, and logic exactly as in the original.

## Project Structure
```
openjade-net/
  src/
    OpenSP/
      *.cs             # One .cs per .h/.cxx pair
      Nsgmls/          # Tool subdirectory
    OpenJade/
      Jade/            # Keep original structure
      Dsssl/           # Keep original structure
      Grove/           # Keep original structure
      Style/           # Keep original structure
      SPGrove/         # Keep original structure
  upstream/            # Git submodules
    opensp/            # Original C++ source
    openjade/          # Original C++ source
```

## Core Requirements

### 0. Stub Methods
When stubbing out methods with TODO comments, always throw `NotImplementedException` instead of returning default values:
```csharp
// WRONG:
protected virtual Boolean parseComment(Mode mode) { return false; }
protected virtual void doInit() { /* TODO */ }

// CORRECT:
protected virtual Boolean parseComment(Mode mode) { throw new NotImplementedException(); }
protected virtual void doInit() { throw new NotImplementedException(); }
```

### 1. Name Preservation
- ALL class names, method names, variable names must be EXACTLY preserved
- `FOTBuilder` stays `FOTBuilder`, not `FlowObjectTreeBuilder`
- `StringC` stays `StringC`, not `StringClass`
- Even "bad" names like `sp_` prefixes must be kept
- **Keep original casing**: `size()` stays `size()`, NOT `Size()` - ignore .NET PascalCase conventions
- Method names like `assign`, `append`, `resize`, `swap` stay lowercase

### 2. File Merging Strategy
For OpenSP:
- `include/StringC.h` + `lib/StringC.cxx` → `OpenSP/StringC.cs`
- `include/Parser.h` + `lib/parser.cxx` → `OpenSP/Parser.cs`
- Remove include/lib separation, everything goes to src/

For OpenJade:
- Keep existing directory structure (already well organized)
- `jade/TeXFOTBuilder.h` + `jade/TeXFOTBuilder.cxx` → `Jade/TeXFOTBuilder.cs`

### 3. Type Mappings

#### Global Using Aliases (GlobalUsings.cs)
OpenSP typedefs are preserved using C# 10 global using aliases in `src/OpenSP/GlobalUsings.cs`:
```csharp
global using Unsigned32 = System.UInt32;
global using Signed32 = System.Int32;
global using Number = System.UInt32;
global using Offset = System.UInt32;
global using Index = System.UInt32;
global using Char = System.UInt32;      // 32-bit codepoint
global using Xchar = System.Int32;      // Char + EOF (-1)
global using UnivChar = System.UInt32;
global using WideChar = System.UInt32;
global using SyntaxChar = System.UInt32;
global using CharClassIndex = System.UInt16;
global using Token = System.UInt32;
global using EquivCode = System.UInt16;
global using Boolean = System.Boolean;
global using PackedBoolean = System.Boolean;
```

**IMPORTANT**: Always use these typedef names (Char, Number, Offset, etc.) in ported code, NOT the underlying C# types. This preserves the original code's readability and intent.

#### Fundamental Types (for types not covered by global usings)
```python
TYPE_MAPPINGS = {
    'char': 'sbyte',
    'unsigned char': 'byte',
    'short': 'short',
    'unsigned short': 'ushort',
    'int': 'int',
    'unsigned int': 'uint',
    'long': 'long',
    'unsigned long': 'ulong',
    'size_t': 'nuint',
    'void': 'void',
    'const char*': 'string',
    'float': 'float',
    'double': 'double'
}
```

#### String Handling
```csharp
// C++: const StringC &
// C#:  StringC str

// C++: StringC *
// C#:  StringC? str

// StringC class must be implemented as:
public class StringC {
    private uint[] chars;  // Preserve 32-bit semantics
    // ... methods
}
```

#### Template Mappings
```python
TEMPLATE_MAPPINGS = {
    'Vector<{T}>': 'List<{T}>',
    'IList<{T}>': 'List<{T}>',
    'IListIter<{T}>': 'IEnumerator<{T}>',
    'Ptr<{T}>': '{T}?',
    'ConstPtr<{T}>': '{T}?',
    'Owner<{T}>': '{T}',  # Just remove ownership semantics
    'CopyOwner<{T}>': '{T}',
    'HashTable<{K},{V}>': 'Dictionary<{K},{V}>',
    'HashTableIter<{K},{V}>': 'IEnumerator<KeyValuePair<{K},{V}>>'
}
```

### 4. Syntax Transformations

#### Classes
```cpp
// C++
class SP_API StringC {
public:
    StringC();
    ~StringC();
    size_t size() const;
private:
    Char *ptr_;
    size_t size_;
};

// C#
public class StringC {
    private uint[] ptr_;
    private nuint size_;

    public StringC() {
        ptr_ = Array.Empty<uint>();
        size_ = 0;
    }

    public nuint Size() {
        return size_;
    }
}
```

#### Namespaces
```cpp
// C++
#ifdef SP_NAMESPACE
namespace SP_NAMESPACE {
#endif

// C# - use file-scoped namespace declaration (C# 10+)
namespace OpenSP;

// classes here (no extra indentation needed)
```

#### Const Methods
```cpp
// C++
size_t size() const { return size_; }

// C#
public nuint Size() { return size_; }
// Note: C# doesn't have const methods, but parameters can use 'in' keyword
```

#### References
```cpp
// C++
void foo(const StringC &str);
StringC &operator+=(const StringC &str);

// C#
public void Foo(StringC str);
public StringC Append(StringC str);  // Return this for chaining
```

#### Pointers
```cpp
// C++
Node *parent_;
const Node *getParent() const;

// C#
private Node? parent_;
public Node? GetParent();
```

### 5. Special Cases

#### Operator Overloading
```python
OPERATOR_MAPPINGS = {
    'operator==': 'Equals',  # Or actual operator overload
    'operator!=': 'NotEquals',
    'operator<': 'CompareTo',  # Implement IComparable
    'operator>': 'CompareTo',
    'operator<=': 'CompareTo',
    'operator>=': 'CompareTo',
    'operator[]': 'this[]',  # C# indexer
    'operator()': 'Invoke',
    'operator*': 'Deref',  # For iterators
    'operator++': 'MoveNext',  # For iterators
    'operator+=': 'Append',
    'operator=': null  # Handle in constructor/assignment
}
```

#### Multiple Inheritance
```cpp
// C++
class TeXFOTBuilder : public FOTBuilder, public OutputByteStream::Escaper {

// C# - use interfaces
public interface IEscaper {
    void Escape(uint ch);
}

public class TeXFOTBuilder : FOTBuilder, IEscaper {
```

#### Friend Classes
```cpp
// C++
friend class Parser;

// C#
// Make methods internal or use InternalsVisibleTo attribute
```

#### Unions
```cpp
// C++
union {
    int i;
    float f;
    void *p;
};

// C# - use struct with explicit layout or discriminated union pattern
[StructLayout(LayoutKind.Explicit)]
public struct UnionValue {
    [FieldOffset(0)] public int i;
    [FieldOffset(0)] public float f;
    [FieldOffset(0)] public IntPtr p;
}
```

### 6. Preprocessor Handling

#### Conditional Compilation
```cpp
#ifdef WIN32
    // Windows code
#else
    // Unix code
#endif

// C# - use runtime detection or preprocessor
#if WINDOWS
    // Windows code
#else
    // Unix code
#endif

// Or runtime:
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
    // Windows code
} else {
    // Unix code
}
```

#### Macros
```python
# Simple macros → const
#define SGML_PARSE_STATE_SIZE 20
# becomes:
public const int SGML_PARSE_STATE_SIZE = 20;

# Function macros → inline methods
#define MIN(a,b) ((a)<(b)?(a):(b))
# becomes:
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static int Min(int a, int b) => a < b ? a : b;

# Complex macros → regular methods
```

### 7. Message System
OpenSP/OpenJade use message resource files (.rc, .msg). Convert to C#:

```csharp
// From ParserMessages.msg
public enum ParserMessages {
    UnexpectedEof = 1001,
    InvalidChar = 1002,
    // ...
}

public static class MessageTexts {
    public static readonly Dictionary<ParserMessages, string> Texts = new() {
        [ParserMessages.UnexpectedEof] = "Unexpected end of file",
        [ParserMessages.InvalidChar] = "Invalid character in input",
        // ...
    };
}
```

### 8. Memory Management
IGNORE most of these - C# GC handles it:
- `delete`
- `delete[]`
- Destructors (unless they do more than free memory - use IDisposable)
- `Owner<T>`, `CopyOwner<T>` (just unwrap the type)

For resources that need cleanup, implement IDisposable:
```csharp
public class InputSource : IDisposable {
    public void Dispose() {
        // cleanup code
    }
}
```

### 9. Include/Import Generation
```python
# Analyze C++ includes and generate appropriate using statements
#include "StringC.h"
#include "types.h"

# becomes:
using OpenSP;
```

### 10. Critical Files to Start With

Port in this order for OpenSP:
1. `types.h` → `Types.cs` (fundamental type definitions)
2. `StringC.h/cxx` → `StringC.cs`
3. `Vector.h/cxx` → `Vector.cs`
4. `IList.h/cxx` → `IList.cs`
5. `Message.h/cxx` → `Message.cs`
6. `Location.h/cxx` → `Location.cs`
7. `EntityManager.h/cxx` → `EntityManager.cs`

## Testing Strategy

1. **Immediate Test**: After porting StringC
```bash
# Create test that makes StringC, does operations, prints results
# Compare with C++ version doing same operations
```

2. **ESIS Test**: After minimal parser port
```bash
original-onsgmls test.sgml > expected.esis
dotnet run --project src/Nsgmls test.sgml > actual.esis
diff expected.esis actual.esis
```

## Specific Patterns to Watch For

### Static Members
```cpp
class Foo {
    static const int maxSize = 100;
    static HashMap<StringC,int> cache;
};

// C#
public class Foo {
    public const int maxSize = 100;
    public static Dictionary<StringC, int> cache = new();
}
```

### RAII Patterns
```cpp
class InputSourceOrigin : public Origin {
public:
    ~InputSourceOrigin();  // Closing files - use IDisposable in C#
};

// C#
public class InputSourceOrigin : Origin, IDisposable {
    public void Dispose() {
        // cleanup
    }
}
```

### Character Handling
```cpp
// These are everywhere in OpenSP
Char c = 0x1234;
if (c > 127) { ... }

// C# - preserve exact semantics
uint c = 0x1234;
if (c > 127) { ... }
```

## Preservation Rules

### MUST Preserve:
- All algorithm logic (even if inefficient)
- All class/method/variable names
- Directory structure (except include/lib merge)
- Comments (especially copyright notices)
- Order of operations (some code relies on side effects)

### Can Modify:
- Pointer arithmetic → array indexing
- Manual memory management → remove (use IDisposable where needed)
- Platform #ifdefs → runtime checks or preprocessor
- Operator overloading → named methods or C# operators

### NEVER Do:
- "Improve" algorithms
- Rename for clarity
- Refactor for "better" structure
- Skip "useless" code (it might have side effects)

## Success Criteria

1. `onsgmls` produces byte-identical ESIS output
2. All original test cases pass
3. Performance within 2x of C++ version
4. Code is recognizable to anyone familiar with OpenSP/OpenJade

## Remember

This is a MECHANICAL PORT. No creativity, no improvements, no "better ways". James Clark's code has worked for 25+ years. Preserve it exactly, just translate the syntax.

When in doubt:
- Keep the original name
- Keep the original structure
- Keep the original algorithm
- Just make it valid C#
