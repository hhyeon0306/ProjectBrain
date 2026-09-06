using System;

namespace ProjectBrain
{
    // MCP resumes from a compact summary; the full immutable baseline stays on disk.
    [Serializable] public sealed class BrainTaskView
    {
        public string id;
        public int revision;
        public string purpose;
        public string[] targetNodeIds;
        public string progress;
        public string decisions;
        public string unresolved;
        public string nextAction;
        public string[] references;
        public string[] allowedPaths;
        public string baselineReference;
        public string baselineHash;
        public int baselineFileCount;
        public string gitStateAtBegin;
        public string createdUtc;
        public string updatedUtc;
        public static BrainTaskView From(BrainTaskRecord task) => new BrainTaskView
        {
            id = task.id, revision = task.revision, purpose = task.purpose, targetNodeIds = task.targetNodeIds,
            progress = task.progress, decisions = task.decisions, unresolved = task.unresolved, nextAction = task.nextAction,
            references = task.references, allowedPaths = task.allowedPaths, baselineReference = ".projectbrain/tasks/active.json#baseline",
            baselineHash = BrainWorkspace.Hash(new UnityBrainJson().Write(task.baseline)), baselineFileCount = task.baseline.files.Length,
            gitStateAtBegin = task.gitStateAtBegin, createdUtc = task.createdUtc, updatedUtc = task.updatedUtc
        };
    }
    [Serializable] public sealed class BrainStatusView
    {
        public BrainTaskView task;
        public bool resumed;
        public BrainChange[] changes;
        public string[] coverageLimitations;
        public static BrainStatusView From(BrainTaskStatus status) => new BrainStatusView { task = BrainTaskView.From(status.task), resumed = status.resumed, changes = status.changes, coverageLimitations = status.coverageLimitations };
    }
}
