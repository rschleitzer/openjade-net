// Copyright (c) 1996, 1997 James Clark
// See the file COPYING for copying permission.

namespace OpenJade.SPGrove;

using OpenSP;
using OpenJade.Grove;
using Char = System.UInt32;
using Index = System.UInt32;
using Boolean = System.Boolean;

// FIXME location for SgmlDocument node.

public static class GroveBuilderConstants
{
    public static bool blockingAccess = true;
    public const nuint initialBlockSize = 8192;
    public const uint maxBlocksPerSize = 20;
}

public abstract class Chunk
{
    public ParentChunk? origin;

    // Set ptr to a node pointing to first Node in this.
    public abstract AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node);

    public virtual AccessResult setNodePtrFirst(ref NodePtr ptr, ElementNode? node)
    {
        return setNodePtrFirst(ref ptr, (BaseNode?)node);
    }

    public virtual AccessResult setNodePtrFirst(ref NodePtr ptr, DataNode? node)
    {
        return setNodePtrFirst(ref ptr, (BaseNode?)node);
    }

    public abstract Chunk? after();

    public virtual AccessResult getFollowing(GroveImpl grove, out Chunk? chunk, out uint nNodes)
    {
        chunk = null;
        nNodes = 0;
        var p = after();
        while (p == grove.completeLimit())
            if (!grove.waitForMoreNodes())
                return AccessResult.accessTimeout;
        if (p?.origin != origin)
            return AccessResult.accessNull;
        nNodes = 1;
        chunk = p;
        return AccessResult.accessOK;
    }

    public virtual AccessResult getFirstSibling(GroveImpl grove, out Chunk? chunk)
    {
        chunk = null;
        if (origin == grove.root())
            return AccessResult.accessNotInClass;
        chunk = origin?.after();
        return AccessResult.accessOK;
    }

    public virtual StringC? id()
    {
        return null;
    }

    public virtual Boolean getLocOrigin(out Origin? origin)
    {
        origin = null;
        return false;
    }
}

public class LocChunk : Chunk
{
    public Index locIndex;

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        // LocChunk is abstract - derived classes should implement
        return AccessResult.accessNull;
    }

    public override Chunk? after()
    {
        // LocChunk is abstract - derived classes should implement
        return null;
    }
}

public class ParentChunk : LocChunk
{
    public Chunk? nextSibling;

    public ParentChunk()
    {
        nextSibling = null;
    }
}

public class ElementChunk : ParentChunk
{
#pragma warning disable CS0649 // Field never assigned (port stub)
    internal ElementType? type;
#pragma warning restore CS0649
    public uint elementIndex;

    public virtual AttributeValue? attributeValue(nuint attIndex, GroveImpl grove)
    {
        return attDefList()?.def(attIndex)?.defaultValue(grove.impliedAttributeValue());
    }

    public virtual Boolean mustOmitEndTag()
    {
        return type?.definition()?.declaredContent() == ElementDefinition.DeclaredContent.empty;
    }

    public virtual Boolean included()
    {
        return false;
    }

    public AttributeDefinitionList? attDefList()
    {
        return type?.attributeDefTemp();
    }

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        ptr.assign(new ElementNode(node!.grove(), this));
        return AccessResult.accessOK;
    }

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, DataNode? node)
    {
        ptr.assign(new ElementNode(node!.grove(), this));
        return AccessResult.accessOK;
    }

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, ElementNode? node)
    {
        if (node!.canReuse(ptr))
            node.reuseFor(this);
        else
            ptr.assign(new ElementNode(node.grove(), this));
        return AccessResult.accessOK;
    }

    public static StringC key(ElementChunk chunk)
    {
        return chunk.id()!;
    }

    public override Chunk? after()
    {
        // Unsafe pointer arithmetic equivalent - return address after this chunk
        // In C#, we need a different approach - chunks are managed objects
        return nextSibling;
    }

    public override AccessResult getFollowing(GroveImpl grove, out Chunk? chunk, out uint nNodes)
    {
        while (nextSibling == null)
        {
            if (!grove.maybeMoreSiblings(this))
            {
                if ((Chunk?)origin == grove.root())
                {
                    chunk = null;
                    nNodes = 0;
                    return AccessResult.accessNotInClass;
                }
                else
                {
                    chunk = null;
                    nNodes = 0;
                    return AccessResult.accessNull;
                }
            }
            if (!grove.waitForMoreNodes())
            {
                chunk = null;
                nNodes = 0;
                return AccessResult.accessTimeout;
            }
        }
        chunk = nextSibling;
        nNodes = 1;
        return AccessResult.accessOK;
    }

    public ElementType? elementType()
    {
        return type;
    }
}

public class LocOriginChunk : Chunk
{
    private Origin? locOrigin;
    private Chunk? nextChunk_;

    public LocOriginChunk(Origin? lo)
    {
        locOrigin = lo;
    }

    public void setNextChunk(Chunk? next)
    {
        nextChunk_ = next;
    }

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        // Delegate to the chunk after this one
        return nextChunk_?.setNodePtrFirst(ref ptr, node) ?? AccessResult.accessNull;
    }

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, ElementNode? node)
    {
        return nextChunk_?.setNodePtrFirst(ref ptr, node) ?? AccessResult.accessNull;
    }

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, DataNode? node)
    {
        return nextChunk_?.setNodePtrFirst(ref ptr, node) ?? AccessResult.accessNull;
    }

    public override Chunk? after()
    {
        return nextChunk_;
    }

    public override AccessResult getFollowing(GroveImpl grove, out Chunk? chunk, out uint nNodes)
    {
        chunk = null;
        nNodes = 0;
        var ret = base.getFollowing(grove, out chunk, out nNodes);
        if (ret == AccessResult.accessOK)
            nNodes = 0;
        return ret;
    }

    public override Boolean getLocOrigin(out Origin? origin)
    {
        origin = locOrigin;
        return true;
    }
}

public class MessageItem
{
    private Node.Severity severity_;
    private StringC text_;
    private Location loc_;
    private MessageItem? next_;

    public MessageItem(Node.Severity severity, StringC text, Location loc)
    {
        severity_ = severity;
        text_ = text;
        loc_ = loc;
        next_ = null;
    }

    public Node.Severity severity() { return severity_; }
    public Location loc() { return loc_; }
    public StringC text() { return text_; }
    public MessageItem? next() { return next_; }
    public ref MessageItem? nextP() { return ref next_; }
}

public class GroveImpl
{
    private uint groveIndex_;
    private SgmlDocumentChunk? root_;
    private ParentChunk? origin_;
    private DataChunk? pendingData_;
    private Action<Chunk>? tailPtrSetter_;  // Function to set the tail pointer
    private ConstPtr<Dtd> dtd_ = new ConstPtr<Dtd>();
    private ConstPtr<Sd> sd_ = new ConstPtr<Sd>();
    private ConstPtr<Syntax> prologSyntax_ = new ConstPtr<Syntax>();
    private ConstPtr<Syntax> instanceSyntax_ = new ConstPtr<Syntax>();
    private ConstPtr<AttributeValue> impliedAttributeValue_ = new ConstPtr<AttributeValue>();
    private List<ConstPtr<AttributeValue>> values_ = new List<ConstPtr<AttributeValue>>();
    private List<ConstPtr<Origin>> origins_ = new List<ConstPtr<Origin>>();
    private NamedResourceTable<Entity> defaultedEntityTable_ = new NamedResourceTable<Entity>();
#pragma warning disable CS0649 // Field never assigned (port stub)
    private Boolean hasDefaultEntity_;
#pragma warning restore CS0649
    private Boolean haveAppinfo_;
    private StringC appinfo_ = new StringC();
    private Origin? currentLocOrigin_;
    private Boolean complete_;
    private object? completeLimit_;
    private object? completeLimitWithLocChunkAfter_;
    private uint refCount_;
    private ulong nEvents_;
    private ulong nElements_;
    private uint pulseStep_;
    private uint nChunksSinceLocOrigin_;
    private const uint maxChunksWithoutLocOrigin = 100;
    private MessageItem? messageList_;
    private MessageItem? messageListTail_;

    public GroveImpl(uint groveIndex)
    {
        groveIndex_ = groveIndex;
        complete_ = false;
        refCount_ = 0;
        nEvents_ = 0;
        nElements_ = 0;
        pulseStep_ = 0;
        nChunksSinceLocOrigin_ = 0;
        root_ = new SgmlDocumentChunk();
        origin_ = root_;
    }

    // Const interface
    public void addRef() { ++refCount_; }

    public void release()
    {
        if (--refCount_ == 0)
        {
            // Will be garbage collected
        }
    }

    public uint groveIndex() { return groveIndex_; }
    public SgmlDocumentChunk? root() { return root_; }

    public AttributeValue? impliedAttributeValue()
    {
        return impliedAttributeValue_.pointer();
    }

    public Boolean getAppinfo(out StringC? appinfo)
    {
        if (!haveAppinfo_)
        {
            appinfo = null;
            return false;
        }
        appinfo = appinfo_;
        return true;
    }

    public SubstTable? generalSubstTable()
    {
        return instanceSyntax_.isNull() ? null : instanceSyntax_.pointer()!.generalSubstTable();
    }

    public SubstTable? entitySubstTable()
    {
        return instanceSyntax_.isNull() ? null : instanceSyntax_.pointer()!.entitySubstTable();
    }

    public Dtd? governingDtd() { return dtd_.pointer(); }

    public Boolean complete() { return complete_; }
    public object? completeLimit() { return completeLimit_; }
    public object? completeLimitWithLocChunkAfter() { return completeLimitWithLocChunkAfter_; }
    public Origin? currentLocOrigin() { return currentLocOrigin_; }
    public Boolean hasDefaultEntity() { return hasDefaultEntity_; }

    // Element lookup by ID
    private Dictionary<StringC, ElementChunk> idTable_ = new Dictionary<StringC, ElementChunk>();

    public ElementChunk? lookupElement(StringC id)
    {
        if (idTable_.TryGetValue(id, out var element))
            return element;
        return null;
    }

    public Boolean maybeMoreSiblings(ParentChunk chunk)
    {
        return complete_
            ? chunk.nextSibling != null
            : (origin_ == chunk || maybeMoreSiblings1(chunk));
    }

    private Boolean maybeMoreSiblings1(ParentChunk chunk)
    {
        for (ParentChunk? open = origin_; open != null; open = open.origin)
            if (open == chunk)
                return true;
        return chunk.nextSibling != null;
    }

    public Boolean waitForMoreNodes()
    {
        // In blocking access mode, would wait for condition
        // For now, return false to indicate timeout (non-blocking)
        if (GroveBuilderConstants.blockingAccess)
            return false; // Would wait on condition in full implementation
        return false;
    }

    public AccessResult proxifyLocation(Location from, out Location to)
    {
        if (from.origin().isNull())
        {
            to = new Location();
            return AccessResult.accessNull;
        }
        to = new Location(new GroveImplProxyOrigin(this, from.origin().pointer()), from.index());
        return AccessResult.accessOK;
    }

    public MessageItem? messageList() { return messageList_; }

    public void getSd(out ConstPtr<Sd> sd, out ConstPtr<Syntax> prologSyntax, out ConstPtr<Syntax> instanceSyntax)
    {
        sd = sd_;
        prologSyntax = prologSyntax_;
        instanceSyntax = instanceSyntax_;
    }

    // Non-const interface
    public object allocChunk(nuint size)
    {
        // In C#, we don't need custom memory management - just track for loc origin
        nChunksSinceLocOrigin_++;
        return new object(); // Placeholder - actual chunks created separately
    }

    public void appendSibling(Chunk chunk)
    {
        if (pendingData_ != null)
        {
            // Must set completeLimit_ before setting tailPtr_
            completeLimit_ = pendingData_.after();
            if (tailPtrSetter_ != null)
            {
                tailPtrSetter_(pendingData_);
                tailPtrSetter_ = null;
            }
            pendingData_ = null;
        }
        // Must set origin before advancing completeLimit_
        chunk.origin = origin_;
        // Must advance completeLimit_ before setting tailPtr_
        completeLimit_ = chunk;
        if (tailPtrSetter_ != null)
        {
            tailPtrSetter_(chunk);
            tailPtrSetter_ = null;
        }
        maybePulse();
    }

    public void appendSibling(DataChunk chunk)
    {
        // Since we might extend this DataChunk, it's
        // not safe to set completeLimit_ to after this chunk yet.
        if (pendingData_ != null)
        {
            // Must set completeLimit_ before setting tailPtr_
            completeLimit_ = pendingData_.after();
            if (tailPtrSetter_ != null)
            {
                tailPtrSetter_(pendingData_);
                tailPtrSetter_ = null;
            }
        }
        chunk.origin = origin_;
        pendingData_ = chunk;
        maybePulse();
    }

    public DataChunk? pendingData() { return pendingData_; }

    public void push(ElementChunk chunk, Boolean hasId)
    {
        if (pendingData_ != null)
        {
            // Must set completeLimit_ before setting tailPtr_
            completeLimit_ = pendingData_.after();
            if (tailPtrSetter_ != null)
            {
                tailPtrSetter_(pendingData_);
                tailPtrSetter_ = null;
            }
            pendingData_ = null;
        }
        chunk.elementIndex = (uint)nElements_++;
        chunk.origin = origin_;
        // Must set origin_ to chunk before advancing completeLimit_
        origin_ = chunk;
        completeLimit_ = chunk;
        // Allow for the possibility of invalid documents with elements
        // after the document element.
        if ((Chunk?)chunk.origin == root_ && root_!.documentElement == null)
            root_.documentElement = chunk;
        else if (tailPtrSetter_ != null)
        {
            tailPtrSetter_(chunk);
            tailPtrSetter_ = null;
        }
        // hasId handling would add to idTable_ in full impl
        maybePulse();
    }

    public void pop()
    {
        if (pendingData_ != null)
        {
            // Must set completeLimit_ before setting tailPtr_
            completeLimit_ = pendingData_.after();
            if (tailPtrSetter_ != null)
            {
                tailPtrSetter_(pendingData_);
                tailPtrSetter_ = null;
            }
            pendingData_ = null;
        }
        tailPtrSetter_ = (c) => { origin_!.nextSibling = c; };
        origin_ = origin_!.origin;
        if ((Chunk?)origin_ == root_)
            finishDocumentElement();
        maybePulse();
    }

    private void finishDocumentElement()
    {
        // Be robust in the case of erroneous documents
        if (root_!.epilog == null)
        {
            tailPtrSetter_ = (c) => { root_!.epilog = c; };
        }
    }

    private void maybePulse()
    {
        // Pulsing condition variable for multi-threading support
        // Once we've had (2^n)*(2^10) events, only pulse every (2^n)th event.
        if ((++nEvents_ & ~(~0UL << (int)pulseStep_)) == 0)
        {
            pulse();
            if (pulseStep_ < 8 && nEvents_ > (1UL << (int)(pulseStep_ + 10)))
                pulseStep_++;
        }
    }

    private void pulse()
    {
        // Signal condition variable - no-op in single-threaded mode
    }

    public void setAppinfo(StringC appinfo)
    {
        appinfo_ = appinfo;
        haveAppinfo_ = true;
    }

    public void setDtd(ConstPtr<Dtd> dtd)
    {
        dtd_ = dtd;
    }

    public void setSd(ConstPtr<Sd> sd, ConstPtr<Syntax> prologSyntax, ConstPtr<Syntax> instanceSyntax)
    {
        sd_ = sd;
        prologSyntax_ = prologSyntax;
        instanceSyntax_ = instanceSyntax;
    }

    public void storeAttributeValue(ConstPtr<AttributeValue> value)
    {
        values_.append(value);
    }

    public void addDefaultedEntity(ConstPtr<Entity> entity)
    {
        // We need a table of ConstPtr<Entity> but we don't have one.
        if (entity.pointer() != null)
            defaultedEntityTable_.insert(new Ptr<Entity>(entity.pointer()));
    }

    public void setComplete()
    {
        complete_ = true;
    }

    public Boolean haveRootOrigin()
    {
        return (Chunk?)origin_ == root_;
    }

    public void setLocOrigin(ConstPtr<Origin> origin)
    {
        if (origin.pointer() != currentLocOrigin_
            || nChunksSinceLocOrigin_ >= maxChunksWithoutLocOrigin)
            storeLocOrigin(origin);
    }

    private void storeLocOrigin(ConstPtr<Origin> locOrigin)
    {
        var chunk = new LocOriginChunk(currentLocOrigin_);
        chunk.origin = origin_;
        completeLimitWithLocChunkAfter_ = completeLimit_;
        nChunksSinceLocOrigin_ = 0;
        if (locOrigin.pointer() == currentLocOrigin_)
            return;
        if (currentLocOrigin_ != null
            && locOrigin.pointer() == currentLocOrigin_.parent().origin().pointer())
        {
            // Don't need to store it.
            currentLocOrigin_ = locOrigin.pointer();
            return;
        }
        currentLocOrigin_ = locOrigin.pointer();
        if (locOrigin.isNull())
            return;
        origins_.append(locOrigin);
    }

    public void appendMessage(MessageItem item)
    {
        if (messageList_ == null)
        {
            messageList_ = item;
            messageListTail_ = item;
        }
        else
        {
            messageListTail_!.nextP() = item;
            messageListTail_ = item;
        }
        pulse();
    }
}

public class GroveImplPtr : IDisposable
{
    private GroveImpl grove_;

    public GroveImplPtr(GroveImpl grove)
    {
        grove_ = grove;
        grove_.addRef();
    }

    public void Dispose()
    {
        grove_.release();
    }

    public GroveImpl Grove => grove_;
}

// Proxy origin that keeps the grove alive
public class GroveImplProxyOrigin : ProxyOrigin
{
    private GroveImplPtr grove_;

    public GroveImplProxyOrigin(GroveImpl grove, Origin? origin)
        : base(origin)
    {
        grove_ = new GroveImplPtr(grove);
    }
}

public abstract class BaseNode : Node, IDisposable
{
    private uint refCount_;
    private GroveImpl grove_;

    protected BaseNode(GroveImpl grove)
    {
        grove_ = grove;
        refCount_ = 0;
    }

    public virtual void Dispose() { }

    public override void addRef() { ++refCount_; }

    public override void release()
    {
        if (--refCount_ == 0)
        {
            // Will be garbage collected
        }
    }

    public bool canReuse(NodePtr ptr)
    {
        return ptr.node == this && refCount_ == 1;
    }

    public override uint groveIndex() { return grove_.groveIndex(); }

    // Helper to set a GroveString from a StringC
    protected static void setString(ref GroveString str, StringC s)
    {
        str.assign(s.data(), s.size());
    }

    public override bool Equals(Node node)
    {
        if (node is BaseNode baseNode)
            return same(baseNode);
        return false;
    }

    public abstract bool same(BaseNode node);

    public virtual bool same2(ChunkNode? node) { return false; }
    public virtual bool same2(DataNode? node) { return false; }
    public virtual bool same2(AttributeAsgnNode? node) { return false; }
    public virtual bool same2(AttributeValueTokenNode? node) { return false; }
    public virtual bool same2(CdataAttributeValueNode? node) { return false; }
    public virtual bool same2(EntityNode? node) { return false; }
    public virtual bool same2(NotationNode? node) { return false; }
    public virtual bool same2(ExternalIdNode? node) { return false; }
    public virtual bool same2(DocumentTypeNode? node) { return false; }
    public virtual bool same2(SgmlConstantsNode? node) { return false; }
    public virtual bool same2(MessageNode? node) { return false; }
    public virtual bool same2(ElementTypeNode? node) { return false; }
    public virtual bool same2(ModelGroupNode? node) { return false; }
    public virtual bool same2(ElementTokenNode? node) { return false; }
    public virtual bool same2(PcdataTokenNode? node) { return false; }
    public virtual bool same2(AttributeDefNode? node) { return false; }
    public virtual bool same2(DefaultEntityNode? node) { return false; }

    public GroveImpl grove() { return grove_; }

    public override AccessResult nextSibling(ref NodePtr ptr)
    {
        return nextChunkSibling(ref ptr);
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        NodePtr head = new NodePtr();
        AccessResult ret = firstChild(ref head);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(head));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        return getParent(ref ptr);
    }

    public override AccessResult getGroveRoot(ref NodePtr ptr)
    {
        ptr.assign(new SgmlDocumentNode(grove_, grove_.root()!));
        return AccessResult.accessOK;
    }

    public virtual AccessResult getLocation(ref Location location)
    {
        return AccessResult.accessNull;
    }

    public override bool queryInterface(string iid, out object? ptr)
    {
        ptr = null;
        return false;
    }

    public override bool chunkContains(Node nd)
    {
        if (!sameGrove(nd))
            return false;
        return same((BaseNode)nd);
    }

    public virtual bool inChunk(DataNode? node) { return false; }
    public virtual bool inChunk(CdataAttributeValueNode? node) { return false; }

    protected static uint secondHash(uint n)
    {
        return n * 1001;
    }
}

public class ChunkNode : BaseNode
{
    protected LocChunk chunk_;

    public ChunkNode(GroveImpl grove, LocChunk chunk) : base(grove)
    {
        chunk_ = chunk;
    }

    public LocChunk chunk() { return chunk_; }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(ChunkNode? node)
    {
        return node != null && chunk_ == node.chunk_;
    }

    public override uint hash()
    {
        return (uint)chunk_.GetHashCode();
    }

    public override AccessResult getParent(ref NodePtr ptr)
    {
        if (chunk_.origin == null)
            return AccessResult.accessNull;
        return chunk_.origin.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult getTreeRoot(ref NodePtr nd)
    {
        nd.assign(new SgmlDocumentNode(grove(), grove().root()!));
        return AccessResult.accessOK;
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        return getParent(ref ptr);
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id id)
    {
        id = ComponentName.Id.idContent;
        return AccessResult.accessOK;
    }

    public override AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        // The forwarding chunk has origin = null, so it will stop
        // the iteration before after() can return null.
        Chunk? p = chunk_.after();
        while (p == grove().completeLimit())
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        if (p?.origin != chunk_.origin)
            return AccessResult.accessNull;
        return p!.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult nextChunkAfter(ref NodePtr ptr)
    {
        Chunk? p = chunk_.after();
        while (p == grove().completeLimit())
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        return p!.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult firstSibling(ref NodePtr ptr)
    {
        Chunk? first;
        AccessResult ret = chunk_.getFirstSibling(grove(), out first);
        if (ret != AccessResult.accessOK)
            return ret;
        return first!.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult siblingsIndex(out uint index)
    {
        Chunk? p;
        AccessResult ret = chunk_.getFirstSibling(grove(), out p);
        if (ret != AccessResult.accessOK)
        {
            index = 0;
            return ret;
        }
        index = 0;
        while (p != chunk_)
        {
            uint tem;
            if (p!.getFollowing(grove(), out p, out tem) != AccessResult.accessOK)
            {
                // Cannot happen
                return AccessResult.accessNull;
            }
            index += tem;
        }
        return AccessResult.accessOK;
    }

    public override AccessResult followSiblingRef(uint n, ref NodePtr ptr)
    {
        Chunk? p;
        uint count;
        AccessResult ret = chunk().getFollowing(grove(), out p, out count);
        if (ret != AccessResult.accessOK)
            return ret;
        uint i = n;
        while (i > 0)
        {
            Chunk? lastP = p;
            ret = p!.getFollowing(grove(), out p, out count);
            if (ret == AccessResult.accessOK && count <= i)
                i -= count;
            else if (ret == AccessResult.accessOK || ret == AccessResult.accessNull)
            {
                lastP!.setNodePtrFirst(ref ptr, this);
                return ptr.node!.followSiblingRef(i - 1, ref ptr);
            }
            else
                return ret;
        }
        return p!.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult getLocation(ref Location location)
    {
        // Simplified implementation
        location = new Location();
        return AccessResult.accessNull;
    }

    public override void accept(NodeVisitor visitor)
    {
        // Base implementation - subclasses override
    }

    public override ClassDef classDef()
    {
        return ClassDef.sgmlDocument; // Base - subclasses override
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        NodePtr firstChild = new NodePtr();
        AccessResult ret = this.firstChild(ref firstChild);
        if (ret != AccessResult.accessOK)
        {
            if (ret == AccessResult.accessNull)
            {
                ptr.assign(new BaseNodeList());
                return AccessResult.accessOK;
            }
            return ret;
        }
        ptr.assign(new SiblingNodeList(firstChild));
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr next = new NodePtr();
        AccessResult ret = nextChunkAfter(ref next);
        if (ret != AccessResult.accessOK)
        {
            if (ret == AccessResult.accessNull)
            {
                ptr.assign(new BaseNodeList());
                return AccessResult.accessOK;
            }
            return ret;
        }
        ptr.assign(new SiblingNodeList(next));
        return AccessResult.accessOK;
    }
}

public class SgmlDocumentChunk : ParentChunk
{
    public Chunk? prolog;
    public Chunk? documentElement;
    public Chunk? epilog;

    public SgmlDocumentChunk()
    {
        prolog = null;
        documentElement = null;
        epilog = null;
    }

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        if (node != null)
            ptr.assign(new SgmlDocumentNode(node.grove(), this));
        return AccessResult.accessOK;
    }

    public override Chunk? after()
    {
        // In C++, returns this + 1 (pointer to next memory location)
        // In C#, we return null since we don't have chunk memory management
        return null;
    }
}

public class SgmlDocumentNode : ChunkNode
{
    public SgmlDocumentNode(GroveImpl grove, SgmlDocumentChunk chunk) : base(grove, chunk)
    {
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.sgmlDocument(this);
    }

    public override ClassDef classDef() { return ClassDef.sgmlDocument; }

    public override AccessResult getDocumentElement(ref NodePtr ptr)
    {
        SgmlDocumentChunk? root = chunk() as SgmlDocumentChunk;
        while (root?.documentElement == null)
        {
            if (grove().complete())
            {
                if (root?.documentElement != null)
                    break;
                return AccessResult.accessNull;
            }
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        return root!.documentElement!.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult getElements(ref NamedNodeListPtr ptr)
    {
        // Wait for document element
        while (grove().root()?.documentElement == null)
        {
            if (grove().complete())
            {
                if (grove().root()?.documentElement != null)
                    break;
                return AccessResult.accessNull;
            }
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        if (grove().generalSubstTable() == null)
            return AccessResult.accessNull;
        // Would create ElementsNamedNodeList
        return AccessResult.accessNull;
    }

    public override AccessResult getEntities(ref NamedNodeListPtr ptr)
    {
        while (grove().governingDtd() == null)
        {
            if (grove().complete())
            {
                if (grove().governingDtd() != null)
                    break;
                return AccessResult.accessNull;
            }
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        // Would create DocEntitiesNamedNodeList
        return AccessResult.accessNull;
    }

    public override AccessResult getDefaultedEntities(ref NamedNodeListPtr ptr)
    {
        while (!grove().complete())
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        // Would create DefaultedEntitiesNamedNodeList
        return AccessResult.accessNull;
    }

    public override AccessResult getGoverningDoctype(ref NodePtr ptr)
    {
        while (grove().governingDtd() == null)
        {
            if (grove().complete())
            {
                if (grove().governingDtd() != null)
                    break;
                return AccessResult.accessNull;
            }
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        ptr.assign(new DocumentTypeNode(grove(), grove().governingDtd()));
        return AccessResult.accessOK;
    }

    public override AccessResult getDoctypesAndLinktypes(ref NamedNodeListPtr ptr)
    {
        while (grove().governingDtd() == null)
        {
            if (grove().complete())
            {
                if (grove().governingDtd() != null)
                    break;
                return AccessResult.accessNull;
            }
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        // Would create DoctypesAndLinktypesNamedNodeList
        return AccessResult.accessNull;
    }

    public override AccessResult getProlog(ref NodeListPtr ptr)
    {
        SgmlDocumentChunk? root = chunk() as SgmlDocumentChunk;
        while (root?.prolog == null)
        {
            if (root?.documentElement != null || grove().complete())
                break;
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        if (root?.prolog == null)
        {
            ptr.assign(new BaseNodeList());
        }
        else
        {
            NodePtr tem = new NodePtr();
            root.prolog.setNodePtrFirst(ref tem, this);
            ptr.assign(new SiblingNodeList(tem));
        }
        return AccessResult.accessOK;
    }

    public override AccessResult getEpilog(ref NodeListPtr ptr)
    {
        SgmlDocumentChunk? root = chunk() as SgmlDocumentChunk;
        while (root?.epilog == null)
        {
            if (grove().complete())
                break;
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        if (root?.epilog == null)
        {
            ptr.assign(new BaseNodeList());
        }
        else
        {
            NodePtr tem = new NodePtr();
            root.epilog.setNodePtrFirst(ref tem, this);
            ptr.assign(new SiblingNodeList(tem));
        }
        return AccessResult.accessOK;
    }

    public override AccessResult getSgmlConstants(ref NodePtr ptr)
    {
        ptr.assign(new SgmlConstantsNode(grove()));
        return AccessResult.accessOK;
    }

    public override AccessResult getApplicationInfo(ref GroveString str)
    {
        StringC? appinfo;
        while (!grove().getAppinfo(out appinfo))
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        if (appinfo == null)
            return AccessResult.accessNull;
        setString(ref str, appinfo);
        return AccessResult.accessOK;
    }

    public override AccessResult getMessages(ref NodeListPtr ptr)
    {
        while (grove().messageList() == null)
        {
            if (grove().complete())
                break;
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        if (grove().messageList() != null)
        {
            NodePtr tem = new NodePtr(new MessageNode(grove(), grove().messageList()));
            ptr.assign(new SiblingNodeList(tem));
        }
        else
            ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        return AccessResult.accessNotInClass;
    }

    public override AccessResult firstSibling(ref NodePtr ptr)
    {
        return AccessResult.accessNotInClass;
    }

    public override AccessResult siblingsIndex(out uint index)
    {
        index = 0;
        return AccessResult.accessNotInClass;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id id)
    {
        id = ComponentName.Id.noId;
        return AccessResult.accessNull;
    }

    public AccessResult getSd(out ConstPtr<Sd> sd, out ConstPtr<Syntax> prologSyntax, out ConstPtr<Syntax> instanceSyntax)
    {
        grove().getSd(out sd, out prologSyntax, out instanceSyntax);
        return AccessResult.accessOK;
    }
}

public class ElementNode : ChunkNode
{
    public ElementNode(GroveImpl grove, ElementChunk chunk) : base(grove, chunk)
    {
    }

    // Return the chunk as ElementChunk
    public new ElementChunk chunk() { return (ElementChunk)chunk_; }

    public override AccessResult attributeRef(uint i, ref NodePtr ptr)
    {
        var defList = chunk().attDefList();
        if (defList == null || i >= defList.size())
            return AccessResult.accessNull;
        ptr.assign(new ElementAttributeAsgnNode(grove(), i, chunk()));
        return AccessResult.accessOK;
    }

    public override AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        while (chunk().nextSibling == null)
        {
            if (!grove().maybeMoreSiblings(chunk()))
            {
                // Allow for the possibility of invalid documents with elements in the epilog
                if ((Chunk?)chunk() == grove().root()?.documentElement)
                    return AccessResult.accessNotInClass;
                else
                    return AccessResult.accessNull;
            }
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        return chunk().nextSibling!.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult nextChunkAfter(ref NodePtr ptr)
    {
        Chunk? p = chunk_.after();
        while (p == grove().completeLimit())
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        return p!.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult firstChild(ref NodePtr ptr)
    {
        Chunk? p = chunk().after();
        while (p == grove().completeLimit())
        {
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        if (p?.origin == chunk())
            return p.setNodePtrFirst(ref ptr, this);
        return AccessResult.accessNull;
    }

    public override AccessResult getAttributes(ref NamedNodeListPtr ptr)
    {
        // Would create ElementAttributesNamedNodeList
        return AccessResult.accessNull;
    }

    public override AccessResult getGi(ref GroveString str)
    {
        if (chunk().type == null)
            return AccessResult.accessNull;
        setString(ref str, chunk().type.name());
        return AccessResult.accessOK;
    }

    public override bool hasGi(GroveString gi)
    {
        if (chunk().type == null)
            return false;
        StringC typeName = chunk().type.name();
        nuint len = typeName.size();
        if (len != gi.size())
            return false;
        SubstTable? subst = grove().generalSubstTable();
        if (subst == null)
            return false;
        for (nuint i = 0; i < len; i++)
            if (subst.subst(gi.data()[i]) != typeName.data()[i])
                return false;
        return true;
    }

    public override AccessResult getId(ref GroveString str)
    {
        StringC? id = chunk().id();
        if (id == null)
            return AccessResult.accessNull;
        setString(ref str, id);
        return AccessResult.accessOK;
    }

    public override AccessResult getContent(ref NodeListPtr ptr)
    {
        return children(ref ptr);
    }

    public override AccessResult getMustOmitEndTag(out bool mustOmit)
    {
        mustOmit = chunk().mustOmitEndTag();
        return AccessResult.accessOK;
    }

    public override AccessResult getIncluded(out bool included)
    {
        included = chunk().included();
        return AccessResult.accessOK;
    }

    public override AccessResult elementIndex(out uint index)
    {
        index = chunk().elementIndex;
        return AccessResult.accessOK;
    }

    public override AccessResult getElementType(ref NodePtr ptr)
    {
        if (chunk().elementType() == null)
            return AccessResult.accessNull;
        ptr.assign(new ElementTypeNode(grove(), chunk().elementType()));
        return AccessResult.accessOK;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.element(this);
    }

    public override ClassDef classDef() { return ClassDef.element; }

    public static void add(GroveImpl grove, StartElementEvent @event)
    {
        grove.setLocOrigin(@event.location().origin());
        ElementChunk chunk = new ElementChunk();
        chunk.type = @event.elementType();
        chunk.locIndex = @event.location().index();
        grove.push(chunk, false);
    }

    public void reuseFor(ElementChunk chunk)
    {
        chunk_ = chunk;
    }
}

public class CharsChunk : LocChunk
{
    public nuint size;
    protected Char[]? chars_;  // Character data stored after this chunk in C++ - we use an array

    public override Chunk? after()
    {
        // In C++, returns pointer after the chars data
        // In C#, we need to maintain chunk linkage differently
        return nextChunk;
    }

    protected Chunk? nextChunk;  // Link to next chunk

    public Char[]? data()
    {
        return chars_;
    }

    public void setData(Char[] data)
    {
        chars_ = data;
    }

    public static nuint allocSize(nuint nChars)
    {
        // Return size needed for chunk + character data
        return nChars;  // Simplified - in C++ this includes struct overhead
    }
}

public class DataChunk : CharsChunk
{
    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        ptr.assign(new DataNode(node!.grove(), this, 0));
        return AccessResult.accessOK;
    }

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, ElementNode? node)
    {
        ptr.assign(new DataNode(node!.grove(), this, 0));
        return AccessResult.accessOK;
    }

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, DataNode? node)
    {
        if (node!.canReuse(ptr))
            node.reuseFor(this, 0);
        else
            ptr.assign(new DataNode(node.grove(), this, 0));
        return AccessResult.accessOK;
    }

    public override AccessResult getFollowing(GroveImpl grove, out Chunk? chunk, out uint nNodes)
    {
        // Similar to Chunk::getFollowing but we know the size
        Chunk? p = after();
        while (p == grove.completeLimit())
            if (!grove.waitForMoreNodes())
            {
                chunk = null;
                nNodes = 0;
                return AccessResult.accessTimeout;
            }
        if (p?.origin != origin)
        {
            chunk = null;
            nNodes = 0;
            return AccessResult.accessNull;
        }
        nNodes = (uint)size;
        chunk = p;
        return AccessResult.accessOK;
    }
}

public class DataNode : ChunkNode
{
    protected nuint index_;

    public DataNode(GroveImpl grove, DataChunk chunk, nuint index) : base(grove, chunk)
    {
        index_ = index;
    }

    // Return chunk as DataChunk
    public new DataChunk chunk() { return (DataChunk)chunk_; }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(DataNode? node)
    {
        return node != null && chunk_ == node.chunk_ && index_ == node.index_;
    }

    public override AccessResult nextSibling(ref NodePtr ptr)
    {
        if (index_ + 1 < chunk().size)
        {
            if (canReuse(ptr))
                index_ += 1;
            else
                ptr.assign(new DataNode(grove(), chunk(), index_ + 1));
            return AccessResult.accessOK;
        }
        return nextChunkSibling(ref ptr);
    }

    public override AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        // The forwarding chunk has origin = null, so it will stop
        // the iteration before after() can return null.
        Chunk? p = chunk_.after();
        while (p == grove().completeLimit())
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        if (p?.origin != chunk_.origin)
            return AccessResult.accessNull;
        return p!.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult nextChunkAfter(ref NodePtr ptr)
    {
        Chunk? p = chunk_.after();
        while (p == grove().completeLimit())
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        return p!.setNodePtrFirst(ref ptr, this);
    }

    public override AccessResult siblingsIndex(out uint index)
    {
        AccessResult ret = base.siblingsIndex(out index);
        if (ret == AccessResult.accessOK)
            index += (uint)index_;
        return ret;
    }

    public override AccessResult followSiblingRef(uint n, ref NodePtr ptr)
    {
        if (n < chunk().size - index_ - 1)
        {
            if (canReuse(ptr))
                index_ += 1 + (nuint)n;
            else
                ptr.assign(new DataNode(grove(), chunk(), index_ + (nuint)n + 1));
            return AccessResult.accessOK;
        }
        return base.followSiblingRef((uint)(n - (chunk().size - index_ - 1)), ref ptr);
    }

    public override AccessResult charChunk(SdataMapper mapper, ref GroveString str)
    {
        str.assign(chunk().data(), index_, chunk().size - index_);
        return AccessResult.accessOK;
    }

    public override bool chunkContains(Node nd)
    {
        if (!sameGrove(nd))
            return false;
        if (nd is BaseNode baseNode)
            return baseNode.inChunk(this);
        return false;
    }

    public override bool inChunk(DataNode? node)
    {
        return node != null && chunk_ == node.chunk_ && index_ >= node.index_;
    }

    public override AccessResult getNonSgml(out uint value)
    {
        value = 0;
        return AccessResult.accessNull;
    }

    public override AccessResult getLocation(ref Location location)
    {
        AccessResult ret = base.getLocation(ref location);
        if (ret == AccessResult.accessOK)
        {
            // Adjust index by character offset within chunk
            uint newIndex = location.index() + (uint)index_;
            location = new Location(location.origin().pointer(), newIndex);
        }
        return ret;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.dataChar(this);
    }

    public override ClassDef classDef() { return ClassDef.dataChar; }

    public override uint hash()
    {
        return secondHash((uint)chunk_.GetHashCode() + (uint)index_);
    }

    public static void add(GroveImpl grove, DataEvent @event)
    {
        nuint dataLen = @event.dataLength();
        if (dataLen > 0)
        {
            grove.setLocOrigin(@event.location().origin());
            DataChunk chunk = new DataChunk();
            chunk.size = dataLen;
            chunk.locIndex = @event.location().index();
            Char[] data = new Char[dataLen];
            if (@event.data() != null)
                Array.Copy(@event.data()!, 0, data, 0, (int)dataLen);
            chunk.setData(data);
            grove.appendSibling(chunk);
        }
    }

    public void reuseFor(DataChunk chunk, nuint index)
    {
        chunk_ = chunk;
        index_ = index;
    }
}

public class PiChunk : CharsChunk
{
    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        ptr.assign(new PiNode(node!.grove(), this));
        return AccessResult.accessOK;
    }
}

public class PiNode : ChunkNode
{
    public PiNode(GroveImpl grove, PiChunk chunk) : base(grove, chunk)
    {
    }

    // Return chunk as PiChunk
    public new PiChunk chunk() { return (PiChunk)chunk_; }

    public override AccessResult getSystemData(ref GroveString str)
    {
        str.assign(chunk().data(), 0, chunk().size);
        return AccessResult.accessOK;
    }

    public override AccessResult getEntityName(ref GroveString str)
    {
        return AccessResult.accessNull;
    }

    public override AccessResult getEntity(ref NodePtr ptr)
    {
        return AccessResult.accessNull;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.pi(this);
    }

    public override ClassDef classDef() { return ClassDef.pi; }

    public static void add(GroveImpl grove, PiEvent @event)
    {
        grove.setLocOrigin(@event.location().origin());
        nuint dataLen = @event.dataLength();
        PiChunk chunk = new PiChunk();
        chunk.size = dataLen;
        chunk.locIndex = @event.location().index();
        if (dataLen > 0 && @event.data() != null)
        {
            Char[] data = new Char[dataLen];
            Array.Copy(@event.data()!, 0, data, 0, (int)dataLen);
            chunk.setData(data);
        }
        grove.appendSibling(chunk);
    }
}

public class EntityRefChunk : LocChunk
{
    public Entity? entity;
    protected Chunk? nextChunk_;

    public override Chunk? after()
    {
        return nextChunk_;
    }
}

public class EntityRefNode : ChunkNode
{
    public EntityRefNode(GroveImpl grove, EntityRefChunk chunk) : base(grove, chunk)
    {
    }

    public override AccessResult getEntity(ref NodePtr ptr)
    {
        var erc = chunk();
        if (erc.entity == null)
            return AccessResult.accessNull;
        ptr.assign(new EntityNode(grove(), erc.entity));
        return AccessResult.accessOK;
    }

    public override AccessResult getEntityName(ref GroveString str)
    {
        var erc = chunk();
        if (erc.entity == null)
            return AccessResult.accessNull;
        setString(ref str, erc.entity.name());
        return AccessResult.accessOK;
    }

    protected new EntityRefChunk chunk()
    {
        return (EntityRefChunk)chunk_;
    }
}

public class SdataChunk : EntityRefChunk
{
    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        ptr.assign(new SdataNode(node!.grove(), this));
        return AccessResult.accessOK;
    }
}

public class SdataNode : EntityRefNode
{
    private Char c_;

    public SdataNode(GroveImpl grove, SdataChunk chunk) : base(grove, chunk)
    {
    }

    public override AccessResult charChunk(SdataMapper mapper, ref GroveString str)
    {
        var sdataChunk = (SdataChunk)chunk_;
        if (sdataChunk.entity == null)
            return AccessResult.accessNull;
        StringC name = sdataChunk.entity.name();
        var internalEntity = sdataChunk.entity.asInternalEntity();
        if (internalEntity == null)
            return AccessResult.accessNull;
        StringC text = internalEntity.@string();
        if (mapper.sdataMap(new GroveString(name.data(), 0, name.size()),
                            new GroveString(text.data(), 0, text.size()), out c_))
        {
            Char[] chars = new Char[] { c_ };
            str.assign(chars, 0, 1);
            return AccessResult.accessOK;
        }
        return AccessResult.accessNull;
    }

    public override AccessResult getSystemData(ref GroveString str)
    {
        var sdataChunk = (SdataChunk)chunk_;
        if (sdataChunk.entity == null)
            return AccessResult.accessNull;
        var internalEntity = sdataChunk.entity.asInternalEntity();
        if (internalEntity == null)
            return AccessResult.accessNull;
        setString(ref str, internalEntity.@string());
        return AccessResult.accessOK;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.sdata(this);
    }

    public override ClassDef classDef() { return ClassDef.sdata; }

    public static void add(GroveImpl grove, SdataEntityEvent @event)
    {
        grove.setLocOrigin(@event.location().origin());
        SdataChunk chunk = new SdataChunk();
        chunk.entity = @event.entity();
        chunk.locIndex = @event.location().index();
        grove.appendSibling(chunk);
    }
}

public class NonSgmlChunk : LocChunk
{
    public Char c;
    protected Chunk? nextChunk_;

    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        ptr.assign(new NonSgmlNode(node!.grove(), this));
        return AccessResult.accessOK;
    }

    public override Chunk? after()
    {
        return nextChunk_;
    }
}

public class NonSgmlNode : ChunkNode
{
    public NonSgmlNode(GroveImpl grove, NonSgmlChunk chunk) : base(grove, chunk)
    {
    }

    // Return chunk as NonSgmlChunk
    public new NonSgmlChunk chunk() { return (NonSgmlChunk)chunk_; }

    public override AccessResult charChunk(SdataMapper mapper, ref GroveString str)
    {
        // Non-SGML characters cannot be mapped to regular characters
        return AccessResult.accessNull;
    }

    public override AccessResult getNonSgml(out uint value)
    {
        value = chunk().c;
        return AccessResult.accessOK;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.nonSgml(this);
    }

    public override ClassDef classDef() { return ClassDef.nonSgml; }

    public static void add(GroveImpl grove, NonSgmlCharEvent @event)
    {
        grove.setLocOrigin(@event.location().origin());
        NonSgmlChunk chunk = new NonSgmlChunk();
        chunk.c = @event.character();
        chunk.locIndex = @event.location().index();
        grove.appendSibling(chunk);
    }
}

public class ExternalDataChunk : EntityRefChunk
{
    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        ptr.assign(new ExternalDataNode(node!.grove(), this));
        return AccessResult.accessOK;
    }
}

public class ExternalDataNode : EntityRefNode
{
    public ExternalDataNode(GroveImpl grove, ExternalDataChunk chunk) : base(grove, chunk)
    {
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.externalData(this);
    }

    public override ClassDef classDef() { return ClassDef.externalData; }

    public static void add(GroveImpl grove, ExternalDataEntityEvent @event)
    {
        grove.setLocOrigin(@event.location().origin());
        ExternalDataChunk chunk = new ExternalDataChunk();
        chunk.entity = @event.entity();
        chunk.locIndex = @event.location().index();
        grove.appendSibling(chunk);
    }
}

public class SubdocChunk : EntityRefChunk
{
    public override AccessResult setNodePtrFirst(ref NodePtr ptr, BaseNode? node)
    {
        ptr.assign(new SubdocNode(node!.grove(), this));
        return AccessResult.accessOK;
    }
}

public class SubdocNode : EntityRefNode
{
    public SubdocNode(GroveImpl grove, SubdocChunk chunk) : base(grove, chunk)
    {
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.subdocument(this);
    }

    public override ClassDef classDef() { return ClassDef.subdocument; }

    public static void add(GroveImpl grove, SubdocEntityEvent @event)
    {
        grove.setLocOrigin(@event.location().origin());
        SubdocChunk chunk = new SubdocChunk();
        chunk.entity = @event.entity();
        chunk.locIndex = @event.location().index();
        grove.appendSibling(chunk);
    }
}

public abstract class AttributeDefOrigin
{
    protected nuint attIndex_;

    public AttributeDefOrigin(nuint attIndex = 0)
    {
        attIndex_ = attIndex;
    }

    public abstract AttributeDefinitionList? attDefList();

    public abstract Node makeCdataAttributeValueNode(
        GroveImpl grove,
        AttributeValue? value,
        nuint attIndex,
        TextIter iter,
        nuint charIndex = 0);

    public abstract Node makeAttributeValueTokenNode(
        GroveImpl grove,
        TokenizedAttributeValue? value,
        nuint attIndex,
        nuint tokenIndex);

    public abstract Node makeOriginNode(GroveImpl grove, nuint attIndex);

    public virtual AccessResult makeAttributeValueNode(GroveImpl grove, ref NodePtr ptr, AttributeValue? value)
    {
        if (value != null)
        {
            Text? text;
            StringC? str;
            switch (value.info(out text, out str))
            {
                case AttributeValue.Type.tokenized:
                    var tokenizedValue = value as TokenizedAttributeValue;
                    if (tokenizedValue != null)
                    {
                        ptr.assign(makeAttributeValueTokenNode(grove, tokenizedValue, attIndex_, 0));
                        return AccessResult.accessOK;
                    }
                    break;
                case AttributeValue.Type.cdata:
                    if (text != null)
                    {
                        TextIter iter = new TextIter(text);
                        if (!CdataAttributeValueNode.skipBoring(iter))
                        {
                            return AccessResult.accessNull;
                        }
                        ptr.assign(makeCdataAttributeValueNode(grove, value, attIndex_, iter));
                        return AccessResult.accessOK;
                    }
                    break;
            }
        }
        return AccessResult.accessNull;
    }

    public virtual AccessResult makeAttributeValueNodeList(GroveImpl grove, ref NodeListPtr ptr, AttributeValue? value)
    {
        NodePtr nodePtr = new NodePtr();
        AccessResult result = makeAttributeValueNode(grove, ref nodePtr, value);
        if (result == AccessResult.accessOK)
        {
            if (nodePtr.node == null)
                ptr.assign(new BaseNodeList());
            else
                ptr.assign(new SiblingNodeList(nodePtr));
        }
        return result;
    }

    public virtual AccessResult makeAttributeDefNode(GroveImpl grove, ref NodePtr ptr, nuint attributeDefIdx)
    {
        return AccessResult.accessNull;
    }

    public virtual AccessResult makeAttributeDefList(GroveImpl grove, ref NodeListPtr ptr, nuint firstIdx)
    {
        return AccessResult.accessNull;
    }

    public AccessResult makeAttributeDefNode(GroveImpl grove, ref NodePtr ptr, StringC name)
    {
        return AccessResult.accessNull;
    }

    public abstract object? attributeOriginId();

    public nuint attIndex() { return attIndex_; }
}

public abstract class AttributeOrigin : AttributeDefOrigin
{
    public abstract AttributeValue? attributeValue(nuint attIndex, GroveImpl grove);
    public abstract AccessResult setNodePtrAttributeOrigin(ref NodePtr ptr, BaseNode? node);
    public abstract Node makeAttributeAsgnNode(GroveImpl grove, nuint attIndex);
}

public abstract class AttributeAsgnNode : BaseNode
{
    protected nuint attIndex_;

    public AttributeAsgnNode(GroveImpl grove, nuint attIndex) : base(grove)
    {
        attIndex_ = attIndex;
    }

    // Abstract methods that derived classes must implement
    public abstract AttributeDefinitionList? attDefList();
    public abstract AttributeValue? attributeValue(nuint attIndex, GroveImpl grove);
    public abstract AccessResult setNodePtrAttributeOrigin(ref NodePtr ptr, BaseNode? node);
    public abstract Node makeAttributeAsgnNode(GroveImpl grove, nuint attIndex);
    public abstract object? attributeOriginId();
    public abstract AccessResult makeAttributeValueNode(GroveImpl grove, ref NodePtr ptr, AttributeValue? value);
    public abstract AccessResult makeAttributeValueNodeList(GroveImpl grove, ref NodeListPtr ptr, AttributeValue? value);
    public abstract AccessResult makeAttributeDefNode(GroveImpl grove, ref NodePtr ptr, nuint attributeDefIdx);

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        return setNodePtrAttributeOrigin(ref ptr, this);
    }

    public override AccessResult getName(ref GroveString str)
    {
        var defList = attDefList();
        if (defList == null)
            return AccessResult.accessNull;
        var def = defList.def(attIndex_);
        if (def == null)
            return AccessResult.accessNull;
        setString(ref str, def.name());
        return AccessResult.accessOK;
    }

    public override AccessResult getImplied(out bool implied)
    {
        var value = attributeValue(attIndex_, grove());
        implied = (value != null && value.text() == null);
        return AccessResult.accessOK;
    }

    public override AccessResult getValue(ref NodeListPtr ptr)
    {
        return children(ref ptr);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        var value = attributeValue(attIndex_, grove());
        return makeAttributeValueNodeList(grove(), ref ptr, value);
    }

    public override AccessResult firstChild(ref NodePtr ptr)
    {
        var value = attributeValue(attIndex_, grove());
        return makeAttributeValueNode(grove(), ref ptr, value);
    }

    public override AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        return followSiblingRef(0, ref ptr);
    }

    public override AccessResult followSiblingRef(uint i, ref NodePtr ptr)
    {
        var defList = attDefList();
        if (defList == null)
            return AccessResult.accessNull;
        // Do it like this to avoid overflow.
        if (i >= defList.size() - attIndex_ - 1)
            return AccessResult.accessNull;
        if (canReuse(ptr))
            attIndex_ += (nuint)i + 1;
        else
            ptr.assign(makeAttributeAsgnNode(grove(), attIndex_ + 1 + (nuint)i));
        return AccessResult.accessOK;
    }

    public override AccessResult firstSibling(ref NodePtr ptr)
    {
        if (canReuse(ptr))
            attIndex_ = 0;
        else
            ptr.assign(makeAttributeAsgnNode(grove(), 0));
        return AccessResult.accessOK;
    }

    public override AccessResult siblingsIndex(out uint index)
    {
        index = (uint)attIndex_;
        return AccessResult.accessOK;
    }

    public override AccessResult getTokenSep(out Char sep)
    {
        sep = 0;
        var value = attributeValue(attIndex_, grove());
        if (value == null)
            return AccessResult.accessNull;
        Text? text;
        StringC? str;
        if (value.info(out text, out str) != AttributeValue.Type.tokenized)
            return AccessResult.accessNull;
        var tValue = value as TokenizedAttributeValue;
        if (tValue == null || tValue.nTokens() <= 1)
            return AccessResult.accessNull;
        Char[]? tokenPtr;
        nuint len;
        tValue.token(0, out tokenPtr, out len);
        // the character following the token is a space
        if (tokenPtr != null && len < (nuint)tokenPtr.Length)
            sep = tokenPtr[len];
        return AccessResult.accessOK;
    }

    public override AccessResult tokens(ref GroveString str)
    {
        var value = attributeValue(attIndex_, grove());
        if (value == null)
            return AccessResult.accessNull;
        Text? text;
        StringC? strC;
        if (value.info(out text, out strC) != AttributeValue.Type.tokenized)
            return AccessResult.accessNull;
        if (strC == null)
            return AccessResult.accessNull;
        setString(ref str, strC);
        return AccessResult.accessOK;
    }

    public override AccessResult getAttributeDef(ref NodePtr ptr)
    {
        return makeAttributeDefNode(grove(), ref ptr, attIndex_);
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.attributeAssignment(this);
    }

    public override ClassDef classDef() { return ClassDef.attributeAssignment; }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idAttributes;
        return AccessResult.accessOK;
    }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(AttributeAsgnNode? node)
    {
        if (node == null)
            return false;
        return attributeOriginId() == node.attributeOriginId() && attIndex_ == node.attIndex_;
    }

    public override uint hash()
    {
        uint n = (uint)(attributeOriginId()?.GetHashCode() ?? 0);
        return secondHash(n + (uint)attIndex_);
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

// Element attribute origin - provides attribute context from an element
public class ElementAttributeOrigin : AttributeOrigin
{
    protected ElementChunk chunk_;

    public ElementAttributeOrigin(ElementChunk chunk)
    {
        chunk_ = chunk;
    }

    public override AttributeDefinitionList? attDefList()
    {
        return chunk_.attDefList();
    }

    public override AttributeValue? attributeValue(nuint attIndex, GroveImpl grove)
    {
        return chunk_.attributeValue(attIndex, grove);
    }

    public override AccessResult setNodePtrAttributeOrigin(ref NodePtr ptr, BaseNode? node)
    {
        return chunk_.setNodePtrFirst(ref ptr, node);
    }

    public override Node makeCdataAttributeValueNode(
        GroveImpl grove,
        AttributeValue? value,
        nuint attIndex,
        TextIter iter,
        nuint charIndex = 0)
    {
        return new ElementCdataAttributeValueNode(grove, value, attIndex, iter, charIndex, chunk_);
    }

    public override Node makeAttributeValueTokenNode(
        GroveImpl grove,
        TokenizedAttributeValue? value,
        nuint attIndex,
        nuint tokenIndex)
    {
        return new ElementAttributeValueTokenNode(grove, value, attIndex, tokenIndex, chunk_);
    }

    public override Node makeAttributeAsgnNode(GroveImpl grove, nuint attIndex)
    {
        return new ElementAttributeAsgnNode(grove, attIndex, chunk_);
    }

    public override Node makeOriginNode(GroveImpl grove, nuint attIndex)
    {
        return makeAttributeAsgnNode(grove, attIndex);
    }

    public override object? attributeOriginId()
    {
        return chunk_;
    }
}

// Element type attribute def origin - provides attribute definition context from element type
public class ElementTypeAttributeDefOrigin : AttributeDefOrigin
{
    protected ElementType elementType_;

    public ElementTypeAttributeDefOrigin(ElementType elementType) : base(0)
    {
        elementType_ = elementType;
    }

    public override AttributeDefinitionList? attDefList()
    {
        return elementType_.attributeDefTemp();
    }

    public override AccessResult makeAttributeDefNode(GroveImpl grove, ref NodePtr ptr, nuint attributeDefIdx)
    {
        ptr.assign(new ElementTypeAttributeDefNode(grove, elementType_, attributeDefIdx));
        return AccessResult.accessOK;
    }

    public override AccessResult makeAttributeDefList(GroveImpl grove, ref NodeListPtr ptr, nuint firstAttDefIdx)
    {
        ptr.assign(new ElementTypeAttributeDefsNodeList(grove, elementType_, firstAttDefIdx));
        return AccessResult.accessOK;
    }

    public override Node makeCdataAttributeValueNode(
        GroveImpl grove,
        AttributeValue? value,
        nuint attIndex,
        TextIter iter,
        nuint charIndex = 0)
    {
        return new ElementTypeCdataAttributeValueNode(grove, value, attIndex, iter, charIndex, elementType_);
    }

    public override Node makeAttributeValueTokenNode(
        GroveImpl grove,
        TokenizedAttributeValue? value,
        nuint attIndex,
        nuint tokenIndex)
    {
        return new ElementTypeAttributeValueTokenNode(grove, value, attIndex, tokenIndex, elementType_);
    }

    public override Node makeOriginNode(GroveImpl grove, nuint attIndex)
    {
        return new ElementTypeAttributeDefNode(grove, elementType_, attIndex_);
    }

    public override object? attributeOriginId()
    {
        return elementType_;
    }
}

// Notation attribute def origin - provides attribute definition context from notation
public class NotationAttributeDefOrigin : AttributeDefOrigin
{
    protected Notation notation_;

    public NotationAttributeDefOrigin(Notation notation) : base(0)
    {
        notation_ = notation;
    }

    public override AttributeDefinitionList? attDefList()
    {
        return notation_.attributeDefTemp();
    }

    public override AccessResult makeAttributeDefNode(GroveImpl grove, ref NodePtr ptr, nuint attributeDefIdx)
    {
        ptr.assign(new NotationAttributeDefNode(grove, notation_, attributeDefIdx));
        return AccessResult.accessOK;
    }

    public override AccessResult makeAttributeDefList(GroveImpl grove, ref NodeListPtr ptr, nuint firstAttDefIdx)
    {
        ptr.assign(new NotationAttributeDefsNodeList(grove, notation_, firstAttDefIdx));
        return AccessResult.accessOK;
    }

    public override Node makeCdataAttributeValueNode(
        GroveImpl grove,
        AttributeValue? value,
        nuint attIndex,
        TextIter iter,
        nuint charIndex = 0)
    {
        return new NotationCdataAttributeValueNode(grove, value, attIndex, iter, charIndex, notation_);
    }

    public override Node makeAttributeValueTokenNode(
        GroveImpl grove,
        TokenizedAttributeValue? value,
        nuint attIndex,
        nuint tokenIndex)
    {
        return new NotationAttributeValueTokenNode(grove, value, attIndex, tokenIndex, notation_);
    }

    public override Node makeOriginNode(GroveImpl grove, nuint attIndex)
    {
        return new NotationAttributeDefNode(grove, notation_, attIndex_);
    }

    public override object? attributeOriginId()
    {
        return notation_;
    }
}

// Element attribute assignment node
public class ElementAttributeAsgnNode : AttributeAsgnNode
{
    protected ElementChunk chunk_;

    public ElementAttributeAsgnNode(GroveImpl grove, nuint attIndex, ElementChunk chunk)
        : base(grove, attIndex)
    {
        chunk_ = chunk;
    }

    public override AttributeDefinitionList? attDefList()
    {
        return chunk_.attDefList();
    }

    public override AttributeValue? attributeValue(nuint attIndex, GroveImpl grove)
    {
        return chunk_.attributeValue(attIndex, grove);
    }

    public override AccessResult setNodePtrAttributeOrigin(ref NodePtr ptr, BaseNode? node)
    {
        return chunk_.setNodePtrFirst(ref ptr, node);
    }

    public override Node makeAttributeAsgnNode(GroveImpl grove, nuint attIndex)
    {
        return new ElementAttributeAsgnNode(grove, attIndex, chunk_);
    }

    public override object? attributeOriginId()
    {
        return chunk_;
    }

    public override AccessResult makeAttributeValueNode(GroveImpl grove, ref NodePtr ptr, AttributeValue? value)
    {
        if (value != null)
        {
            Text? text;
            StringC? str;
            switch (value.info(out text, out str))
            {
                case AttributeValue.Type.tokenized:
                    var tokenizedValue = value as TokenizedAttributeValue;
                    if (tokenizedValue != null)
                    {
                        ptr.assign(new ElementAttributeValueTokenNode(grove, tokenizedValue, attIndex_, 0, chunk_));
                        return AccessResult.accessOK;
                    }
                    break;
                case AttributeValue.Type.cdata:
                    if (text != null)
                    {
                        TextIter iter = new TextIter(text);
                        if (!CdataAttributeValueNode.skipBoring(iter))
                        {
                            return AccessResult.accessNull;
                        }
                        ptr.assign(new ElementCdataAttributeValueNode(grove, value, attIndex_, iter, 0, chunk_));
                        return AccessResult.accessOK;
                    }
                    break;
            }
        }
        return AccessResult.accessNull;
    }

    public override AccessResult makeAttributeValueNodeList(GroveImpl grove, ref NodeListPtr ptr, AttributeValue? value)
    {
        NodePtr nodePtr = new NodePtr();
        AccessResult result = makeAttributeValueNode(grove, ref nodePtr, value);
        if (result == AccessResult.accessOK)
        {
            if (nodePtr.node == null)
                ptr.assign(new BaseNodeList());
            else
                ptr.assign(new SiblingNodeList(nodePtr));
        }
        return result;
    }

    public override AccessResult makeAttributeDefNode(GroveImpl grove, ref NodePtr ptr, nuint attributeDefIdx)
    {
        if (chunk_.elementType() == null)
            return AccessResult.accessNull;
        ptr.assign(new ElementTypeAttributeDefNode(grove, chunk_.elementType()!, attributeDefIdx));
        return AccessResult.accessOK;
    }
}

// Element CDATA attribute value node
public class ElementCdataAttributeValueNode : CdataAttributeValueNode
{
    protected ElementChunk chunk_;

    public ElementCdataAttributeValueNode(
        GroveImpl grove,
        AttributeValue? value,
        nuint attIndex,
        TextIter iter,
        nuint charIndex,
        ElementChunk chunk)
        : base(grove, value, attIndex, iter, charIndex)
    {
        chunk_ = chunk;
    }
}

// Element attribute value token node
public class ElementAttributeValueTokenNode : AttributeValueTokenNode
{
    protected ElementChunk chunk_;

    public ElementAttributeValueTokenNode(
        GroveImpl grove,
        TokenizedAttributeValue? value,
        nuint attIndex,
        nuint tokenIndex,
        ElementChunk chunk)
        : base(grove, value, attIndex, tokenIndex)
    {
        chunk_ = chunk;
    }
}

public class AttributeValueTokenNode : BaseNode
{
    protected TokenizedAttributeValue? value_;
    protected nuint tokenIndex_;
    protected nuint attIndex_;

    public AttributeValueTokenNode(
        GroveImpl grove,
        TokenizedAttributeValue? value,
        nuint attIndex,
        nuint tokenIndex) : base(grove)
    {
        value_ = value;
        attIndex_ = attIndex;
        tokenIndex_ = tokenIndex;
    }

    public override AccessResult getParent(ref NodePtr ptr)
    {
        ptr.assign(makeOriginNode(grove(), attIndex_));
        return AccessResult.accessOK;
    }

    protected virtual Node makeOriginNode(GroveImpl grove, nuint attIndex)
    {
        // To be implemented by derived classes
        return null!;
    }

    protected virtual AttributeDefinitionList? attDefList()
    {
        return null;
    }

    protected virtual object? attributeOriginId()
    {
        return null;
    }

    protected virtual Node makeAttributeValueTokenNode(GroveImpl grove, TokenizedAttributeValue? value, nuint attIndex, nuint tokenIndex)
    {
        return new AttributeValueTokenNode(grove, value, attIndex, tokenIndex);
    }

    public override AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        return followSiblingRef(0, ref ptr);
    }

    public override AccessResult followSiblingRef(uint n, ref NodePtr ptr)
    {
        // Do it like this to avoid possibility of overflow
        if (value_ == null || n >= value_.nTokens() - tokenIndex_ - 1)
            return AccessResult.accessNull;
        if (canReuse(ptr))
        {
            tokenIndex_ += n + 1;
        }
        else
            ptr.assign(makeAttributeValueTokenNode(grove(), value_, attIndex_,
                       tokenIndex_ + n + 1));
        return AccessResult.accessOK;
    }

    public override AccessResult firstSibling(ref NodePtr ptr)
    {
        if (canReuse(ptr))
            tokenIndex_ = 0;
        else
            ptr.assign(makeAttributeValueTokenNode(grove(), value_, attIndex_, 0));
        return AccessResult.accessOK;
    }

    public override AccessResult siblingsIndex(out uint index)
    {
        index = (uint)tokenIndex_;
        return AccessResult.accessOK;
    }

    public override AccessResult getToken(ref GroveString str)
    {
        if (value_ == null)
            return AccessResult.accessNull;
        nuint len;
        Char[]? chars;
        value_.token(tokenIndex_, out chars, out len);
        if (chars != null)
            str.assign(chars, len);
        return AccessResult.accessOK;
    }

    public override AccessResult getEntity(ref NodePtr ptr)
    {
        var defList = attDefList();
        if (defList == null || !defList.def(attIndex_)!.isEntity())
            return AccessResult.accessNull;
        if (value_ == null)
            return AccessResult.accessNull;
        StringC token = value_.token(tokenIndex_);
        var dtd = grove().governingDtd();
        if (dtd == null)
            return AccessResult.accessNull;
        Entity? entity = dtd.lookupEntityTemp(false, token);
        if (entity == null)
            return AccessResult.accessNull;
        ptr.assign(new EntityNode(grove(), entity));
        return AccessResult.accessOK;
    }

    public override AccessResult getNotation(ref NodePtr ptr)
    {
        var defList = attDefList();
        if (defList == null || !defList.def(attIndex_)!.isNotation())
            return AccessResult.accessNull;
        if (value_ == null)
            return AccessResult.accessNull;
        StringC token = value_.token(tokenIndex_);
        var dtd = grove().governingDtd();
        if (dtd == null)
            return AccessResult.accessNull;
        Notation? notation = dtd.lookupNotationTemp(token);
        if (notation == null)
            return AccessResult.accessNull;
        ptr.assign(new NotationNode(grove(), notation));
        return AccessResult.accessOK;
    }

    public override AccessResult getReferent(ref NodePtr ptr)
    {
        var defList = attDefList();
        if (defList == null || !defList.def(attIndex_)!.isIdref())
            return AccessResult.accessNull;
        if (value_ == null)
            return AccessResult.accessNull;
        StringC token = value_.token(tokenIndex_);
        for (;;)
        {
            Boolean complete = grove().complete();
            ElementChunk? element = grove().lookupElement(token);
            if (element != null)
            {
                ptr.assign(new ElementNode(grove(), element));
                break;
            }
            if (complete)
                return AccessResult.accessNull;
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        return AccessResult.accessOK;
    }

    public override AccessResult getLocation(ref Location location)
    {
        if (value_ == null)
            return AccessResult.accessNull;
        ConstPtr<Origin>? originP;
        Index index;
        if (!value_.tokenLocation(tokenIndex_, out originP, out index)
            && originP?.pointer() != null)
        {
            location = new Location(new GroveImplProxyOrigin(grove(), originP.pointer()), index);
            return AccessResult.accessOK;
        }
        return AccessResult.accessNull;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.attributeValueToken(this);
    }

    public override ClassDef classDef() { return ClassDef.attributeValueToken; }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idValue;
        return AccessResult.accessOK;
    }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(AttributeValueTokenNode? node)
    {
        if (node == null) return false;
        return (attributeOriginId() == node.attributeOriginId()
                && attIndex_ == node.attIndex_
                && tokenIndex_ == node.tokenIndex_);
    }

    public override uint hash()
    {
        return secondHash(secondHash((uint)((nuint)(attributeOriginId()?.GetHashCode() ?? 0) + attIndex_)) + (uint)tokenIndex_);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public class CdataAttributeValueNode : BaseNode
{
    protected AttributeValue? value_;
    protected TextIter iter_;
    protected nuint charIndex_;
    protected Char c_;
    protected nuint attIndex_;

    public CdataAttributeValueNode(
        GroveImpl grove,
        AttributeValue? value,
        nuint attIndex,
        TextIter iter,
        nuint charIndex) : base(grove)
    {
        value_ = value;
        attIndex_ = attIndex;
        iter_ = iter;
        charIndex_ = charIndex;
    }

    public static bool skipBoring(TextIter iter)
    {
        while (iter.valid())
        {
            switch (iter.type())
            {
                case TextItem.Type.data:
                case TextItem.Type.cdata:
                case TextItem.Type.sdata:
                {
                    nuint length;
                    iter.chars(out length);
                    if (length > 0)
                        return true;
                }
                break;
            }
            iter.advance();
        }
        return false;
    }

    public override AccessResult getParent(ref NodePtr ptr)
    {
        ptr.assign(makeOriginNode(grove(), attIndex_));
        return AccessResult.accessOK;
    }

    protected virtual Node makeOriginNode(GroveImpl grove, nuint attIndex)
    {
        // To be implemented by derived classes (ElementCdataAttributeValueNode, etc.)
        return null!;
    }

    public virtual object? attributeOriginId()
    {
        return null;
    }

    public override AccessResult charChunk(SdataMapper mapper, ref GroveString str)
    {
        if (iter_.type() == TextItem.Type.sdata)
        {
            var entityOrigin = iter_.location().origin().pointer()?.asEntityOrigin();
            if (entityOrigin != null)
            {
                var entity = entityOrigin.entity();
                if (entity != null)
                {
                    StringC name = entity.name();
                    var internalEntity = entity.asInternalEntity();
                    if (internalEntity != null)
                    {
                        StringC text = internalEntity.@string();
                        Char c;
                        if (mapper.sdataMap(new GroveString(name.data(), name.size()),
                                           new GroveString(text.data(), text.size()), out c))
                        {
                            c_ = c;
                            str.assign(new Char[] { c_ }, 1);
                            return AccessResult.accessOK;
                        }
                    }
                }
            }
            return AccessResult.accessNull;
        }
        // Regular data
        nuint length;
        Char[]? chars = iter_.chars(out length);
        if (chars != null)
        {
            str.assign(chars, charIndex_, length - charIndex_);
        }
        return AccessResult.accessOK;
    }

    public override bool chunkContains(Node nd)
    {
        if (!sameGrove(nd))
            return false;
        return ((BaseNode)nd).inChunk(this);
    }

    public override bool inChunk(CdataAttributeValueNode? node)
    {
        if (node == null) return false;
        nuint tem1, tem2;
        Char[]? chars1 = iter_.chars(out tem1);
        Char[]? chars2 = node.iter_.chars(out tem2);
        return (attributeOriginId() == node.attributeOriginId()
                && attIndex_ == node.attIndex_
                && chars1 == chars2
                && charIndex_ >= node.charIndex_);
    }

    public override AccessResult getEntity(ref NodePtr ptr)
    {
        if (iter_.type() != TextItem.Type.sdata)
            return AccessResult.accessNotInClass;
        var entityOrigin = iter_.location().origin().pointer()?.asEntityOrigin();
        if (entityOrigin == null)
            return AccessResult.accessNull;
        var entity = entityOrigin.entity();
        ptr.assign(new EntityNode(grove(), entity));
        return AccessResult.accessOK;
    }

    public override AccessResult getEntityName(ref GroveString str)
    {
        if (iter_.type() != TextItem.Type.sdata)
            return AccessResult.accessNotInClass;
        var entityOrigin = iter_.location().origin().pointer()?.asEntityOrigin();
        if (entityOrigin == null)
            return AccessResult.accessNull;
        var entity = entityOrigin.entity();
        setString(ref str, entity!.name());
        return AccessResult.accessOK;
    }

    public override AccessResult getSystemData(ref GroveString str)
    {
        if (iter_.type() != TextItem.Type.sdata)
            return AccessResult.accessNotInClass;
        nuint len;
        Char[]? chars = iter_.chars(out len);
        if (chars != null)
            str.assign(chars, len);
        return AccessResult.accessOK;
    }

    public override AccessResult nextSibling(ref NodePtr ptr)
    {
        if (iter_.type() != TextItem.Type.sdata)
        {
            nuint length;
            iter_.chars(out length);
            if (charIndex_ + 1 < length)
            {
                if (canReuse(ptr))
                    charIndex_++;
                else
                    ptr.assign(makeCdataAttributeValueNode(grove(), value_, attIndex_,
                        new TextIter(iter_), charIndex_ + 1));
                return AccessResult.accessOK;
            }
        }
        return nextChunkSibling(ref ptr);
    }

    protected virtual CdataAttributeValueNode makeCdataAttributeValueNode(
        GroveImpl grove, AttributeValue? value, nuint attIndex,
        TextIter iter, nuint charIndex)
    {
        return new CdataAttributeValueNode(grove, value, attIndex, iter, charIndex);
    }

    public override AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        TextIter copy = new TextIter(iter_);
        copy.advance();
        if (!skipBoring(copy))
            return AccessResult.accessNull;
        if (canReuse(ptr))
        {
            iter_ = copy;
            charIndex_ = 0;
        }
        else
            ptr.assign(makeCdataAttributeValueNode(grove(), value_, attIndex_, copy, 0));
        return AccessResult.accessOK;
    }

    public override AccessResult firstSibling(ref NodePtr ptr)
    {
        TextIter copy = new TextIter(iter_);
        copy.rewind();
        skipBoring(copy);
        if (canReuse(ptr))
        {
            iter_ = copy;
            charIndex_ = 0;
        }
        else
            ptr.assign(makeCdataAttributeValueNode(grove(), value_, attIndex_, copy, 0));
        return AccessResult.accessOK;
    }

    public override AccessResult siblingsIndex(out uint index)
    {
        TextIter copy = new TextIter(iter_);
        nuint tem;
        Char[]? iterChars = iter_.chars(out tem);
        copy.rewind();
        skipBoring(copy);
        index = 0;
        nuint copyTem;
        while (copy.chars(out copyTem) != iterChars)
        {
            if (copy.type() == TextItem.Type.sdata)
                index += 1;
            else
                index += (uint)copyTem;
            copy.advance();
            if (!skipBoring(copy))
                break;
        }
        index += (uint)charIndex_;
        return AccessResult.accessOK;
    }

    public override AccessResult getLocation(ref Location location)
    {
        if (iter_.type() == TextItem.Type.sdata)
            return grove().proxifyLocation(iter_.location().origin().pointer()!.parent(), out location);
        else
            return grove().proxifyLocation(iter_.location(), out location);
    }

    public override void accept(NodeVisitor visitor)
    {
        if (iter_.type() == TextItem.Type.sdata)
            visitor.sdata(this);
        else
            visitor.dataChar(this);
    }

    public override ClassDef classDef()
    {
        if (iter_.type() == TextItem.Type.sdata)
            return ClassDef.sdata;
        else
            return ClassDef.dataChar;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idValue;
        return AccessResult.accessOK;
    }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(CdataAttributeValueNode? node)
    {
        if (node == null) return false;
        nuint tem1, tem2;
        Char[]? chars1 = iter_.chars(out tem1);
        Char[]? chars2 = node.iter_.chars(out tem2);
        return (attributeOriginId() == node.attributeOriginId()
                && attIndex_ == node.attIndex_
                && charIndex_ == node.charIndex_
                && chars1 == chars2);
    }

    public override uint hash()
    {
        uint n;
        siblingsIndex(out n);
        return secondHash(secondHash((uint)((nuint)(attributeOriginId()?.GetHashCode() ?? 0) + attIndex_)) + n);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public abstract class EntityNodeBase : BaseNode
{
    protected Entity? entity_;

    public EntityNodeBase(GroveImpl grove, Entity? entity) : base(grove)
    {
        entity_ = entity;
    }

    public override AccessResult getName(ref GroveString str)
    {
        if (entity_ == null)
            return AccessResult.accessNull;
        setString(ref str, entity_.name());
        return AccessResult.accessOK;
    }

    public override AccessResult getExternalId(ref NodePtr ptr)
    {
        if (entity_ == null)
            return AccessResult.accessNull;
        var x = entity_.asExternalEntity();
        if (x == null)
            return AccessResult.accessNull;
        ptr.assign(new EntityExternalIdNode(grove(), x));
        return AccessResult.accessOK;
    }

    public override AccessResult getNotation(ref NodePtr ptr)
    {
        if (entity_ == null)
            return AccessResult.accessNull;
        var x = entity_.asExternalDataEntity();
        if (x == null || x.notation() == null)
            return AccessResult.accessNull;
        ptr.assign(new NotationNode(grove(), x.notation()));
        return AccessResult.accessOK;
    }

    public override AccessResult getNotationName(ref GroveString str)
    {
        if (entity_ == null)
            return AccessResult.accessNull;
        var x = entity_.asExternalDataEntity();
        if (x == null || x.notation() == null)
            return AccessResult.accessNull;
        setString(ref str, x.notation().name());
        return AccessResult.accessOK;
    }

    public override AccessResult getText(ref GroveString str)
    {
        if (entity_ == null)
            return AccessResult.accessNull;
        var i = entity_.asInternalEntity();
        if (i == null)
            return AccessResult.accessNull;
        setString(ref str, i.@string());
        return AccessResult.accessOK;
    }

    public override AccessResult getEntityType(out EntityType.Enum type)
    {
        type = EntityType.Enum.text;
        if (entity_ == null)
            return AccessResult.accessNull;
        switch (entity_.dataType())
        {
            case EntityDecl.DataType.sgmlText:
                type = EntityType.Enum.text;
                break;
            case EntityDecl.DataType.pi:
                type = EntityType.Enum.pi;
                break;
            case EntityDecl.DataType.cdata:
                type = EntityType.Enum.cdata;
                break;
            case EntityDecl.DataType.sdata:
                type = EntityType.Enum.sdata;
                break;
            case EntityDecl.DataType.ndata:
                type = EntityType.Enum.ndata;
                break;
            case EntityDecl.DataType.subdoc:
                type = EntityType.Enum.subdocument;
                break;
            default:
                type = EntityType.Enum.text;
                break;
        }
        return AccessResult.accessOK;
    }

    public override AccessResult getAttributes(ref NamedNodeListPtr ptr)
    {
        if (entity_ == null)
            return AccessResult.accessNull;
        var x = entity_.asExternalDataEntity();
        if (x == null)
            return AccessResult.accessNull;
        // Would create EntityAttributesNamedNodeList
        return AccessResult.accessNull;
    }

    public override AccessResult attributeRef(uint i, ref NodePtr ptr)
    {
        if (entity_ == null)
            return AccessResult.accessNull;
        var x = entity_.asExternalDataEntity();
        if (x == null || i >= x.attributes().size())
            return AccessResult.accessNull;
        // Would create EntityAttributeAsgnNode
        return AccessResult.accessNull;
    }

    public override AccessResult getLocation(ref Location location)
    {
        if (entity_ == null)
            return AccessResult.accessNull;
        return grove().proxifyLocation(entity_.defLocation(), out location);
    }

    public override uint hash()
    {
        return (uint)(entity_?.GetHashCode() ?? 0);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public class EntityNode : EntityNodeBase
{
    public EntityNode(GroveImpl grove, Entity? entity) : base(grove, entity)
    {
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        if (entity_ != null && entity_.defaulted())
            ptr.assign(new SgmlDocumentNode(grove(), grove().root()));
        else
            ptr.assign(new DocumentTypeNode(grove(), grove().governingDtd()));
        return AccessResult.accessOK;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id id)
    {
        if (entity_ != null && entity_.defaulted())
            id = ComponentName.Id.idDefaultedEntities;
        else
            id = ComponentName.Id.idGeneralEntities;
        return AccessResult.accessOK;
    }

    public override AccessResult getDefaulted(out bool defaulted)
    {
        defaulted = entity_?.defaulted() ?? false;
        return AccessResult.accessOK;
    }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(EntityNode? node)
    {
        return node != null && entity_ == node.entity_;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.entity(this);
    }

    public override ClassDef classDef() { return ClassDef.entity; }
}

public class DefaultEntityNode : EntityNodeBase
{
    public DefaultEntityNode(GroveImpl grove, Entity? entity) : base(grove, entity)
    {
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        ptr.assign(new DocumentTypeNode(grove(), grove().governingDtd()));
        return AccessResult.accessOK;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id id)
    {
        id = ComponentName.Id.idDefaultEntity;
        return AccessResult.accessOK;
    }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(DefaultEntityNode? node)
    {
        return node != null && entity_ == node.entity_;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.defaultEntity(this);
    }

    public override ClassDef classDef() { return ClassDef.defaultEntity; }
}

public class NotationNode : BaseNode
{
    private Notation? notation_;

    public NotationNode(GroveImpl grove, Notation? notation) : base(grove)
    {
        notation_ = notation;
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        ptr.assign(new DocumentTypeNode(grove(), grove().governingDtd()));
        return AccessResult.accessOK;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idNotations;
        return AccessResult.accessOK;
    }

    public override AccessResult getName(ref GroveString str)
    {
        if (notation_ == null)
            return AccessResult.accessNull;
        setString(ref str, notation_.name());
        return AccessResult.accessOK;
    }

    public override AccessResult getExternalId(ref NodePtr ptr)
    {
        if (notation_ == null)
            return AccessResult.accessNull;
        ptr.assign(new NotationExternalIdNode(grove(), notation_));
        return AccessResult.accessOK;
    }

    public override AccessResult getAttributeDefs(ref NamedNodeListPtr ptr)
    {
        if (notation_ == null)
            return AccessResult.accessNull;
        ptr.assign(new NotationAttributeDefsNamedNodeList(grove(), notation_));
        return AccessResult.accessOK;
    }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(NotationNode? node)
    {
        return node != null && notation_ == node.notation_;
    }

    public override AccessResult getLocation(ref Location location)
    {
        if (notation_ == null)
            return AccessResult.accessNull;
        return grove().proxifyLocation(notation_.defLocation(), out location);
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.notation(this);
    }

    public override ClassDef classDef() { return ClassDef.notation; }

    public override uint hash()
    {
        return (uint)(notation_?.GetHashCode() ?? 0);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public abstract class ExternalIdNode : BaseNode
{
    public ExternalIdNode(GroveImpl grove) : base(grove)
    {
    }

    public abstract ExternalId externalId();

    public override AccessResult getPublicId(ref GroveString str)
    {
        var eid = externalId();
        if (eid == null)
            return AccessResult.accessNull;
        var s = eid.publicIdString();
        if (s == null)
            return AccessResult.accessNull;
        setString(ref str, s);
        return AccessResult.accessOK;
    }

    public override AccessResult getSystemId(ref GroveString str)
    {
        var eid = externalId();
        if (eid == null)
            return AccessResult.accessNull;
        var s = eid.systemIdString();
        if (s == null)
            return AccessResult.accessNull;
        setString(ref str, s);
        return AccessResult.accessOK;
    }

    public override AccessResult getGeneratedSystemId(ref GroveString str)
    {
        var eid = externalId();
        if (eid == null)
            return AccessResult.accessNull;
        var s = eid.effectiveSystemId();
        if (s.size() == 0)
            return AccessResult.accessNull;
        setString(ref str, s);
        return AccessResult.accessOK;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.externalId(this);
    }

    public override ClassDef classDef() { return ClassDef.externalId; }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idExternalId;
        return AccessResult.accessOK;
    }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(ExternalIdNode? node)
    {
        if (node == null)
            return false;
        return externalId() == node.externalId();
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public class EntityExternalIdNode : ExternalIdNode
{
    private ExternalEntity entity_;

    public EntityExternalIdNode(GroveImpl grove, ExternalEntity entity) : base(grove)
    {
        entity_ = entity;
    }

    public override ExternalId externalId()
    {
        return entity_.externalId();
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        ptr.assign(new EntityNode(grove(), entity_));
        return AccessResult.accessOK;
    }

    public override uint hash()
    {
        return secondHash((uint)entity_.GetHashCode());
    }
}

public class NotationExternalIdNode : ExternalIdNode
{
    private Notation notation_;

    public NotationExternalIdNode(GroveImpl grove, Notation notation) : base(grove)
    {
        notation_ = notation;
    }

    public override ExternalId externalId()
    {
        return notation_.externalId();
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        ptr.assign(new NotationNode(grove(), notation_));
        return AccessResult.accessOK;
    }

    public override uint hash()
    {
        return secondHash((uint)notation_.GetHashCode());
    }
}

public class DocumentTypeNode : BaseNode
{
    private Dtd? dtd_;

    public DocumentTypeNode(GroveImpl grove, Dtd? dtd) : base(grove)
    {
        dtd_ = dtd;
    }

    public override AccessResult getName(ref GroveString str)
    {
        if (dtd_ == null)
            return AccessResult.accessNull;
        setString(ref str, dtd_.name());
        return AccessResult.accessOK;
    }

    public override AccessResult getGoverning(out bool governing)
    {
        governing = dtd_?.isBase() ?? false;
        return AccessResult.accessOK;
    }

    public override AccessResult getGeneralEntities(ref NamedNodeListPtr ptr)
    {
        if (dtd_ == null)
            return AccessResult.accessNull;
        ptr.assign(new GeneralEntitiesNamedNodeList(grove(), dtd_));
        return AccessResult.accessOK;
    }

    public override AccessResult getNotations(ref NamedNodeListPtr ptr)
    {
        if (dtd_ == null)
            return AccessResult.accessNull;
        ptr.assign(new NotationsNamedNodeList(grove(), dtd_));
        return AccessResult.accessOK;
    }

    public override AccessResult getElementTypes(ref NamedNodeListPtr ptr)
    {
        if (dtd_ == null)
            return AccessResult.accessNull;
        ptr.assign(new ElementTypesNamedNodeList(grove(), dtd_));
        return AccessResult.accessOK;
    }

    public override AccessResult getDefaultEntity(ref NodePtr ptr)
    {
        if (dtd_ == null)
            return AccessResult.accessNull;
        var entity = dtd_.defaultEntityTemp();
        if (entity == null)
            return AccessResult.accessNull;
        ptr.assign(new DefaultEntityNode(grove(), entity));
        return AccessResult.accessOK;
    }

    public override AccessResult getParameterEntities(ref NamedNodeListPtr ptr)
    {
        if (dtd_ == null)
            return AccessResult.accessNull;
        ptr.assign(new ParameterEntitiesNamedNodeList(grove(), dtd_));
        return AccessResult.accessOK;
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        ptr.assign(new SgmlDocumentNode(grove(), grove().root()));
        return AccessResult.accessOK;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idDoctypesAndLinktypes;
        return AccessResult.accessOK;
    }

    public override AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        return AccessResult.accessNull;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.documentType(this);
    }

    public override ClassDef classDef() { return ClassDef.documentType; }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(DocumentTypeNode? node)
    {
        return node != null && dtd_ == node.dtd_;
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public class SgmlConstantsNode : BaseNode
{
    public SgmlConstantsNode(GroveImpl grove) : base(grove)
    {
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        ptr.assign(new SgmlDocumentNode(grove(), grove().root()));
        return AccessResult.accessOK;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.sgmlConstants(this);
    }

    public override ClassDef classDef() { return ClassDef.sgmlConstants; }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idSgmlConstants;
        return AccessResult.accessOK;
    }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(SgmlConstantsNode? node)
    {
        return node != null; // same2 returns true for any SgmlConstantsNode
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public class MessageNode : BaseNode
{
    private MessageItem? item_;

    public MessageNode(GroveImpl grove, MessageItem? item) : base(grove)
    {
        item_ = item;
    }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        ptr.assign(new SgmlDocumentNode(grove(), grove().root()));
        return AccessResult.accessOK;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        // Messages property
        name = ComponentName.Id.noId; // Messages don't have a specific property ID
        return AccessResult.accessOK;
    }

    public override AccessResult nextChunkSibling(ref NodePtr ptr)
    {
        if (item_ == null)
            return AccessResult.accessNull;
        while (item_.next() == null)
        {
            if (grove().complete())
                return AccessResult.accessNull;
            if (!grove().waitForMoreNodes())
                return AccessResult.accessTimeout;
        }
        ptr.assign(new MessageNode(grove(), item_.next()));
        return AccessResult.accessOK;
    }

    public override AccessResult firstSibling(ref NodePtr ptr)
    {
        ptr.assign(new MessageNode(grove(), grove().messageList()));
        return AccessResult.accessOK;
    }

    public override AccessResult siblingsIndex(out uint index)
    {
        index = 0;
        for (var p = grove().messageList(); p != item_; p = p?.next())
            index++;
        return AccessResult.accessOK;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.message(this);
    }

    public override ClassDef classDef() { return ClassDef.message; }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(MessageNode? node)
    {
        return node != null && item_ == node.item_;
    }

    public override AccessResult getLocation(ref Location location)
    {
        if (item_ == null)
            return AccessResult.accessNull;
        return grove().proxifyLocation(item_.loc(), out location);
    }

    public override AccessResult getText(ref GroveString str)
    {
        if (item_ == null)
            return AccessResult.accessNull;
        setString(ref str, item_.text());
        return AccessResult.accessOK;
    }

    public override AccessResult getSeverity(out Severity severity)
    {
        severity = item_?.severity() ?? Severity.info;
        return AccessResult.accessOK;
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public class ElementTypeNode : BaseNode
{
    protected ElementType? elementType_;

    public ElementTypeNode(GroveImpl grove, ElementType? elementType) : base(grove)
    {
        elementType_ = elementType;
    }

    public ElementType? elementType() { return elementType_; }

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        ptr.assign(new DocumentTypeNode(grove(), grove().governingDtd()));
        return AccessResult.accessOK;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idElementTypes;
        return AccessResult.accessOK;
    }

    public override AccessResult getGi(ref GroveString str)
    {
        if (elementType_ == null)
            return AccessResult.accessNull;
        setString(ref str, elementType_.name());
        return AccessResult.accessOK;
    }

    public override AccessResult getContentType(out ContentType.Enum type)
    {
        type = ContentType.Enum.modelgrp;
        if (elementType_ == null)
            return AccessResult.accessNull;
        var def = elementType_.definition();
        if (def == null)
            return AccessResult.accessNull;
        switch (def.declaredContent())
        {
            case ElementDefinition.DeclaredContent.modelGroup:
                type = ContentType.Enum.modelgrp;
                break;
            case ElementDefinition.DeclaredContent.any:
                type = ContentType.Enum.any;
                break;
            case ElementDefinition.DeclaredContent.cdata:
                type = ContentType.Enum.cdata;
                break;
            case ElementDefinition.DeclaredContent.rcdata:
                type = ContentType.Enum.rcdata;
                break;
            case ElementDefinition.DeclaredContent.empty:
                type = ContentType.Enum.empty;
                break;
            default:
                type = ContentType.Enum.modelgrp;
                break;
        }
        return AccessResult.accessOK;
    }

    public override AccessResult getExclusions(ref GroveStringListPtr ptr)
    {
        // TODO: Implement exclusions list
        return AccessResult.accessNull;
    }

    public override AccessResult getInclusions(ref GroveStringListPtr ptr)
    {
        // TODO: Implement inclusions list
        return AccessResult.accessNull;
    }

    public override AccessResult getModelGroup(ref NodePtr ptr)
    {
        // TODO: Implement model group
        return AccessResult.accessNull;
    }

    public override AccessResult getOmitEndTag(out bool omit)
    {
        omit = false;
        if (elementType_ == null)
            return AccessResult.accessNull;
        var def = elementType_.definition();
        if (def == null || !def.omittedTagSpec())
            return AccessResult.accessNull;
        omit = def.canOmitEndTag();
        return AccessResult.accessOK;
    }

    public override AccessResult getOmitStartTag(out bool omit)
    {
        omit = false;
        if (elementType_ == null)
            return AccessResult.accessNull;
        var def = elementType_.definition();
        if (def == null || !def.omittedTagSpec())
            return AccessResult.accessNull;
        omit = def.canOmitStartTag();
        return AccessResult.accessOK;
    }

    public override AccessResult getAttributeDefs(ref NamedNodeListPtr ptr)
    {
        if (elementType_ == null)
            return AccessResult.accessNull;
        ptr.assign(new ElementTypeAttributeDefsNamedNodeList(grove(), elementType_));
        return AccessResult.accessOK;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.elementType(this);
    }

    public override ClassDef classDef() { return ClassDef.elementType; }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(ElementTypeNode? node)
    {
        return node != null && elementType_ == node.elementType_;
    }

    public override uint hash()
    {
        return (uint)(elementType_?.GetHashCode() ?? 0);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        AccessResult ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public class ModelGroupNode : BaseNode
{
    protected ElementType? elementType_;
    protected ModelGroup? modelGroup_;
    protected ModelGroupNode? parentModelGroupNode_;

    public ModelGroupNode(GroveImpl grove) : base(grove)
    {
    }

    public ModelGroupNode(GroveImpl grove, ElementType elementType, ModelGroup modelGroup, ModelGroupNode? parent = null)
        : base(grove)
    {
        elementType_ = elementType;
        modelGroup_ = modelGroup;
        parentModelGroupNode_ = parent;
    }

    public ModelGroup? modelGroup() { return modelGroup_; }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = parentModelGroupNode_ == null
            ? ComponentName.Id.idModelGroup
            : ComponentName.Id.idContentTokens;
        return AccessResult.accessOK;
    }

    public override AccessResult getConnector(out Connector.Enum connector)
    {
        connector = Connector.Enum.seq;
        if (modelGroup_ == null)
            return AccessResult.accessNull;
        // TODO: Map modelGroup_.connector() to enum when ModelGroup is implemented
        return AccessResult.accessOK;
    }

    public override AccessResult getOccurIndicator(out OccurIndicator.Enum indicator)
    {
        indicator = OccurIndicator.Enum.opt;
        if (modelGroup_ == null)
            return AccessResult.accessNull;
        // TODO: Map modelGroup_.occurrenceIndicator() to enum
        return AccessResult.accessNull;
    }

    public override AccessResult getContentTokens(ref NodeListPtr ptr)
    {
        // TODO: Implement ContentTokenNodeList
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.modelGroup(this);
    }

    public override ClassDef classDef() { return ClassDef.modelGroup; }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(ModelGroupNode? node)
    {
        return node != null && modelGroup_ == node.modelGroup_;
    }

    public override uint hash()
    {
        return (uint)(modelGroup_?.GetHashCode() ?? 0);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        return getContentTokens(ref ptr);
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        var ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public class ElementTokenNode : BaseNode
{
    protected ElementType? elementType_;
    protected ElementToken? elementToken_;
    protected ModelGroupNode? parentModelGroupNode_;

    public ElementTokenNode(GroveImpl grove) : base(grove)
    {
    }

    public ElementTokenNode(GroveImpl grove, ElementType elementType, ElementToken elementToken, ModelGroupNode? parent = null)
        : base(grove)
    {
        elementType_ = elementType;
        elementToken_ = elementToken;
        parentModelGroupNode_ = parent;
    }

    public ElementToken? elementToken() { return elementToken_; }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idContentTokens;
        return AccessResult.accessOK;
    }

    public override AccessResult getGi(ref GroveString str)
    {
        if (elementToken_ == null || elementToken_.elementType() == null)
            return AccessResult.accessNull;
        setString(ref str, elementToken_.elementType()!.name());
        return AccessResult.accessOK;
    }

    public override AccessResult getOccurIndicator(out OccurIndicator.Enum indicator)
    {
        indicator = OccurIndicator.Enum.opt;
        if (elementToken_ == null)
            return AccessResult.accessNull;
        // TODO: Map elementToken_.occurrenceIndicator() to enum
        return AccessResult.accessNull;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.elementToken(this);
    }

    public override ClassDef classDef() { return ClassDef.elementToken; }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(ElementTokenNode? node)
    {
        return node != null && elementToken_ == node.elementToken_;
    }

    public override uint hash()
    {
        return (uint)(elementToken_?.GetHashCode() ?? 0);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        var ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public class PcdataTokenNode : BaseNode
{
    protected ElementType? elementType_;
    protected PcdataToken? pcdataToken_;
    protected ModelGroupNode? parentModelGroupNode_;

    public PcdataTokenNode(GroveImpl grove) : base(grove)
    {
    }

    public PcdataTokenNode(GroveImpl grove, ElementType elementType, PcdataToken pcdataToken, ModelGroupNode? parent = null)
        : base(grove)
    {
        elementType_ = elementType;
        pcdataToken_ = pcdataToken;
        parentModelGroupNode_ = parent;
    }

    public PcdataToken? pcdataToken() { return pcdataToken_; }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idContentTokens;
        return AccessResult.accessOK;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.pcdataToken(this);
    }

    public override ClassDef classDef() { return ClassDef.pcdataToken; }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(PcdataTokenNode? node)
    {
        return node != null && pcdataToken_ == node.pcdataToken_;
    }

    public override uint hash()
    {
        return (uint)(pcdataToken_?.GetHashCode() ?? 0);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        var ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

public abstract class AttributeDefNode : BaseNode
{
    protected nuint attIndex_;

    public AttributeDefNode(GroveImpl grove, nuint attIndex) : base(grove)
    {
        attIndex_ = attIndex;
    }

    // Abstract methods that derived classes must implement
    public abstract AttributeDefinitionList? attDefList();
    public abstract Node makeOriginNode(GroveImpl grove, nuint attIndex);
    public abstract object? attributeOriginId();

    public override AccessResult getOrigin(ref NodePtr ptr)
    {
        ptr.assign(makeOriginNode(grove(), attIndex_));
        return AccessResult.accessOK;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idAttributeDefs;
        return AccessResult.accessOK;
    }

    public override AccessResult getName(ref GroveString str)
    {
        var defList = attDefList();
        if (defList == null)
            return AccessResult.accessNull;
        var def = defList.def(attIndex_);
        if (def == null)
            return AccessResult.accessNull;
        setString(ref str, def.name());
        return AccessResult.accessOK;
    }

    public override AccessResult getDeclValueType(out DeclValueType.Enum type)
    {
        type = DeclValueType.Enum.cdata;
        // TODO: Implement full getDeclValueType
        return AccessResult.accessNull;
    }

    public override AccessResult getDefaultValueType(out DefaultValueType.Enum type)
    {
        type = DefaultValueType.Enum.implied;
        // TODO: Implement full getDefaultValueType
        return AccessResult.accessNull;
    }

    public override AccessResult getTokens(ref GroveStringListPtr ptr)
    {
        // TODO: Implement getTokens
        return AccessResult.accessNull;
    }

    public override AccessResult getCurrentAttributeIndex(out long index)
    {
        index = (long)attIndex_;
        return AccessResult.accessOK;
    }

    public override AccessResult getCurrentGroup(ref NodeListPtr ptr)
    {
        // TODO: Implement getCurrentGroup
        return AccessResult.accessNull;
    }

    public override AccessResult getDefaultValue(ref NodeListPtr ptr)
    {
        // TODO: Implement getDefaultValue
        return AccessResult.accessNull;
    }

    public override void accept(NodeVisitor visitor)
    {
        visitor.attributeDef(this);
    }

    public override ClassDef classDef() { return ClassDef.attributeDef; }

    public override bool same(BaseNode node)
    {
        return node.same2(this);
    }

    public override bool same2(AttributeDefNode? node)
    {
        if (node == null)
            return false;
        return attributeOriginId() == node.attributeOriginId() && attIndex_ == node.attIndex_;
    }

    public override uint hash()
    {
        uint n = (uint)(attributeOriginId()?.GetHashCode() ?? 0);
        return secondHash(n + (uint)attIndex_);
    }

    public override AccessResult children(ref NodeListPtr ptr)
    {
        ptr.assign(new BaseNodeList());
        return AccessResult.accessOK;
    }

    public override AccessResult follow(ref NodeListPtr ptr)
    {
        NodePtr nd = new NodePtr();
        var ret = nextSibling(ref nd);
        switch (ret)
        {
            case AccessResult.accessOK:
                ptr.assign(new SiblingNodeList(nd));
                break;
            case AccessResult.accessNull:
                ptr.assign(new BaseNodeList());
                ret = AccessResult.accessOK;
                break;
        }
        return ret;
    }
}

// Element type attribute def node
public class ElementTypeAttributeDefNode : AttributeDefNode
{
    protected ElementType elementType_;

    public ElementTypeAttributeDefNode(GroveImpl grove, ElementType elementType, nuint attributeDefIdx)
        : base(grove, attributeDefIdx)
    {
        elementType_ = elementType;
    }

    public override AttributeDefinitionList? attDefList()
    {
        return elementType_.attributeDefTemp();
    }

    public override Node makeOriginNode(GroveImpl grove, nuint attIndex)
    {
        return new ElementTypeNode(grove, elementType_);
    }

    public override object? attributeOriginId()
    {
        return elementType_;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idAttributeDefs;
        return AccessResult.accessOK;
    }

    public override AccessResult getCurrentGroup(ref NodeListPtr ptr)
    {
        // Would implement attribute group node list
        return AccessResult.accessNull;
    }

    public override AccessResult getLocation(ref Location location)
    {
        // AttributeDefinition doesn't expose location in C# port
        return AccessResult.accessNull;
    }

    public override AccessResult getDefaultValue(ref NodeListPtr ptr)
    {
        // Would implement default value node list
        return AccessResult.accessNull;
    }
}

// Element type attribute defs node list
public class ElementTypeAttributeDefsNodeList : BaseNodeList
{
    private GroveImpl grove_;
    private ElementType elementType_;
    private nuint firstAttDefIdx_;

    public ElementTypeAttributeDefsNodeList(GroveImpl grove, ElementType elementType, nuint firstAttDefIdx)
    {
        grove_ = grove;
        elementType_ = elementType;
        firstAttDefIdx_ = firstAttDefIdx;
    }

    public override AccessResult first(ref NodePtr ptr)
    {
        var defList = elementType_.attributeDefTemp();
        if (defList == null || firstAttDefIdx_ >= defList.size())
            return AccessResult.accessNull;
        ptr.assign(new ElementTypeAttributeDefNode(grove_, elementType_, firstAttDefIdx_));
        return AccessResult.accessOK;
    }

    public override AccessResult chunkRest(ref NodeListPtr ptr)
    {
        var defList = elementType_.attributeDefTemp();
        nuint nextIdx = firstAttDefIdx_ + 1;
        if (defList == null || nextIdx >= defList.size())
            return AccessResult.accessNull;
        ptr.assign(new ElementTypeAttributeDefsNodeList(grove_, elementType_, nextIdx));
        return AccessResult.accessOK;
    }
}

// Element type cdata attribute value node
public class ElementTypeCdataAttributeValueNode : CdataAttributeValueNode
{
    protected ElementType elementType_;

    public ElementTypeCdataAttributeValueNode(
        GroveImpl grove,
        AttributeValue? value,
        nuint attIndex,
        TextIter iter,
        nuint charIndex,
        ElementType elementType)
        : base(grove, value, attIndex, iter, charIndex)
    {
        elementType_ = elementType;
    }
}

// Element type attribute value token node
public class ElementTypeAttributeValueTokenNode : AttributeValueTokenNode
{
    protected ElementType elementType_;

    public ElementTypeAttributeValueTokenNode(
        GroveImpl grove,
        TokenizedAttributeValue? value,
        nuint attIndex,
        nuint tokenIndex,
        ElementType elementType)
        : base(grove, value, attIndex, tokenIndex)
    {
        elementType_ = elementType;
    }
}

// Notation attribute def node
public class NotationAttributeDefNode : AttributeDefNode
{
    protected Notation notation_;

    public NotationAttributeDefNode(GroveImpl grove, Notation notation, nuint attributeDefIdx)
        : base(grove, attributeDefIdx)
    {
        notation_ = notation;
    }

    public override AttributeDefinitionList? attDefList()
    {
        return notation_.attributeDefTemp();
    }

    public override Node makeOriginNode(GroveImpl grove, nuint attIndex)
    {
        return new NotationNode(grove, notation_);
    }

    public override object? attributeOriginId()
    {
        return notation_;
    }

    public override AccessResult getOriginToSubnodeRelPropertyName(out ComponentName.Id name)
    {
        name = ComponentName.Id.idAttributeDefs;
        return AccessResult.accessOK;
    }

    public override AccessResult getCurrentGroup(ref NodeListPtr ptr)
    {
        return AccessResult.accessNull;
    }

    public override AccessResult getLocation(ref Location location)
    {
        return AccessResult.accessNull;
    }

    public override AccessResult getDefaultValue(ref NodeListPtr ptr)
    {
        return AccessResult.accessNull;
    }
}

// Notation attribute defs node list
public class NotationAttributeDefsNodeList : BaseNodeList
{
    private GroveImpl grove_;
    private Notation notation_;
    private nuint firstAttDefIdx_;

    public NotationAttributeDefsNodeList(GroveImpl grove, Notation notation, nuint firstAttDefIdx)
    {
        grove_ = grove;
        notation_ = notation;
        firstAttDefIdx_ = firstAttDefIdx;
    }

    public override AccessResult first(ref NodePtr ptr)
    {
        var defList = notation_.attributeDefTemp();
        if (defList == null || firstAttDefIdx_ >= defList.size())
            return AccessResult.accessNull;
        ptr.assign(new NotationAttributeDefNode(grove_, notation_, firstAttDefIdx_));
        return AccessResult.accessOK;
    }

    public override AccessResult chunkRest(ref NodeListPtr ptr)
    {
        var defList = notation_.attributeDefTemp();
        nuint nextIdx = firstAttDefIdx_ + 1;
        if (defList == null || nextIdx >= defList.size())
            return AccessResult.accessNull;
        ptr.assign(new NotationAttributeDefsNodeList(grove_, notation_, nextIdx));
        return AccessResult.accessOK;
    }
}

// Notation cdata attribute value node
public class NotationCdataAttributeValueNode : CdataAttributeValueNode
{
    protected Notation notation_;

    public NotationCdataAttributeValueNode(
        GroveImpl grove,
        AttributeValue? value,
        nuint attIndex,
        TextIter iter,
        nuint charIndex,
        Notation notation)
        : base(grove, value, attIndex, iter, charIndex)
    {
        notation_ = notation;
    }
}

// Notation attribute value token node
public class NotationAttributeValueTokenNode : AttributeValueTokenNode
{
    protected Notation notation_;

    public NotationAttributeValueTokenNode(
        GroveImpl grove,
        TokenizedAttributeValue? value,
        nuint attIndex,
        nuint tokenIndex,
        Notation notation)
        : base(grove, value, attIndex, tokenIndex)
    {
        notation_ = notation;
    }
}

// Base class for named node lists
public abstract class BaseNamedNodeList : NamedNodeList
{
    protected GroveImpl grove_;
    protected SubstTable? substTable_;
    private uint refCount_;

    public BaseNamedNodeList(GroveImpl grove, SubstTable? substTable)
    {
        grove_ = grove;
        substTable_ = substTable;
        refCount_ = 0;
    }

    public override void addRef() { ++refCount_; }

    public bool canReuse(NamedNodeListPtr ptr)
    {
        return ptr.list == this && refCount_ == 1;
    }

    public override void release()
    {
        System.Diagnostics.Debug.Assert(refCount_ != 0);
        --refCount_;
    }

    public override nuint normalize(uint[] s, nuint n)
    {
        if (substTable_ != null)
        {
            for (nuint i = 0; i < n; i++)
                substTable_.subst(ref s[i]);
        }
        return n;
    }

    public GroveImpl grove() { return grove_; }

    public override AccessResult namedNode(GroveString str, ref NodePtr node)
    {
        StringC tem = new StringC(str.data() ?? Array.Empty<uint>(), str.size());
        var chars = tem.data()?.ToArray() ?? Array.Empty<uint>();
        normalize(chars, (nuint)chars.Length);
        for (int i = 0; i < chars.Length; i++)
            tem[(nuint)i] = chars[i];
        return namedNodeU(tem, ref node);
    }

    public abstract AccessResult namedNodeU(StringC str, ref NodePtr ptr);
}

// General entities named node list
public class GeneralEntitiesNamedNodeList : BaseNamedNodeList
{
    private Dtd dtd_;

    public GeneralEntitiesNamedNodeList(GroveImpl grove, Dtd dtd)
        : base(grove, grove.entitySubstTable())
    {
        dtd_ = dtd;
    }

    public override NodeListPtr nodeList()
    {
        return new NodeListPtr(new EntitiesNodeList(grove_, dtd_.generalEntityIter()));
    }

    public override AccessResult namedNodeU(StringC str, ref NodePtr ptr)
    {
        var entity = dtd_.lookupEntityTemp(false, str);
        if (entity == null)
            return AccessResult.accessNull;
        ptr.assign(new EntityNode(grove_, entity));
        return AccessResult.accessOK;
    }

    public override Type type() { return Type.entities; }
}

// Parameter entities named node list
public class ParameterEntitiesNamedNodeList : BaseNamedNodeList
{
    private Dtd dtd_;

    public ParameterEntitiesNamedNodeList(GroveImpl grove, Dtd dtd)
        : base(grove, grove.entitySubstTable())
    {
        dtd_ = dtd;
    }

    public override NodeListPtr nodeList()
    {
        return new NodeListPtr(new EntitiesNodeList(grove_, dtd_.parameterEntityIter()));
    }

    public override AccessResult namedNodeU(StringC str, ref NodePtr ptr)
    {
        var entity = dtd_.lookupEntityTemp(true, str);
        if (entity == null)
            return AccessResult.accessNull;
        ptr.assign(new EntityNode(grove_, entity));
        return AccessResult.accessOK;
    }

    public override Type type() { return Type.entities; }
}

// Notations named node list
public class NotationsNamedNodeList : BaseNamedNodeList
{
    private Dtd dtd_;

    public NotationsNamedNodeList(GroveImpl grove, Dtd dtd)
        : base(grove, grove.generalSubstTable())
    {
        dtd_ = dtd;
    }

    public override NodeListPtr nodeList()
    {
        return new NodeListPtr(new NotationsNodeList(grove_, dtd_.notationIter()));
    }

    public override AccessResult namedNodeU(StringC str, ref NodePtr ptr)
    {
        var notation = dtd_.lookupNotationTemp(str);
        if (notation == null)
            return AccessResult.accessNull;
        ptr.assign(new NotationNode(grove_, notation));
        return AccessResult.accessOK;
    }

    public override Type type() { return Type.notations; }
}

// Element types named node list
public class ElementTypesNamedNodeList : BaseNamedNodeList
{
    private Dtd dtd_;

    public ElementTypesNamedNodeList(GroveImpl grove, Dtd dtd)
        : base(grove, grove.generalSubstTable())
    {
        dtd_ = dtd;
    }

    public override NodeListPtr nodeList()
    {
        return new NodeListPtr(new ElementTypesNodeList(grove_, dtd_.elementTypeIter()));
    }

    public override AccessResult namedNodeU(StringC str, ref NodePtr ptr)
    {
        var elementType = dtd_.lookupElementType(str);
        if (elementType == null)
            return AccessResult.accessNull;
        ptr.assign(new ElementTypeNode(grove_, elementType));
        return AccessResult.accessOK;
    }

    public override Type type() { return Type.elementTypes; }
}

// Entities node list (iterator-based)
public class EntitiesNodeList : BaseNodeList
{
    private GroveImpl grove_;
    private NamedResourceTableIter<Entity> iter_;

    public EntitiesNodeList(GroveImpl grove, NamedResourceTableIter<Entity> iter)
    {
        grove_ = grove;
        iter_ = iter;
    }

    public override AccessResult first(ref NodePtr ptr)
    {
        var entityPtr = iter_.next();
        if (entityPtr == null || entityPtr.isNull())
            return AccessResult.accessNull;
        ptr.assign(new EntityNode(grove_, entityPtr.pointer()));
        return AccessResult.accessOK;
    }

    public override AccessResult chunkRest(ref NodeListPtr ptr)
    {
        var entityPtr = iter_.next();
        if (entityPtr == null || entityPtr.isNull())
            return AccessResult.accessNull;
        ptr.assign(new EntitiesNodeList(grove_, iter_));
        return AccessResult.accessOK;
    }
}

// Notations node list (iterator-based)
public class NotationsNodeList : BaseNodeList
{
    private GroveImpl grove_;
    private NamedResourceTableIter<Notation> iter_;

    public NotationsNodeList(GroveImpl grove, NamedResourceTableIter<Notation> iter)
    {
        grove_ = grove;
        iter_ = iter;
    }

    public override AccessResult first(ref NodePtr ptr)
    {
        var notationPtr = iter_.next();
        if (notationPtr == null || notationPtr.isNull())
            return AccessResult.accessNull;
        ptr.assign(new NotationNode(grove_, notationPtr.pointer()));
        return AccessResult.accessOK;
    }

    public override AccessResult chunkRest(ref NodeListPtr ptr)
    {
        var notationPtr = iter_.next();
        if (notationPtr == null || notationPtr.isNull())
            return AccessResult.accessNull;
        ptr.assign(new NotationsNodeList(grove_, iter_));
        return AccessResult.accessOK;
    }
}

// Element types node list (iterator-based)
public class ElementTypesNodeList : BaseNodeList
{
    private GroveImpl grove_;
    private NamedTableIter<ElementType> iter_;

    public ElementTypesNodeList(GroveImpl grove, NamedTableIter<ElementType> iter)
    {
        grove_ = grove;
        iter_ = iter;
    }

    public override AccessResult first(ref NodePtr ptr)
    {
        var elementType = iter_.next();
        if (elementType == null)
            return AccessResult.accessNull;
        ptr.assign(new ElementTypeNode(grove_, elementType));
        return AccessResult.accessOK;
    }

    public override AccessResult chunkRest(ref NodeListPtr ptr)
    {
        var elementType = iter_.next();
        if (elementType == null)
            return AccessResult.accessNull;
        ptr.assign(new ElementTypesNodeList(grove_, iter_));
        return AccessResult.accessOK;
    }
}

// Element type attribute defs named node list
public class ElementTypeAttributeDefsNamedNodeList : BaseNamedNodeList
{
    private ElementType elementType_;

    public ElementTypeAttributeDefsNamedNodeList(GroveImpl grove, ElementType elementType)
        : base(grove, grove.generalSubstTable())
    {
        elementType_ = elementType;
    }

    public override NodeListPtr nodeList()
    {
        return new NodeListPtr(new ElementTypeAttributeDefsNodeList(grove_, elementType_, 0));
    }

    public override AccessResult namedNodeU(StringC str, ref NodePtr ptr)
    {
        var defList = elementType_.attributeDefTemp();
        if (defList == null)
            return AccessResult.accessNull;
        for (nuint i = 0; i < defList.size(); i++)
        {
            var def = defList.def(i);
            if (def != null && def.name().Equals(str))
            {
                ptr.assign(new ElementTypeAttributeDefNode(grove_, elementType_, i));
                return AccessResult.accessOK;
            }
        }
        return AccessResult.accessNull;
    }

    public override Type type() { return Type.attributeDefs; }
}

// Notation attribute defs named node list
public class NotationAttributeDefsNamedNodeList : BaseNamedNodeList
{
    private Notation notation_;

    public NotationAttributeDefsNamedNodeList(GroveImpl grove, Notation notation)
        : base(grove, grove.generalSubstTable())
    {
        notation_ = notation;
    }

    public override NodeListPtr nodeList()
    {
        return new NodeListPtr(new NotationAttributeDefsNodeList(grove_, notation_, 0));
    }

    public override AccessResult namedNodeU(StringC str, ref NodePtr ptr)
    {
        var defList = notation_.attributeDefTemp();
        if (defList == null)
            return AccessResult.accessNull;
        for (nuint i = 0; i < defList.size(); i++)
        {
            var def = defList.def(i);
            if (def != null && def.name().Equals(str))
            {
                ptr.assign(new NotationAttributeDefNode(grove_, notation_, i));
                return AccessResult.accessOK;
            }
        }
        return AccessResult.accessNull;
    }

    public override Type type() { return Type.attributeDefs; }
}

// Grove builder message event handler - handles messages and validation
public class GroveBuilderMessageEventHandler : ErrorCountEventHandler
{
    protected GroveImpl grove_;
    protected Messenger? messenger_;
    protected MessageFormatter? formatter_;

    public GroveBuilderMessageEventHandler(uint index, Messenger? messenger, MessageFormatter? formatter)
    {
        grove_ = new GroveImpl(index);
        messenger_ = messenger;
        formatter_ = formatter;
    }

    public GroveBuilderMessageEventHandler(uint index, Messenger? messenger, MessageFormatter? formatter,
        ConstPtr<Sd> sd, ConstPtr<Syntax> prologSyntax, ConstPtr<Syntax> instanceSyntax)
    {
        grove_ = new GroveImpl(index);
        grove_.setSd(sd, prologSyntax, instanceSyntax);
        messenger_ = messenger;
        formatter_ = formatter;
    }

    public void makeInitialRoot(ref NodePtr root)
    {
        var r = grove_.root();
        if (r != null)
            root.assign(new SgmlDocumentNode(grove_, r));
    }

    public override void message(MessageEvent? @event)
    {
        if (@event != null)
        {
            messenger_?.dispatchMessage(@event.message());
            base.message(@event);
        }
    }

    public override void endProlog(EndPrologEvent? @event)
    {
        if (@event != null)
            grove_.setDtd(@event.dtdPointer());
    }

    public override void appinfo(AppinfoEvent? @event)
    {
        if (@event != null)
        {
            StringC? appinfo;
            if (@event.literal(out appinfo) && appinfo != null)
                grove_.setAppinfo(appinfo);
        }
    }

    public override void sgmlDecl(SgmlDeclEvent? @event)
    {
        if (@event != null)
            grove_.setSd(@event.sdPointer(), @event.prologSyntaxPointer(), @event.instanceSyntaxPointer());
    }

    public override void entityDefaulted(EntityDefaultedEvent? @event)
    {
        if (@event != null)
            grove_.addDefaultedEntity(@event.entityPointer());
    }

    ~GroveBuilderMessageEventHandler()
    {
        grove_.setComplete();
    }
}

// Grove builder event handler - extends message handler with actual grove construction
public class GroveBuilderEventHandler : GroveBuilderMessageEventHandler
{
    public GroveBuilderEventHandler(uint index, Messenger? messenger, MessageFormatter? formatter)
        : base(index, messenger, formatter)
    {
    }

    public GroveBuilderEventHandler(uint index, Messenger? messenger, MessageFormatter? formatter,
        ConstPtr<Sd> sd, ConstPtr<Syntax> prologSyntax, ConstPtr<Syntax> instanceSyntax)
        : base(index, messenger, formatter, sd, prologSyntax, instanceSyntax)
    {
    }

    public override void startElement(StartElementEvent? @event)
    {
        if (@event != null)
            ElementNode.add(grove_, @event);
    }

    public override void endElement(EndElementEvent? @event)
    {
        grove_.pop();
    }

    public override void data(DataEvent? @event)
    {
        if (@event != null)
            DataNode.add(grove_, @event);
    }

    public override void pi(PiEvent? @event)
    {
        if (@event != null)
            PiNode.add(grove_, @event);
    }

    public override void sdataEntity(SdataEntityEvent? @event)
    {
        if (@event != null)
            SdataNode.add(grove_, @event);
    }

    public override void nonSgmlChar(NonSgmlCharEvent? @event)
    {
        if (@event != null)
            NonSgmlNode.add(grove_, @event);
    }

    public override void externalDataEntity(ExternalDataEntityEvent? @event)
    {
        if (@event != null)
            ExternalDataNode.add(grove_, @event);
    }

    public override void subdocEntity(SubdocEntityEvent? @event)
    {
        if (@event != null)
            SubdocNode.add(grove_, @event);
    }
}

// Main GroveBuilder class - factory for creating grove event handlers
public class GroveBuilder
{
    private GroveBuilder() { }

    public static bool setBlocking(bool blocking)
    {
        bool old = GroveBuilderConstants.blockingAccess;
        GroveBuilderConstants.blockingAccess = blocking;
        return old;
    }

    public static ErrorCountEventHandler make(
        uint index,
        Messenger? messenger,
        MessageFormatter? formatter,
        bool validateOnly,
        ref NodePtr root)
    {
        GroveBuilderMessageEventHandler eh;
        if (validateOnly)
            eh = new GroveBuilderMessageEventHandler(index, messenger, formatter);
        else
            eh = new GroveBuilderEventHandler(index, messenger, formatter);
        eh.makeInitialRoot(ref root);
        return eh;
    }

    public static ErrorCountEventHandler make(
        uint index,
        Messenger? messenger,
        MessageFormatter? formatter,
        bool validateOnly,
        ConstPtr<Sd> sd,
        ConstPtr<Syntax> prologSyntax,
        ConstPtr<Syntax> instanceSyntax,
        ref NodePtr root)
    {
        GroveBuilderMessageEventHandler eh;
        if (validateOnly)
            eh = new GroveBuilderMessageEventHandler(index, messenger, formatter, sd, prologSyntax, instanceSyntax);
        else
            eh = new GroveBuilderEventHandler(index, messenger, formatter, sd, prologSyntax, instanceSyntax);
        eh.makeInitialRoot(ref root);
        return eh;
    }
}

// Base node list class
public class BaseNodeList : NodeList
{
    private uint refCount_;

    public BaseNodeList()
    {
        refCount_ = 0;
    }

    public override void addRef() { ++refCount_; }

    public bool canReuse(NodeListPtr ptr)
    {
        return ptr.list == this && refCount_ == 1;
    }

    public override void release()
    {
        if (--refCount_ == 0)
        {
            // Will be garbage collected
        }
    }

    public override AccessResult first(ref NodePtr ptr)
    {
        return AccessResult.accessNull;
    }

    public override AccessResult rest(ref NodeListPtr ptr)
    {
        return chunkRest(ref ptr);
    }

    public override AccessResult chunkRest(ref NodeListPtr ptr)
    {
        return AccessResult.accessNull;
    }
}

// Sibling node list - iterates over siblings
public class SiblingNodeList : BaseNodeList
{
    private NodePtr first_;

    public SiblingNodeList(NodePtr first)
    {
        first_ = first;
    }

    public override AccessResult first(ref NodePtr ptr)
    {
        ptr = first_;
        return AccessResult.accessOK;
    }

    public override AccessResult rest(ref NodeListPtr ptr)
    {
        AccessResult ret;
        if (canReuse(ptr))
        {
            ret = first_.assignNextSibling();
            if (ret == AccessResult.accessOK)
                return ret;
        }
        else
        {
            NodePtr next = new NodePtr();
            ret = first_.node!.nextSibling(ref next);
            if (ret == AccessResult.accessOK)
            {
                ptr.assign(new SiblingNodeList(next));
                return ret;
            }
        }
        if (ret == AccessResult.accessNull)
        {
            ptr.assign(new BaseNodeList());
            return AccessResult.accessOK;
        }
        return ret;
    }

    public override AccessResult chunkRest(ref NodeListPtr ptr)
    {
        AccessResult ret;
        if (canReuse(ptr))
        {
            ret = first_.assignNextChunkSibling();
            if (ret == AccessResult.accessOK)
                return ret;
        }
        else
        {
            NodePtr next = new NodePtr();
            ret = first_.node!.nextChunkSibling(ref next);
            if (ret == AccessResult.accessOK)
            {
                ptr.assign(new SiblingNodeList(next));
                return ret;
            }
        }
        if (ret == AccessResult.accessNull)
        {
            ptr.assign(new BaseNodeList());
            return AccessResult.accessOK;
        }
        return ret;
    }

    public override AccessResult @ref(uint i, ref NodePtr ptr)
    {
        if (i == 0)
        {
            ptr = first_;
            return AccessResult.accessOK;
        }
        return first_.node!.followSiblingRef(i - 1, ref ptr);
    }
}
