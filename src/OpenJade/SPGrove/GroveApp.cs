// Copyright (c) 1996 James Clark
// See the file COPYING for copying permission.

namespace OpenJade.SPGrove;

using OpenSP;
using OpenJade.Grove;

public abstract class GroveApp : ParserApp
{
    protected NodePtr rootNode_ = new NodePtr();

    public GroveApp(string? requiredCodingSystem = null)
        : base(requiredCodingSystem)
    {
    }

    public override ErrorCountEventHandler makeEventHandler()
    {
        return GroveBuilder.make(0, this, null, false, ref rootNode_);
    }

    protected override int generateEvents(ErrorCountEventHandler eceh)
    {
        // First, parse the document and build the grove
        // (In the original C++, this runs in a separate thread)
        int result = base.generateEvents(eceh);

        // Now that the grove is built, process it
        processGrove();
        rootNode_.clear();

        return result;
    }

    public abstract void processGrove();

    public class GenerateEventArgs
    {
        private ErrorCountEventHandler eceh_;
        private GroveApp app_;

        public GenerateEventArgs(ErrorCountEventHandler eceh, GroveApp app)
        {
            eceh_ = eceh;
            app_ = app;
        }

        public int run()
        {
            return app_.inheritedGenerateEvents(eceh_);
        }
    }

    public new void dispatchMessage(Message message)
    {
        // In the original, this uses a mutex for thread safety
        base.dispatchMessage(message);
    }

    private int inheritedGenerateEvents(ErrorCountEventHandler eceh)
    {
        return base.generateEvents(eceh);
    }
}
