using System.Collections.Generic;
using Godot;

namespace Bytenol.Tetris.Scripts;

public partial class GridDrawer : Node2D
{
	public static readonly int Width = 10;
	public static readonly int Height = 20;
	public static readonly int CellSize = 32;	// size of the cell without spacing
	public static readonly int EmptyVal = -1;	// value assigned to an empty board cell
	private static readonly List<List<int>> Board;
	public static float Size{ get; private set; } = 30;	// size of the cell with spacing

	public delegate void OnClearDel(int clearCount);
	public static event OnClearDel OnClear;		// emitted when a grid row is cleared

	static GridDrawer()
	{
		Board = new();
		ResetBoard();

		Tetris.OnRestart += () =>
		{
			ResetBoard();
		};
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Size = CellSize * 0.9f;
		Tetromino.OnSave += OnSave;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(!(Tetris.PlayState == GameState.PLAYING)) 
			return;
		QueueRedraw();
	}

	public override void _Draw()
	{
		for(int y = 0; y < Height; y++)
		{
			for(int x = 0; x < 	Width; x++)
			{
				int id = GetBoardAt(y, x);
				var pos = GetPosition(y, x);
				Rect2 rect = new (pos.X, pos.Y, Size, Size);
				if(id != EmptyVal)
				{
					Rect2 src = new(id * 30, 0, 30, 30);
					DrawTextureRectRegion(Tetromino.BlockTexture, rect, src);
				}
				DrawRect(rect, new Color((float)(132 / 255), (float)(140 / 255), 1.0f, 0.3f), false);
			}
		}

		DrawLine(new(Width * CellSize, 0), new(Width * CellSize, Height * CellSize), Colors.Black, 2);
	}

	/* Converts a 2d row/column data to the window's location

	Paramaters:
		row - is the row of the position based on the grid's height
		col - is the column of the position based on the grid's width
	
	Returns:
		A float vector representing the position on the visible screen
	*/
	public static Vector2 GetPosition(int row, int col)
	{
		float spacing = (CellSize - Size) * 0.5f;
		float px = col * CellSize + spacing;
		float py = row * CellSize + spacing;
		return new(px, py);
	}

	public static void SetBoardAt(int row, int col, int value)
	{
		Board[row][col] = value;
	}


	public static int GetBoardAt(int row, int col)
	{
		if(row < 0 || row >= Height || col < 0 || col >= Width)
			return EmptyVal;
		return Board[row][col];
	}

	// Called when a Tetromino get saved on the board
	// @param tetromino - is the tetromino to be saved
	private void OnSave(Tetromino tetromino)
	{
		List<int> filledRow = new();
		for(int i = 0; i < Board.Count; i++)
		{
			if(!Board[i].Contains(EmptyVal))
				filledRow.Add(i);
		}

		foreach (int row in filledRow)
		{
			Board.RemoveAt(row);
			List<int> list = new(){EmptyVal, EmptyVal, EmptyVal, EmptyVal, EmptyVal, EmptyVal, EmptyVal, EmptyVal, EmptyVal, EmptyVal};
			Board.Insert(0, list);
		}

		if(filledRow.Count > 0)
			OnClear(filledRow.Count);
	}

	private static void ResetBoard()
	{
		Board.Clear();
		for(int i = 0; i < Height; i++)
		{
			Board.Add(new());
			for(int j = 0; j < Width; j++)
				Board[i].Add(EmptyVal);
		}
	}
}
