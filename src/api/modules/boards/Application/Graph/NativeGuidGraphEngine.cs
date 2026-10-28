using System.Runtime.InteropServices;

namespace Coordina.Api.Modules.Boards.Application.Graph;

public sealed class NativeGuidGraphEngine : IGraphEngine<Guid>
{
  private const string LibraryName = "coordina_graph_engine";

  public GraphEdgeEvaluation EvaluateEdge(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges,
    Guid source,
    Guid target)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);
    var status = NativeMethods.EvaluateEdge(
      nativeNodes,
      ToNativeCount(nativeNodes.Length),
      nativeEdges,
      ToNativeCount(nativeEdges.Length),
      ToNative(source),
      ToNative(target));

    return new GraphEdgeEvaluation(status);
  }

  public IReadOnlyCollection<Guid> SuggestTargets(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges,
    Guid source,
    int maxSuggestions)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);

    if (maxSuggestions < 1)
    {
      throw new ArgumentOutOfRangeException(
        nameof(maxSuggestions),
        "At least one suggestion is required.");
    }

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);
    var output = new NativeGraphNodeId[maxSuggestions];
    var count = NativeMethods.SuggestTargets(
      nativeNodes,
      ToNativeCount(nativeNodes.Length),
      nativeEdges,
      ToNativeCount(nativeEdges.Length),
      ToNative(source),
      output,
      ToNativeCount(output.Length));

    return output
      .Take(checked((int)count))
      .Select(FromNative)
      .ToArray();
  }

  public bool IsAcyclic(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);
    var result = NativeMethods.IsAcyclic(
      nativeNodes,
      ToNativeCount(nativeNodes.Length),
      nativeEdges,
      ToNativeCount(nativeEdges.Length));

    return result == 1;
  }

  public IReadOnlyCollection<Guid> SortTopologically(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);

    return ReadNodeCollection(
      nativeNodes.Length,
      output => NativeMethods.TopologicalSort(
        nativeNodes,
        ToNativeCount(nativeNodes.Length),
        nativeEdges,
        ToNativeCount(nativeEdges.Length),
        output,
        ToNativeCount(output.Length)));
  }

  public IReadOnlyCollection<Guid> FindReadyNodes(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);

    return ReadNodeCollection(
      nativeNodes.Length,
      output => NativeMethods.ReadyNodes(
        nativeNodes,
        ToNativeCount(nativeNodes.Length),
        nativeEdges,
        ToNativeCount(nativeEdges.Length),
        output,
        ToNativeCount(output.Length)));
  }

  public IReadOnlyCollection<Guid> FindImpactedDependents(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges,
    Guid dependency)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);

    return ReadNodeCollection(
      nativeNodes.Length,
      output => NativeMethods.ImpactedDependents(
        nativeNodes,
        ToNativeCount(nativeNodes.Length),
        nativeEdges,
        ToNativeCount(nativeEdges.Length),
        ToNative(dependency),
        output,
        ToNativeCount(output.Length)));
  }

  public IReadOnlyCollection<GraphNodeAnalysis<Guid>> AnalyzeNodes(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);

    return ReadNodeAnalysisCollection(
      nativeNodes.Length,
      output => NativeMethods.AnalyzeNodes(
        nativeNodes,
        ToNativeCount(nativeNodes.Length),
        nativeEdges,
        ToNativeCount(nativeEdges.Length),
        output,
        ToNativeCount(output.Length)));
  }

  public IReadOnlyCollection<Guid> FindCriticalPath(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);

    return ReadNodeCollection(
      nativeNodes.Length,
      output => NativeMethods.CriticalPath(
        nativeNodes,
        ToNativeCount(nativeNodes.Length),
        nativeEdges,
        ToNativeCount(nativeEdges.Length),
        output,
        ToNativeCount(output.Length)));
  }

  public IReadOnlyCollection<GraphWorkRecommendation<Guid>> PlanWork(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges,
    IReadOnlyCollection<GraphWorkItem<Guid>> workItems,
    GraphPlanningOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);
    ArgumentNullException.ThrowIfNull(workItems);

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);
    var nativeWorkItems = ToNativeWorkItems(workItems);
    var nativeOptions = ToNativeOptions(options ?? GraphPlanningOptions.Default);

    return ReadWorkRecommendationCollection(
      nativeWorkItems.Length,
      output => NativeMethods.PlanWork(
        nativeNodes,
        ToNativeCount(nativeNodes.Length),
        nativeEdges,
        ToNativeCount(nativeEdges.Length),
        nativeWorkItems,
        ToNativeCount(nativeWorkItems.Length),
        ref nativeOptions,
        output,
        ToNativeCount(output.Length)));
  }

  public GraphBoardAnalysis<Guid> AnalyzeBoard(
    IReadOnlyCollection<Guid> nodes,
    IReadOnlyCollection<GraphEdge<Guid>> edges,
    IReadOnlyCollection<GraphWorkItem<Guid>> workItems,
    GraphPlanningOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(nodes);
    ArgumentNullException.ThrowIfNull(edges);
    ArgumentNullException.ThrowIfNull(workItems);

    var nativeNodes = ToNativeNodes(nodes);
    var nativeEdges = ToNativeEdges(edges);
    var nativeWorkItems = ToNativeWorkItems(workItems);
    var nativeOptions = ToNativeOptions(options ?? GraphPlanningOptions.Default);
    var buffers = new BoardAnalysisBuffers(nativeNodes.Length, nativeWorkItems.Length);

    var status = buffers.Run((ref NativeGraphBoardAnalysis output) => NativeMethods.AnalyzeBoard(
      nativeNodes,
      ToNativeCount(nativeNodes.Length),
      nativeEdges,
      ToNativeCount(nativeEdges.Length),
      nativeWorkItems,
      ToNativeCount(nativeWorkItems.Length),
      ref nativeOptions,
      ref output));

    if (status != NativeGraphStatus.Ok)
    {
      throw new GraphEngineException("analyze_board", (int)status);
    }

    return buffers.ToAnalysis();
  }

  private static Guid[] ReadNodeCollection(
    int capacity,
    Func<NativeGraphNodeId[], UIntPtr> read)
  {
    var output = new NativeGraphNodeId[capacity];
    var count = read(output);

    return output
      .Take(checked((int)count))
      .Select(FromNative)
      .ToArray();
  }

  private static GraphNodeAnalysis<Guid>[] ReadNodeAnalysisCollection(
    int capacity,
    Func<NativeGraphNodeAnalysis[], UIntPtr> read)
  {
    var output = new NativeGraphNodeAnalysis[capacity];
    var count = read(output);

    return output
      .Take(checked((int)count))
      .Select(FromNative)
      .ToArray();
  }

  private static GraphNodeAnalysis<Guid> FromNative(NativeGraphNodeAnalysis analysis) =>
    new(
      FromNative(analysis.Node),
      checked((int)analysis.DependencyCount),
      checked((int)analysis.DependentCount),
      checked((int)analysis.DependencyDepth),
      checked((int)analysis.DependentDepth),
      checked((int)analysis.TransitiveDependentCount));

  private static GraphWorkRecommendation<Guid>[] ReadWorkRecommendationCollection(
    int capacity,
    Func<NativeGraphWorkRecommendation[], UIntPtr> read)
  {
    var output = new NativeGraphWorkRecommendation[capacity];
    var count = read(output);

    return output
      .Take(checked((int)count))
      .Select(FromNative)
      .ToArray();
  }

  private static GraphWorkRecommendation<Guid> FromNative(
    NativeGraphWorkRecommendation recommendation) =>
    new(
      FromNative(recommendation.Node),
      checked((int)recommendation.Score),
      checked((int)recommendation.BlockerCount),
      checked((int)recommendation.UnlockScore),
      checked((int)recommendation.UrgencyScore),
      checked((int)recommendation.PriorityScore),
      checked((int)recommendation.ProgressScore),
      recommendation.IsCriticalPath != 0U,
      recommendation.EffortScore,
      recommendation.AssignmentScore,
      checked((int)recommendation.MomentumScore),
      checked((int)recommendation.ContextScore),
      recommendation.FitsAvailableTime != 0U,
      recommendation.IsAssignmentMatch != 0U,
      recommendation.IsContextMatch != 0U,
      recommendation.IsInProgress != 0U);

  private static NativeGraphNodeId[] ToNativeNodes(
    IReadOnlyCollection<Guid> nodes) =>
    nodes.Select(ToNative).ToArray();

  private static NativeGraphEdge[] ToNativeEdges(
    IReadOnlyCollection<GraphEdge<Guid>> edges) =>
    edges
      .Select(edge => new NativeGraphEdge(
        ToNative(edge.Source),
        ToNative(edge.Target)))
      .ToArray();

  private static NativeGraphWorkItem[] ToNativeWorkItems(
    IReadOnlyCollection<GraphWorkItem<Guid>> workItems) =>
    workItems
      .Select(item => new NativeGraphWorkItem(
        ToNative(item.Node),
        item.IsCompleted ? 1U : 0U,
        checked((uint)item.Priority),
        item.DueDays,
        checked((uint)item.SubtaskTotal),
        checked((uint)item.SubtaskCompleted),
        checked((uint)item.Position),
        checked((uint)item.EstimatedMinutes),
        (uint)item.Assignment,
        item.IsInProgress ? 1U : 0U,
        item.IsContextMatch ? 1U : 0U))
      .ToArray();

  private static NativeGraphPlanningOptions ToNativeOptions(
    GraphPlanningOptions options) =>
    new(
      checked((uint)options.AvailableMinutes),
      options.PreferAssigned ? 1U : 0U,
      options.IncludeUnassigned ? 1U : 0U);

  private static NativeGraphNodeId ToNative(Guid value)
  {
    Span<byte> bytes = stackalloc byte[16];
    value.TryWriteBytes(bytes);

    return new NativeGraphNodeId(
      BitConverter.ToUInt64(bytes[..8]),
      BitConverter.ToUInt64(bytes[8..]));
  }

  private static Guid FromNative(NativeGraphNodeId value)
  {
    Span<byte> bytes = stackalloc byte[16];
    BitConverter.TryWriteBytes(bytes[..8], value.High);
    BitConverter.TryWriteBytes(bytes[8..], value.Low);

    return new Guid(bytes);
  }

  private static UIntPtr ToNativeCount(int count) => new((ulong)count);

  [StructLayout(LayoutKind.Sequential)]
  private readonly record struct NativeGraphNodeId(
    ulong High,
    ulong Low);

  [StructLayout(LayoutKind.Sequential)]
  private readonly record struct NativeGraphEdge(
    NativeGraphNodeId Source,
    NativeGraphNodeId Target);

  [StructLayout(LayoutKind.Sequential)]
  private readonly record struct NativeGraphNodeAnalysis(
    NativeGraphNodeId Node,
    uint DependencyCount,
    uint DependentCount,
    uint DependencyDepth,
    uint DependentDepth,
    uint TransitiveDependentCount);

  [StructLayout(LayoutKind.Sequential)]
  private readonly record struct NativeGraphWorkItem(
    NativeGraphNodeId Node,
    uint IsCompleted,
    uint Priority,
    int DueDays,
    uint SubtaskTotal,
    uint SubtaskCompleted,
    uint Position,
    uint EstimatedMinutes,
    uint Assignment,
    uint IsInProgress,
    uint IsContextMatch);

  [StructLayout(LayoutKind.Sequential)]
  private record struct NativeGraphPlanningOptions(
    uint AvailableMinutes,
    uint PreferAssigned,
    uint IncludeUnassigned);

  [StructLayout(LayoutKind.Sequential)]
  private readonly record struct NativeGraphWorkRecommendation(
    NativeGraphNodeId Node,
    uint Score,
    uint BlockerCount,
    uint UnlockScore,
    uint UrgencyScore,
    uint PriorityScore,
    uint ProgressScore,
    uint IsCriticalPath,
    int EffortScore,
    int AssignmentScore,
    uint MomentumScore,
    uint ContextScore,
    uint FitsAvailableTime,
    uint IsAssignmentMatch,
    uint IsContextMatch,
    uint IsInProgress);

  private enum NativeGraphStatus
  {
    Ok = 0,
    InvalidArgument = 1,
    InsufficientCapacity = 2
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct NativeGraphNodeBuffer
  {
    public IntPtr Items;
    public UIntPtr Capacity;
    public UIntPtr Count;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct NativeGraphBoardAnalysis
  {
    public NativeGraphNodeBuffer ReadyNodes;
    public NativeGraphNodeBuffer DependencyOrder;
    public NativeGraphNodeBuffer CriticalPath;
    public IntPtr NodeAnalyses;
    public UIntPtr NodeAnalysisCapacity;
    public UIntPtr NodeAnalysisCount;
    public IntPtr Recommendations;
    public UIntPtr RecommendationCapacity;
    public UIntPtr RecommendationCount;
    public uint IsAcyclic;
    public uint Reserved;
  }

  private delegate NativeGraphStatus BoardAnalysisCall(ref NativeGraphBoardAnalysis output);

  private sealed class BoardAnalysisBuffers(int nodeCount, int workItemCount)
  {
    private readonly NativeGraphNodeId[] _readyNodes = new NativeGraphNodeId[nodeCount];
    private readonly NativeGraphNodeId[] _dependencyOrder = new NativeGraphNodeId[nodeCount];
    private readonly NativeGraphNodeId[] _criticalPath = new NativeGraphNodeId[nodeCount];
    private readonly NativeGraphNodeAnalysis[] _nodeAnalyses = new NativeGraphNodeAnalysis[nodeCount];
    private readonly NativeGraphWorkRecommendation[] _recommendations =
      new NativeGraphWorkRecommendation[workItemCount];
    private NativeGraphBoardAnalysis _output;

    public NativeGraphStatus Run(BoardAnalysisCall call)
    {
      var handles = new[]
      {
        GCHandle.Alloc(_readyNodes, GCHandleType.Pinned),
        GCHandle.Alloc(_dependencyOrder, GCHandleType.Pinned),
        GCHandle.Alloc(_criticalPath, GCHandleType.Pinned),
        GCHandle.Alloc(_nodeAnalyses, GCHandleType.Pinned),
        GCHandle.Alloc(_recommendations, GCHandleType.Pinned)
      };

      try
      {
        _output = new NativeGraphBoardAnalysis
        {
          ReadyNodes = Buffer(handles[0], _readyNodes.Length),
          DependencyOrder = Buffer(handles[1], _dependencyOrder.Length),
          CriticalPath = Buffer(handles[2], _criticalPath.Length),
          NodeAnalyses = handles[3].AddrOfPinnedObject(),
          NodeAnalysisCapacity = ToNativeCount(_nodeAnalyses.Length),
          Recommendations = handles[4].AddrOfPinnedObject(),
          RecommendationCapacity = ToNativeCount(_recommendations.Length)
        };

        return call(ref _output);
      }
      finally
      {
        foreach (var handle in handles)
        {
          handle.Free();
        }
      }
    }

    public GraphBoardAnalysis<Guid> ToAnalysis() =>
      new(
        _output.IsAcyclic != 0U,
        Take(_readyNodes, _output.ReadyNodes.Count).Select(FromNative).ToArray(),
        Take(_dependencyOrder, _output.DependencyOrder.Count).Select(FromNative).ToArray(),
        Take(_criticalPath, _output.CriticalPath.Count).Select(FromNative).ToArray(),
        Take(_nodeAnalyses, _output.NodeAnalysisCount).Select(FromNative).ToArray(),
        Take(_recommendations, _output.RecommendationCount).Select(FromNative).ToArray());

    private static NativeGraphNodeBuffer Buffer(GCHandle handle, int capacity) =>
      new()
      {
        Items = handle.AddrOfPinnedObject(),
        Capacity = ToNativeCount(capacity)
      };

    private static IEnumerable<T> Take<T>(T[] buffer, UIntPtr count)
    {
      var written = checked((int)count.ToUInt64());

      if (written > buffer.Length)
      {
        throw new GraphEngineException(
          $"Native graph engine reported {written} items for a buffer of {buffer.Length}.");
      }

      return buffer.Take(written);
    }
  }

  private static partial class NativeMethods
  {
    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_evaluate_edge",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern GraphEdgeStatus EvaluateEdge(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount,
      NativeGraphNodeId source,
      NativeGraphNodeId target);

    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_suggest_targets",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern UIntPtr SuggestTargets(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount,
      NativeGraphNodeId source,
      [Out]
      NativeGraphNodeId[] output,
      UIntPtr outputCapacity);

    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_is_acyclic",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern int IsAcyclic(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount);

    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_topological_sort",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern UIntPtr TopologicalSort(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount,
      [Out]
      NativeGraphNodeId[] output,
      UIntPtr outputCapacity);

    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_ready_nodes",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern UIntPtr ReadyNodes(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount,
      [Out]
      NativeGraphNodeId[] output,
      UIntPtr outputCapacity);

    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_impacted_dependents",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern UIntPtr ImpactedDependents(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount,
      NativeGraphNodeId dependency,
      [Out]
      NativeGraphNodeId[] output,
      UIntPtr outputCapacity);

    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_analyze_nodes",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern UIntPtr AnalyzeNodes(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount,
      [Out]
      NativeGraphNodeAnalysis[] output,
      UIntPtr outputCapacity);

    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_critical_path",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern UIntPtr CriticalPath(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount,
      [Out]
      NativeGraphNodeId[] output,
      UIntPtr outputCapacity);

    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_plan_work",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern UIntPtr PlanWork(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount,
      NativeGraphWorkItem[] workItems,
      UIntPtr workItemCount,
      ref NativeGraphPlanningOptions options,
      [Out]
      NativeGraphWorkRecommendation[] output,
      UIntPtr outputCapacity);

    [DllImport(
      LibraryName,
      EntryPoint = "coordina_graph_analyze_board",
      CallingConvention = CallingConvention.Cdecl)]
    internal static extern NativeGraphStatus AnalyzeBoard(
      NativeGraphNodeId[] nodes,
      UIntPtr nodeCount,
      NativeGraphEdge[] edges,
      UIntPtr edgeCount,
      NativeGraphWorkItem[] workItems,
      UIntPtr workItemCount,
      ref NativeGraphPlanningOptions options,
      ref NativeGraphBoardAnalysis output);
  }
}
