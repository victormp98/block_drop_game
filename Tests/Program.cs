using BlockDrop.Tests.Tetris;
using BlockDrop.Tests.Shared;
using BlockDrop.Tests.ConsoleUI;
using BlockDrop.Tests.Snake;
using BlockDrop.Tests.Racing;
using BlockDrop.Tests.Platformer;

var checks = TetrisChecks.Run() + SnakeChecks.Run() + RacingChecks.Run() + PlatformerChecks.Run() + StorageChecks.Run() + ControllerChecks.Run();
Console.WriteLine($"All {checks} game, storage and console checks passed.");
