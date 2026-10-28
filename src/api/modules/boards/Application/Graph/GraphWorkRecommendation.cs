namespace Coordina.Api.Modules.Boards.Application.Graph;

public sealed record GraphWorkRecommendation<TNode>(
  TNode Node,
  int Score,
  int BlockerCount,
  int UnlockScore,
  int UrgencyScore,
  int PriorityScore,
  int ProgressScore,
  bool IsCriticalPath,
  int EffortScore,
  int AssignmentScore,
  int MomentumScore,
  int ContextScore,
  bool FitsAvailableTime,
  bool IsAssignmentMatch,
  bool IsContextMatch,
  bool IsInProgress)
  where TNode : notnull;
