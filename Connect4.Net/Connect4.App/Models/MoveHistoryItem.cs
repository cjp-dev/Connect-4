namespace Connect4.App.Models;

/// <summary>A turn/round in the move history containing Red's and Yellow's moves.</summary>
public sealed record MoveHistoryItem(int Round, string Notation, string RedMove, string YellowMove);

