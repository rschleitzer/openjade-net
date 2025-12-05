// Copyright (c) 1996 James Clark
// See the file copying.txt for copying permission.

namespace OpenJade.Style;

// A garbage collector interface
// In C#, we rely on the built-in GC, but maintain the interface for compatibility

public class Collector
{
    // Object base class for collected objects
    public class Object
    {
        private bool readOnly_ = false;
        private bool permanent_ = false;
        protected char hasSubObjects_ = (char)0;

        public bool readOnly() { return readOnly_; }
        public bool permanent() { return permanent_; }

        protected Object() { }

        protected Object(Object obj)
        {
            hasSubObjects_ = obj.hasSubObjects_;
        }

        // Override in subclasses to trace sub-objects
        public virtual void traceSubObjects(Collector c) { }

        public bool hasSubObjects() { return hasSubObjects_ != 0; }

        internal void setReadOnly(bool value) { readOnly_ = value; }
        internal void setPermanent(bool value) { permanent_ = value; }

        public void makePermanent()
        {
            permanent_ = true;
        }

        // Set hasSubObjects flag
        public void setHasSubObjects(bool value)
        {
            hasSubObjects_ = value ? (char)1 : (char)0;
        }
    }

    // Dynamic root for rooting objects during GC
    public class DynamicRoot : IDisposable
    {
        private Collector collector_;

        public DynamicRoot(Collector c)
        {
            collector_ = c;
        }

        public virtual void trace(Collector c) { }

        public void Dispose()
        {
            // C# GC handles cleanup
        }
    }

    // Object dynamic root - roots a single object
    public class ObjectDynamicRoot : DynamicRoot
    {
        private Object? obj_;

        public ObjectDynamicRoot(Collector c, Object? obj = null) : base(c)
        {
            obj_ = obj;
        }

        public void assign(Object? obj)
        {
            obj_ = obj;
        }

        public override void trace(Collector c)
        {
            if (obj_ != null)
                c.trace(obj_);
        }

        public static implicit operator Object?(ObjectDynamicRoot root)
        {
            return root.obj_;
        }
    }

    private nuint maxSize_;
    private ulong totalObjects_ = 0;

    public Collector(nuint maxSize)
    {
        maxSize_ = maxSize;
    }

    // Allocate an object - in C#, just let CLR handle allocation
    public T allocateObject<T>(bool hasFinalizer) where T : Object, new()
    {
        totalObjects_++;
        return new T();
    }

    // Trace an object during collection
    public void trace(Object? obj)
    {
        // In C#, GC handles reachability automatically
        // This is a no-op but maintains interface compatibility
    }

    // Make an object permanent (will not be collected)
    public void makePermanent(Object? obj)
    {
        if (obj != null)
            obj.setPermanent(true);
    }

    // Collect garbage - returns count of live objects
    public ulong collect()
    {
        // Trigger GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        return totalObjects_;
    }

    // Make an object read-only
    public void makeReadOnly(Object? obj)
    {
        if (obj != null)
            obj.setReadOnly(true);
    }

    // Check if object might be live
    public bool objectMaybeLive(Object? obj)
    {
        // In C#, if we have a reference, it's live
        return obj != null;
    }

    // Override in subclasses to trace static roots
    protected virtual void traceStaticRoots() { }
}
